/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     UDP client for connecting to a COTLMP server
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using COTLMPServer;
using COTLMPServer.Messages;
using System;
using System.Net;
using System.Net.Sockets;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Network
{
    /**
     * @brief
     * Manages the UDP connection from the local player to a COTLMP server.
     * All events are fired on the background receive thread; callers must
     * marshal to the Unity main thread before touching game objects.
     */
    internal sealed class Client : IDisposable
    {
        /* ------------------------------------------------------------------ */
        /* Public state                                                         */
        /* ------------------------------------------------------------------ */

        /** True once the server has sent a successful PlayerJoinAck */
        public bool IsConnected  { get; private set; }

        /** Server-assigned ID for the local player (-1 until connected) */
        public int  LocalId      { get; private set; } = -1;

        /* ------------------------------------------------------------------ */
        /* Events (fired on network thread)                                     */
        /* ------------------------------------------------------------------ */

        /** Server confirmed our join; arg = assigned ID */
        public event Action<int>                  Connected;

        /** Connection closed locally or remotely */
        public event Action                       Disconnected;

        /** Kicked by the server; arg = reason string */
        public event Action<string>               KickedFromServer;

        /** Another player joined; args = (id, name) */
        public event Action<int, string>          PlayerJoined;

        /** Another player left; arg = id */
        public event Action<int>                  PlayerLeft;

        /** Position update relayed from server; args = (id, x, y, facingAngle) */
        public event Action<int, float, float, float>  PlayerPositionUpdated;

        /** State update relayed from server; args = (id, StateMachine.State as int) */
        public event Action<int, int>             PlayerStateUpdated;

        /** Health update relayed from server; args = (id, hp, totalHp) */
        public event Action<int, float, float>    PlayerHealthUpdated;

        /** Animation event relayed from server; args = (id, animationName) */
        public event Action<int, string>          PlayerAnimationUpdated;

        /** Chat message relayed from server; args = (id, text) */
        public event Action<int, string>          ChatMessageReceived;

        /** Host save data received from server; arg = compressed save bytes */
        public event Action<byte[]>               HostSaveDataReceived;

        /** World state heartbeat from host; arg = compressed snapshot bytes */
        public event Action<byte[]>               WorldStateHeartbeatReceived;

        /** Host changed scene; arg = scene name */
        public event Action<string>               SceneChangeReceived;

        /* ------------------------------------------------------------------ */
        /* Private fields                                                       */
        /* ------------------------------------------------------------------ */

        private UdpClient  _udp;
        private IPEndPoint _serverEndpoint;
        private bool       _disposed;

        /* ------------------------------------------------------------------ */
        /* Public API                                                           */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Opens the UDP socket and sends a PlayerJoin request to the server.
         *
         * @param[in] address    Server IP address.
         * @param[in] port       Server game port.
         * @param[in] playerName Local player's display name.
         *
         * @returns true if the socket was opened and the join packet sent.
         */
        public bool Connect(IPAddress address, int port, string playerName)
        {
            if (IsConnected || _disposed) return false;
            try
            {
                _udp            = new UdpClient(0);      // OS-assigned local port
                _serverEndpoint = new IPEndPoint(address, port);

                // Increase the receive buffer to handle large payloads
                // like host save data (default 8KB is too small).
                try { _udp.Client.ReceiveBufferSize = 512 * 1024; }
                catch { /* non-critical */ }

                BeginReceive();

                Send(new Message
                {
                    Type = MessageType.PlayerJoin,
                    ID   = -1,
                    Data = MessagePayload.EncodePlayerJoin(playerName)
                });
                return true;
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogError($"[Client] Connect failed: {e.Message}");
                CleanupSocket();
                return false;
            }
        }

        /**
         * @brief
         * Sends a graceful PlayerLeave and disposes the socket.
         */
        public void Disconnect()
        {
            if (!_disposed)
            {
                try { Send(new Message { Type = MessageType.PlayerLeave, ID = LocalId }); }
                catch { /* best-effort */ }
            }
            Dispose();
        }

        /* ---- per-frame sync helpers ---- */

        public void SendPosition(float x, float y, float facingAngle)
        {
            if (!IsConnected) return;
            Send(new Message
            {
                Type = MessageType.PlayerPosition,
                ID   = LocalId,
                Data = MessagePayload.EncodePlayerPosition(x, y, facingAngle)
            });
        }

        public void SendState(int state)
        {
            if (!IsConnected) return;
            Send(new Message
            {
                Type = MessageType.PlayerState,
                ID   = LocalId,
                Data = MessagePayload.EncodePlayerState(state)
            });
        }

        public void SendHealth(float hp, float totalHp)
        {
            if (!IsConnected) return;
            Send(new Message
            {
                Type = MessageType.PlayerHealth,
                ID   = LocalId,
                Data = MessagePayload.EncodePlayerHealth(hp, totalHp)
            });
        }

        public void SendAnimation(string animName)
        {
            if (!IsConnected) return;
            Send(new Message
            {
                Type = MessageType.PlayerAnimation,
                ID   = LocalId,
                Data = MessagePayload.EncodeStringPayload(animName)
            });
        }

        public void SendChat(string text)
        {
            if (!IsConnected) return;
            Send(new Message
            {
                Type = MessageType.ChatMessage,
                ID   = LocalId,
                Data = MessagePayload.EncodeStringPayload(text)
            });
        }

        public void SendHeartbeat()
        {
            if (!IsConnected) return;
            Send(new Message { Type = MessageType.Heartbeat, ID = LocalId });
        }

        public void SendWorldStateHeartbeat(byte[] compressedSnapshot)
        {
            if (!IsConnected || compressedSnapshot == null) return;
            Send(new Message
            {
                Type = MessageType.WorldStateHeartbeat,
                ID   = LocalId,
                Data = compressedSnapshot
            });
        }

        public void SendSceneChange(string sceneName)
        {
            if (!IsConnected) return;
            Send(new Message
            {
                Type = MessageType.SceneChange,
                ID   = LocalId,
                Data = MessagePayload.EncodeStringPayload(sceneName)
            });
        }

        public void SendSaveResync(byte[] compressedSave)
        {
            if (!IsConnected || compressedSave == null) return;
            Send(new Message
            {
                Type = MessageType.SaveResync,
                ID   = LocalId,
                Data = compressedSave
            });
        }

        /* ------------------------------------------------------------------ */
        /* Internals                                                            */
        /* ------------------------------------------------------------------ */

        private void Send(Message message)
        {
            if (_disposed || _udp == null) return;
            try
            {
                byte[] data = message.Serialize();
                _udp.Send(data, data.Length, _serverEndpoint);
            }
            catch { /* socket may be closing */ }
        }

        private void BeginReceive()
        {
            if (_disposed || _udp == null) return;
            try { _udp.BeginReceive(OnReceive, null); }
            catch { /* socket closed */ }
        }

        private void OnReceive(IAsyncResult ar)
        {
            if (_disposed) return;
            IPEndPoint ep = null;
            byte[] data;
            try
            {
                data = _udp.EndReceive(ar, ref ep);
            }
            catch
            {
                if (!_disposed) Dispose();
                return;
            }

            Message msg;
            try   { msg = Message.Deserialize(data); }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[Client] Failed to deserialize packet ({data?.Length ?? 0} bytes): {ex.Message}");
                BeginReceive();
                return;
            }

            HandleMessage(msg);
            BeginReceive();
        }

        private void HandleMessage(Message msg)
        {
            switch (msg.Type)
            {
                case MessageType.PlayerJoinAck:
                    try
                    {
                        MessagePayload.DecodePlayerJoinAck(msg.Data, out int id, out bool ok);
                        if (ok)
                        {
                            LocalId     = id;
                            IsConnected = true;
                            Connected?.Invoke(id);
                        }
                        else
                        {
                            KickedFromServer?.Invoke("Server rejected connection");
                            Dispose();
                        }
                    }
                    catch { }
                    break;

                case MessageType.PlayerJoin:
                    try
                    {
                        string name = MessagePayload.DecodePlayerJoin(msg.Data);
                        PlayerJoined?.Invoke(msg.ID, name);
                    }
                    catch { }
                    break;

                case MessageType.PlayerLeave:
                    PlayerLeft?.Invoke(msg.ID);
                    break;

                case MessageType.PlayerPosition:
                    try
                    {
                        MessagePayload.DecodePlayerPosition(msg.Data, out float x, out float y, out float a);
                        PlayerPositionUpdated?.Invoke(msg.ID, x, y, a);
                    }
                    catch { }
                    break;

                case MessageType.PlayerState:
                    try
                    {
                        int state = MessagePayload.DecodePlayerState(msg.Data);
                        PlayerStateUpdated?.Invoke(msg.ID, state);
                    }
                    catch { }
                    break;

                case MessageType.PlayerHealth:
                    try
                    {
                        MessagePayload.DecodePlayerHealth(msg.Data, out float hp, out float tot);
                        PlayerHealthUpdated?.Invoke(msg.ID, hp, tot);
                    }
                    catch { }
                    break;

                case MessageType.PlayerAnimation:
                    try
                    {
                        string anim = MessagePayload.DecodeStringPayload(msg.Data);
                        PlayerAnimationUpdated?.Invoke(msg.ID, anim);
                    }
                    catch { }
                    break;

                case MessageType.ChatMessage:
                    try
                    {
                        string text = MessagePayload.DecodeStringPayload(msg.Data);
                        ChatMessageReceived?.Invoke(msg.ID, text);
                    }
                    catch { }
                    break;

                case MessageType.PlayerKick:
                    try
                    {
                        string reason = MessagePayload.DecodeStringPayload(msg.Data);
                        IsConnected = false;
                        KickedFromServer?.Invoke(reason);
                    }
                    catch { }
                    Dispose();
                    break;

                case MessageType.HeartbeatAck:
                    /* connection alive */ break;

                case MessageType.HostSaveData:
                    Plugin.Logger?.LogInfo($"[Client] Received HostSaveData ({msg.Data?.Length ?? 0} bytes)");
                    HostSaveDataReceived?.Invoke(msg.Data);
                    break;

                case MessageType.HostSaveDataChunk:
                    try { HandleSaveChunk(msg.Data); }
                    catch { }
                    break;

                case MessageType.WorldStateHeartbeat:
                    WorldStateHeartbeatReceived?.Invoke(msg.Data);
                    break;

                case MessageType.SceneChange:
                    try
                    {
                        string scene = MessagePayload.DecodeStringPayload(msg.Data);
                        SceneChangeReceived?.Invoke(scene);
                    }
                    catch { }
                    break;
            }
        }

        /* ------------------------------------------------------------------ */
        /* Chunked save-data reassembly                                         */
        /* ------------------------------------------------------------------ */

        private byte[] _saveBuffer;
        private int    _saveChunksReceived;
        private int    _saveTotalChunks;

        private void HandleSaveChunk(byte[] chunkPayload)
        {
            if (chunkPayload == null || chunkPayload.Length < 12) return;

            int chunkIndex  = System.BitConverter.ToInt32(chunkPayload, 0);
            int totalChunks = System.BitConverter.ToInt32(chunkPayload, 4);
            int totalSize   = System.BitConverter.ToInt32(chunkPayload, 8);
            int dataLen     = chunkPayload.Length - 12;

            // Allocate or reset buffer on first (or new) transfer
            if (_saveBuffer == null || _saveBuffer.Length != totalSize || totalChunks != _saveTotalChunks)
            {
                _saveBuffer         = new byte[totalSize];
                _saveChunksReceived = 0;
                _saveTotalChunks    = totalChunks;
            }

            int offset = chunkIndex * 60000; // must match server's MaxSafePayload
            System.Buffer.BlockCopy(chunkPayload, 12, _saveBuffer, offset, dataLen);
            _saveChunksReceived++;

            if (_saveChunksReceived >= _saveTotalChunks)
            {
                HostSaveDataReceived?.Invoke(_saveBuffer);
                _saveBuffer = null;
            }
        }

        private void CleanupSocket()
        {
            try { _udp?.Close(); } catch { }
            _udp = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed   = false; // set before cleanup so send sees it
            _disposed   = true;
            IsConnected = false;
            CleanupSocket();
            Disconnected?.Invoke();
        }
    }
}

/* EOF */
