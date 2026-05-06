/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the network class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMP.Ui;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using COTLMPServer.Messages;
using System;
using MMTools;
using COTLMP.Game;
using COTLMP.Data;
using System.Collections;
using System.Text;
using System.IO;
using COTLMP.Debug;
using Rewired.Utils.Interfaces;
using System.Runtime.InteropServices;

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
     */
    internal static class Network
    {
        public static event Action OnDisconnect;
        public static bool IsConnected => online != 0;

        private static UdpClient client;
        private static PlayerFarming localPlayer;
        private static CancellationToken cancelToken;
        private static Vector3 lastPosition;
        private static SemaphoreSlim sendLock;
        private static int online;
        private static CancellationTokenRegistration registration;
        private static uint localID;
        private static object seqLock;
        private static uint sequence;

        private const float updateFrequencySec = 1f / 30f; // 30hz

        private static async System.Threading.Tasks.Task Send(Message msg)
        {
            await sendLock.WaitAsync(cancelToken);
            try
            {
                lock(seqLock)
                    msg.Sequence = sequence++;
                byte[] bytes = msg.Serialize();
                await client.SendAsync(bytes, bytes.Length);
            }
            finally
            {
                sendLock.Release();
            }
        }

        private static async System.Threading.Tasks.Task<Message> Get()
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
            try
            {
                var limit = System.Threading.Tasks.Task.Delay(30000, cts.Token);
                var recv = client.ReceiveAsync();
                if(await System.Threading.Tasks.Task.WhenAny(limit, recv) == limit)
                {
                    throw new Exception();
                }
                UdpReceiveResult result = await recv;
                if (result.Buffer.Length > 1500)
                    throw new Exception();
                return Message.Deserialize(result.Buffer);
            }
            catch
            {
                lock (seqLock)
                    return new(MessageType.Disconnect, sequence++, Encoding.UTF8.GetBytes("Disconnected"));
            }
            finally
            {
                cts.Cancel();
            }
        }

        public static IEnumerator SendUpdates()
        {
            float lastMessage = Time.time;
            var wait = new WaitForSecondsRealtime(updateFrequencySec);
            while (!cancelToken.IsCancellationRequested)
            {
                if(Time.time - lastMessage > 7.5f)
                {
                    Message ping = new(MessageType.Ping, 0);
                    lastMessage = Time.time;
                    _ = Send(ping);
                }
                if ((lastPosition -(localPlayer?.transform.position ?? new())).sqrMagnitude > 0.0001f)
                {
                    lastPosition = localPlayer?.transform.position ?? new();
                    Message msg = new(MessageType.PositionUpdate, 0, lastPosition.ToNetwork().Serialize());
                    lastMessage = Time.time;
                    _ = Send(msg);
                }
                yield return wait;
            }
        }

        private static uint ReverseEndianness(uint val)
        {
            return ((val & 0x000000FFU) << 24 |
                    (val & 0x0000FF00U) << 8 |
                    (val & 0x00FF0000U) >> 8 |
                    (val & 0xFF000000U) >> 24);
        }

        public static IEnumerator PollServer()
        {
            var wait = new WaitForTask(null);
            bool keepLooping = true;
            while(!cancelToken.IsCancellationRequested && keepLooping)
            {
                yield return null; // let the game run for one frame

                var recv = Get();
                wait.what = recv;
                yield return wait;

                if (recv.IsFaulted || recv.IsCanceled)
                {
                    PauseMenuPatches.Message = recv.IsFaulted ? recv.Exception?.InnerException?.Message : "Disconnected";
                    PrintLogger.Print(DebugLevel.FATAL_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, recv.Exception?.InnerException?.Message);
                    break;
                }

                Message msg = recv.Result;

                lock(seqLock)
                {
                    if (msg.Sequence < sequence && msg.Type != MessageType.Disconnect)
                        continue;
                    if (msg.Sequence >= sequence)
                        sequence = msg.Sequence + 1;
                }

                if (localPlayer == null && msg.Type != MessageType.Disconnect) // don't process messages if we're not in game
                    continue;

                try
                {
                    switch(msg.Type)
                    {
                        case MessageType.PositionUpdate:
                            {
                                if (msg.Data.Length < sizeof(uint) + COTLMPServer.Vector3.SerializedSize)
                                    throw new InvalidDataException("data too small!");

                                uint id = BitConverter.ToUInt32(msg.Data, 0);
                                if (!BitConverter.IsLittleEndian) // the data in the message is in little endian, convert
                                    id = ReverseEndianness(id);

                                var pos = COTLMPServer.Vector3.Deserialize(msg.Data, sizeof(uint), out _);

                                if (!PlayerManager.DoesPlayerExist(id))
                                    PlayerManager.CreatePlayer(id, pos.ToUnity());
                                else
                                    PlayerManager.MovePlayer(id, pos.ToUnity(), updateFrequencySec);
                            }
                            break;

                        case MessageType.StateUpdate:
                            {
                                var plrinfo = PlayerInfo.Deserialize(msg.Data);

                                if (!PlayerManager.DoesPlayerExist(plrinfo.ID))
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

                                if (!PlayerManager.DoesPlayerExist(info.ID))
                                    PlayerManager.CreatePlayer(info.ID, info.Position.ToUnity());
                                else
                                    PlayerManager.MovePlayer(info.ID, info.Position.ToUnity(), 0);

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
                    PrintLogger.Print(DebugLevel.FATAL_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, $"Server sent invalid data: {e.Message}");
                    PauseMenuPatches.Message = e.Message;
                    break;
                }
                catch (Exception e) when ((e is OperationCanceledException) || (e is ObjectDisposedException && cancelToken.IsCancellationRequested))  
                {
                    PrintLogger.Print(DebugLevel.INFO_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, "Disconnected.");
                }
                catch (Exception e)
                {
                    PrintLogger.Print(DebugLevel.FATAL_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, e.Message);
                    PauseMenuPatches.Message = e.Message;
                    break;
                }
            }

            Message disconnect = new Message(MessageType.Disconnect, 0, PauseMenuPatches.Message != null ? Encoding.UTF8.GetBytes(PauseMenuPatches.Message) : null);
            wait.what = Send(disconnect);
            yield return wait;

            client.Dispose();
            registration.Dispose();
            OnDisconnect?.Invoke();
            Interlocked.Exchange(ref online, 0);
            yield break;
        }

        public async static Task<bool> Connect(IPEndPoint server, CancellationToken token)
        {
            if (Interlocked.CompareExchange(ref online, 1, 0) != 0)
                return false;

            cancelToken = token;
            lastPosition = new();
            sequence = 3;
            sendLock = new(1, 1);
            client = new();
            seqLock = new();
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

                localID = BitConverter.ToUInt32(msg.Data, 0);
                if (!BitConverter.IsLittleEndian)
                {
                    localID = ReverseEndianness(localID);
                }

                Plugin.MonoInstance.StartCoroutine(PollServer());
                Plugin.MonoInstance.StartCoroutine(SendUpdates());
                return true;
            } catch
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

        private async static void OnStateChanged(StateMachine.State newState, StateMachine.State _)
        {
            if (newState == StateMachine.State.CustomAnimation || newState == StateMachine.State.Moving || localPlayer == null)
                return;
            await Send(new Message(MessageType.StateUpdate, 0, localPlayer.state.ToNetwork(localPlayer.transform.position.ToNetwork()).Serialize()));
        }

        private static async void OnTransitionComplete()
        {
            localPlayer?.state.OnStateChange -= OnStateChanged;
            localPlayer = PlayerFarming.Instance;
            
            if(localPlayer != null)
            {
                localPlayer.state.OnStateChange += OnStateChanged;
                Message msg = new Message(MessageType.Transition, 0, Encoding.UTF8.GetBytes(SceneManager.GetActiveScene().name));
                await Send(msg);
            }
        }

        private static void OnBeginTransition()
        {
            if (online != 0)
                for (uint i = 0; i < InternalData.MaxPlayersPerServerInternal; ++i)
                    PlayerManager.DeletePlayer(i);
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
    }
}

/* EOF */
