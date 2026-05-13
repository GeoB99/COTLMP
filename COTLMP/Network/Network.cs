/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the network class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMP.Debug;
using COTLMP.Game;
using COTLMP.Ui;
using COTLMPServer.Messages;
using HarmonyLib;
using MMTools;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * The namespace for all network-related classes, enums and structs
 */
namespace COTLMP.Network
{
    /**
     * @brief
     * The main class for network features
     * 
     * @field OnDisconnect
     * The event that will be invoked upon a disconnect
     * 
     * @field IsConnected
     * Whether the game is online or not
     * 
     * @field client
     * The main UdpClient for the connection
     * 
     * @field localPlayer
     * The local player instance
     * 
     * @field cancelToken
     * The cancellation token used to cancel online functions
     * 
     * @field sendLock
     * The lock used to protect UdpClient.SendAsync()
     * 
     * @field online
     * Whether the game is online or not (internal)
     * 
     * @field registration
     * The cancellation token cancellation for disposing of the UdpClient once the token is cancelled
     * 
     * @field seqLock
     * The lock to protect sequence
     * 
     * @field sequence
     * The sequence number of the next message
     * 
     * @field transitionHappened
     * Whether a transition happened
     * 
     * @field messageQueue
     * A queue of messages
     * 
     * @field updateFrequencySec
     * How often the game sends position updates (in seconds)
     * 
     * @field maxProcessPerFrame
     * How many messages the game will process in one frame
     */
    internal static class Network
    {
        public static event Action OnDisconnect;
        public static bool IsConnected => online != 0;

        private static UdpClient client;
        private static PlayerFarming localPlayer;
        private static CancellationToken cancelToken;
        private static SemaphoreSlim sendLock;
        private static int online;
        private static CancellationTokenRegistration registration;
        private static object seqLock;
        private static uint sequence;
        private static bool transitionHappened;
        private static ConcurrentQueue<Message> messageQueue;

        private const float updateFrequencySec = 1f / 15f; // 15hz
        private const uint maxProcessPerFrame = 100;

        /**
         * @brief
         * Safely concurrently send messages
         * 
         * @param[in] msg
         * What message to send
         * 
         * @remarks
         * The sequence number in msg is ignored and overwritten in the method
         */
        private static async System.Threading.Tasks.Task Send(Message msg)
        {
            if (online == 0)
                return;

            await sendLock.WaitAsync(cancelToken);
            try
            {
                lock (seqLock)
                    msg.Sequence = sequence++;
                byte[] bytes = msg.Serialize();
                await client.SendAsync(bytes, bytes.Length);
            }
            finally
            {
                sendLock.Release();
            }
        }

        /**
         * @brief
         * Monitor if the server has stopped responding and send pings at regular intervals
         */
        private static async System.Threading.Tasks.Task HeartBeat()
        {
            var ping = new Message(MessageType.Ping, 0);
            while (!cancelToken.IsCancellationRequested && online != 0)
            {
                _ = Send(ping);
                await System.Threading.Tasks.Task.Delay(10000, cancelToken);
            }
        }

        /**
         * @brief
         * Run in a loop recieving messages from the client
         */
        private static async System.Threading.Tasks.Task Recv()
        {
            try
            {
                while (!cancelToken.IsCancellationRequested && online != 0)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);

                    var limit = System.Threading.Tasks.Task.Delay(30000, cts.Token);
                    var recv = client.ReceiveAsync();

                    if (await System.Threading.Tasks.Task.WhenAny(limit, recv) == limit)
                        throw new TimeoutException("Timed out");
                    cts.Cancel();

                    var result = await recv;

                    messageQueue.Enqueue(Message.Deserialize(result.Buffer));
                }
            }
            catch (OperationCanceledException)
            {
                PrintLogger.Print(DebugLevel.INFO_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, "Disconnecting...");
            }
            catch (Exception e)
            {
                PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, $"Recieve error type {e.GetType().Name} message {e.Message}");
                while (messageQueue.TryDequeue(out _)) ;
                messageQueue.Enqueue(new Message(MessageType.Disconnect, 0, Encoding.UTF8.GetBytes(e.Message)));
            }
        }

        /**
         * @brief
         * Send position updates
         */
        public static IEnumerator SendUpdates()
        {
            var wait = new WaitForSecondsRealtime(updateFrequencySec);
            Vector3 lastPosition = new();
            while (!cancelToken.IsCancellationRequested && online != 0)
            {
                if ((lastPosition - (localPlayer?.transform.position ?? new())).sqrMagnitude > 0.0001f)
                {
                    lastPosition = localPlayer?.transform.position ?? new();
                    Message msg = new(MessageType.PositionUpdate, 0, lastPosition.ToNetwork().Serialize());
                    _ = Send(msg);
                }
                yield return wait;
            }
        }

        /**
         * @brief
         * Reverse the endianness of a uint
         * 
         * @param[in] val
         * The uint
         * 
         * @returns
         * The uint with its byte order reversed
         */
        private static uint ReverseEndianness(uint val)
        {
            return ((val & 0x000000FFU) << 24 |
                    (val & 0x0000FF00U) << 8 |
                    (val & 0x00FF0000U) >> 8 |
                    (val & 0xFF000000U) >> 24);
        }

        /**
         * @brief
         * Main message processing loop
         */
        public static IEnumerator PollServer()
        {
            var wait = new WaitForTask(null);
            bool keepLooping = true;
            int processedThisFrame = 0;
            while (!cancelToken.IsCancellationRequested && keepLooping)
            {
                if (processedThisFrame == maxProcessPerFrame)
                {
                    if (messageQueue.TryPeek(out _))
                        PrintLogger.Print(DebugLevel.WARNING_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, "Can't keep up! Is the game overloaded?");
                    processedThisFrame = 0;
                    yield return null;
                }

                Message msg;
                while (!messageQueue.TryDequeue(out msg))
                {
                    processedThisFrame = 0;
                    yield return null;
                }

                ++processedThisFrame;

                lock (seqLock)
                {
                    if (msg.Sequence < sequence && msg.Type != MessageType.Disconnect)
                        continue;
                    if (msg.Sequence >= sequence)
                        sequence = msg.Sequence + 1;
                }

                if (localPlayer == null) // try to get the localplayer again if its null
                {
                    localPlayer = PlayerFarming.Instance;
                    localPlayer?.state.OnStateChange += OnStateChanged;
                }

                // FIXME: this doesn't seem to work to differentiate between the crusade select place and the main cult
                if (localPlayer != null && transitionHappened)
                {
                    _ = Send(new Message(MessageType.Transition,
                        0,
                        Encoding.UTF8.GetBytes(SceneManager.GetActiveScene().name)));
                    transitionHappened = false;
                }

                try
                {
                    switch (msg.Type)
                    {
                        case MessageType.PositionUpdate:
                            {
                                if (msg.Data.Length < sizeof(uint) + COTLMPServer.Vector3.SerializedSize)
                                    throw new InvalidDataException("data too small!");

                                uint id = BitConverter.ToUInt32(msg.Data, 0);
                                if (!BitConverter.IsLittleEndian) // the data in the message is in little endian, convert
                                    id = ReverseEndianness(id);

                                var pos = COTLMPServer.Vector3.Deserialize(msg.Data, sizeof(uint), out _);

                                if (!PlayerManager.DoesPlayerExist(id) && PlayerFarming.Instance != null)
                                    PlayerManager.CreatePlayer(id, pos.ToUnity());
                                else
                                    PlayerManager.MovePlayer(id, pos.ToUnity(), 1); // just use 1 as a timeout to get smooth animation
                            }
                            break;

                        case MessageType.StateUpdate:
                            {
                                var plrinfo = PlayerInfo.Deserialize(msg.Data);

                                if (!PlayerManager.DoesPlayerExist(plrinfo.ID) && PlayerFarming.Instance != null)
                                {
                                    PlayerManager.CreatePlayer(plrinfo.ID, plrinfo.State.Position.ToUnity(), plrinfo.Skin);
                                    PlayerManager.SetPlayerState(plrinfo.ID, plrinfo.State.ToUnity());
                                }
                                else
                                {
                                    PlayerManager.MovePlayer(plrinfo.ID, plrinfo.State.Position.ToUnity(), 0);
                                    PlayerManager.SetPlayerState(plrinfo.ID, plrinfo.State.ToUnity());
                                }
                            }
                            break;

                        case MessageType.PlayerLeft:
                            {
                                if (msg.Data.Length < sizeof(uint))
                                    throw new InvalidDataException("data too small!");

                                uint id = BitConverter.ToUInt32(msg.Data, 0);
                                if (!BitConverter.IsLittleEndian)
                                    id = ReverseEndianness(id);

                                PlayerManager.DeletePlayer(id);
                            }
                            break;

                        case MessageType.CustomAnimation:
                            {
                                var info = CustomAnimationInfo.Deserialize(msg.Data);

                                if (!PlayerManager.DoesPlayerExist(info.ID) && PlayerFarming.Instance != null)
                                    PlayerManager.CreatePlayer(info.ID, info.Position.ToUnity());
                                else
                                    PlayerManager.MovePlayerNow(info.ID, info.Position.ToUnity());

                                PlayerManager.SetPlayerState(info.ID, null, true, info.Name, info.Loop);
                            }
                            break;

                        case MessageType.Disconnect:
                            keepLooping = false;
                            if (msg.Data != null)
                                PauseMenuPatches.Message = Encoding.UTF8.GetString(msg.Data);
                            break;
                    }
                }
                catch (InvalidDataException e)
                {
                    PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, $"Server sent invalid data: {e.Message}");
                    PauseMenuPatches.Message = e.Message;
                    break;
                }
                catch (Exception e) when ((e is OperationCanceledException) || (e is ObjectDisposedException && cancelToken.IsCancellationRequested))
                {
                    PrintLogger.Print(DebugLevel.INFO_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, "Disconnected.");
                }
                catch (Exception e)
                {
                    PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, $"Unknown error {e.GetType().Name} {e}");
                    PauseMenuPatches.Message = e.Message;
                    break;
                }
            }

            Message disconnect = new Message(MessageType.Disconnect, 0, PauseMenuPatches.Message != null ? Encoding.UTF8.GetBytes(PauseMenuPatches.Message) : null);
            wait.what = Send(disconnect);
            yield return wait;

            client.Dispose();
            client = null;
            registration.Dispose();
            OnDisconnect?.Invoke();
            Interlocked.Exchange(ref online, 0);
            yield break;
        }

        /**
         * @brief
         * Attempt to connect to a server
         * 
         * @param[in] server
         * The server's endpoint
         * 
         * @param[in] token
         * The cancellation token to use for this connection
         */
        public async static Task<bool> Connect(IPEndPoint server, CancellationToken token)
        {
            if (Interlocked.CompareExchange(ref online, 1, 0) != 0)
                return false;

            cancelToken = token;
            sequence = 3;
            sendLock = new(1, 1);
            client = new();
            seqLock = new();
            messageQueue = new();
            transitionHappened = false;
            client.Connect(server);

            registration = cancelToken.Register(client.Dispose);

            byte[] handshakeBytes = new Message(MessageType.Handshake, 1, new HandshakeClient(Plugin.Globals.PlayerName, Application.version).Serialize()).Serialize();

            try
            {
                await client.SendAsync(handshakeBytes, handshakeBytes.Length);

                Task<UdpReceiveResult> resultTask = client.ReceiveAsync();

                UdpReceiveResult result = await resultTask;
                if (result.Buffer.Length > 1500)
                    throw new Exception();

                Message msg = Message.Deserialize(result.Buffer);
                if (msg.Type != MessageType.Handshake || msg.Sequence != 2 || msg.Data.Length < sizeof(uint))
                {
                    if (msg.Type == MessageType.Disconnect && msg.Data != null)
                        PauseMenuPatches.Message = Encoding.UTF8.GetString(msg.Data);
                    throw new Exception();
                }

                Plugin.MonoInstance.StartCoroutine(PollServer());
                Plugin.MonoInstance.StartCoroutine(SendUpdates());
                _ = System.Threading.Tasks.Task.Run(Recv);
                _ = System.Threading.Tasks.Task.Run(HeartBeat);
                return true;
            }
            catch
            {
                client.Dispose();
                registration.Dispose();
                Interlocked.Exchange(ref online, 0);
                return false;
            }
        }

        /**
         * @brief
         * If the scene loaded is main menu, stop the integrated server
         *
         * @param[in] scene
         * The scene
         *
         * @param[in] _
         * The scene load mode, unused
         */
        private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            if (scene.name.Equals("Main Menu"))
            {
                // set the quitting flag temporarily so it doesn't try to transition to the main menu on server stop
                PauseMenuPatches.Quitting = true;
                PauseMenuPatches.StopServer();
                PauseMenuPatches.Quitting = false;
            }
        }

        /**
         * @brief
         * On game quitting, stop the integrated server
         */
        private static void OnQuitting()
        {
            PauseMenuPatches.Quitting = true;
            PauseMenuPatches.Server?.Dispose();
        }

        /**
         * @brief
         * When the local player state changes, update the server
         * 
         * @param[in] newState
         * The new player state
         */
        private async static void OnStateChanged(StateMachine.State newState, StateMachine.State _)
        {
            if (newState == StateMachine.State.CustomAnimation || newState == StateMachine.State.Moving || newState == StateMachine.State.Idle || localPlayer == null)
                return;
            await Send(new Message(MessageType.StateUpdate, 0, localPlayer.state.ToNetwork(localPlayer.transform.position.ToNetwork()).Serialize()));
        }

        /**
         * @brief
         * When a transition completes, update the server
         */
        private static async void OnTransitionComplete()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "Main Menu")
            {
                transitionHappened = true;
            }
            else
            {
                while (messageQueue.TryDequeue(out _)) ;
                messageQueue.Enqueue(new Message(MessageType.Disconnect,
                    0,
                    Encoding.UTF8.GetBytes("Disconnected")));
            }
        }

        /**
         * @brief
         * When a transition begins, remove the localPlayer reference
         */
        private static void OnBeginTransition()
        {
            localPlayer?.state.OnStateChange -= OnStateChanged;
            localPlayer = null;
        }

        /**
         * @brief
         * Initialize the network components
         */
        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting += OnQuitting;
            MMTransition.OnTransitionCompelte += OnTransitionComplete;
            MMTransition.OnBeginTransition += OnBeginTransition;
        }

        /**
         * @brief
         * Game patches related to networking
         */
        [HarmonyPatch]
        private static class NetworkPatches
        {
            [HarmonyPatch(typeof(PlayerFarming), nameof(PlayerFarming.CustomAnimation))]
            [HarmonyPrefix]
            private static void CustomAnimation(string Animation, bool Loop, PlayerFarming __instance)
            {
                if (__instance != PlayerFarming.Instance)
                    return;

                var msg = new Message(MessageType.CustomAnimation,
                    0,
                    new CustomAnimationInfo(Animation,
                    loop: Loop,
                    pos: __instance.gameObject.transform.position.ToNetwork()).Serialize());

                _ = Send(msg);
            }

            [HarmonyPatch(typeof(PlayerFarming), nameof(PlayerFarming.CustomAnimationRoutine))]
            [HarmonyPrefix]
            private static void CustomAnimationRoutine(string Animation, bool Loop, PlayerFarming __instance)
            {
                if (__instance != PlayerFarming.Instance)
                    return;

                var msg = new Message(MessageType.CustomAnimation,
                    0,
                    new CustomAnimationInfo(Animation,
                    loop: Loop,
                    pos: __instance.gameObject.transform.position.ToNetwork()).Serialize());

                _ = Send(msg);
            }

            [HarmonyPatch(typeof(PlayerFarming), nameof(PlayerFarming.CustomAnimationWithCallback))]
            [HarmonyPrefix]
            private static void CustomAnimationWithCallback(string Animation, bool Loop, PlayerFarming __instance)
            {
                if (__instance != PlayerFarming.Instance)
                    return;

                var msg = new Message(MessageType.CustomAnimation,
                    0,
                    new CustomAnimationInfo(Animation,
                    loop: Loop,
                    pos: __instance.gameObject.transform.position.ToNetwork()).Serialize());

                _ = Send(msg);
            }

            [HarmonyPatch(typeof(PlayerFarming), nameof(PlayerFarming.CustomAnimationWithCallBackRoutine))]
            [HarmonyPrefix]
            private static void CustomAnimationWithCallbackRoutine(string Animation, bool Loop, PlayerFarming __instance)
            {
                if (__instance != PlayerFarming.Instance)
                    return;

                var msg = new Message(MessageType.CustomAnimation,
                    0,
                    new CustomAnimationInfo(Animation,
                    loop: Loop,
                    pos: __instance.gameObject.transform.position.ToNetwork()).Serialize());

                _ = Send(msg);
            }
        }
    }
}

/* EOF */
