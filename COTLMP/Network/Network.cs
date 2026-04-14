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
        private static UdpClient client;
        private static PlayerFarming localPlayer;
        private static CancellationToken cancelToken;
        private static Vector3 lastPosition;
        private static SemaphoreSlim sendLock;
        private static bool walking;
        private static int online;
        private static int localID;
        private static uint sequence;

        private static async System.Threading.Tasks.Task Send(Message msg)
        {
            await sendLock.WaitAsync(cancelToken);
            try
            {
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


            return null;
        }

        public static IEnumerator SendUpdates()
        {
            while (!cancelToken.IsCancellationRequested)
            {
                if (walking && (lastPosition -(localPlayer?.transform.position ?? new())).sqrMagnitude > 0.0001f)
                {
                    lastPosition = localPlayer.transform.position;
                    Message msg = new(MessageType.PositionUpdate, 0, lastPosition.ToNetwork().Serialize());
                    _ = Send(msg);
                }
                yield return new WaitForSecondsRealtime(1f / 60f);// 60hz
            }
        }

        public static IEnumerator PollServer()
        {

            Interlocked.Exchange(ref online, 0);
            OnDisconnect?.Invoke();
            yield break;
        }

        public async static Task<bool> Connect(IPEndPoint server, CancellationToken token)
        {
            if (Interlocked.CompareExchange(ref online, 1, 0) != 0)
                return false;

            cancelToken = token;
            walking = false;
            lastPosition = new();
            sequence = 3;
            sendLock = new(1, 1);
            client = new();
            client.Connect(server);

            byte[] handshakeBytes = new Message(MessageType.Handshake, 1, new HandshakeClient(Plugin.Globals.PlayerName, Application.version).Serialize()).Serialize();

            try
            {
                await client.SendAsync(handshakeBytes, handshakeBytes.Length);

                Task<UdpReceiveResult> resultTask = client.ReceiveAsync();
                var delayTask = System.Threading.Tasks.Task.Delay(3500, cancelToken);

                if (await System.Threading.Tasks.Task.WhenAny(delayTask, resultTask) == delayTask)
                    throw new Exception();

                UdpReceiveResult result = await resultTask;
                if (result.Buffer.Length > 1500)
                    throw new Exception();

                Message msg = Message.Deserialize(result.Buffer);
                if (msg.Type != MessageType.Handshake || msg.Sequence != 2)
                    throw new Exception();

                localID = BitConverter.ToInt32(msg.Data, 0);

                Plugin.MonoInstance.StartCoroutine(PollServer());
                return true;
            } catch
            {
                client.Dispose();
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
            if (newState == StateMachine.State.CustomAnimation || localPlayer == null)
                return;
            if (newState == StateMachine.State.Moving)
                walking = true;
            walking = false;
            await Send(new Message(MessageType.StateUpdate, 0, localPlayer.state.ToNetwork(localPlayer.transform.position.ToNetwork()).Serialize()));
        }

        private static void OnTransitionComplete()
        {
            localPlayer?.state.OnStateChange -= OnStateChanged;
            localPlayer = PlayerFarming.Instance;
            localPlayer?.state.OnStateChange += OnStateChanged;
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
