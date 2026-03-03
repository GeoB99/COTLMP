/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define COTLMP server class
 * COPYRIGHT:   Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMPServer.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer
{
    /**
     * @brief
     * Hosts a UDP game server, tracks connected clients, relays player-sync
     * messages between clients and runs a companion LAN discovery listener.
     *
     * @field Port
     * The bound UDP port.
     *
     * @field ServerName / MaxPlayers / GameMode / PlayerCount
     * Server metadata exposed to the server browser.
     *
     * @field ServerStopped
     * Fired when the server stops (normally or on error).
     *
     * @field ClientJoined / ClientLeft
     * Fired on the network thread when a client joins or leaves.
     */
    public sealed class Server : IDisposable
    {
        public readonly int Port;
        public string  ServerName  { get; private set; }
        public int     MaxPlayers  { get; private set; }
        public string  GameMode    { get; private set; }
        public int     PlayerCount { get { lock (_clientsLock) { return _clients.Count; } } }

        public event EventHandler<ServerStoppedArgs> ServerStopped;
        public event EventHandler<ClientEventArgs>   ClientJoined;
        public event EventHandler<ClientEventArgs>   ClientLeft;

        private UdpClient  _client;
        private bool       _disposedValue;
        private ILogger    _logger;
        private ServerStopReason _reason = ServerStopReason.NormalShutdown;
        private static bool _started = false;

        // Client tracking
        private readonly Dictionary<int,    ClientInfo> _clients       = new Dictionary<int, ClientInfo>();
        private readonly Dictionary<string, int>        _endpointToId  = new Dictionary<string, int>();
        private int  _nextClientId = 1;
        private readonly object _clientsLock = new object();
        private readonly object _sendLock    = new object();

        // Host save data sent to joining clients for world sync
        private byte[] _hostSaveData;

        private LanDiscoveryServer _discoveryServer;

        /* ------------------------------------------------------------------ */
        /* Public API                                                           */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Stores the host's compressed save data so it can be sent to
         * each client that joins.  Call this after starting the server.
         */
        public void SetHostSaveData(byte[] data)
        {
            lock (_clientsLock)
            {
                _hostSaveData = data;
            }
        }

        /* ------------------------------------------------------------------ */
        /* Constructor                                                          */
        /* ------------------------------------------------------------------ */

        private Server(int port, ILogger logger, string serverName, int maxPlayers, string gameMode)
        {
            _logger    = logger;
            ServerName = string.IsNullOrEmpty(serverName) ? "COTL Server" : serverName;
            MaxPlayers = maxPlayers > 0 ? maxPlayers : 12;
            GameMode   = string.IsNullOrEmpty(gameMode)  ? "Standard"    : gameMode;

            _client = new UdpClient(port);

            // Increase send/receive buffers to handle large payloads (host save data).
            try { _client.Client.SendBufferSize    = 512 * 1024; } catch { }
            try { _client.Client.ReceiveBufferSize = 512 * 1024; } catch { }

            StartReceive();
            Port = ((IPEndPoint)_client.Client.LocalEndPoint).Port;

            try
            {
                _discoveryServer = new LanDiscoveryServer(ServerName, Port, MaxPlayers, GameMode,
                    () => PlayerCount);
            }
            catch (Exception e)
            {
                _logger?.LogWarning($"LAN discovery unavailable: {e.Message}");
            }

            _logger?.LogInfo($"Server started on port {Port} (name: \"{ServerName}\", maxPlayers: {MaxPlayers})");
        }

        /* ------------------------------------------------------------------ */
        /* Receive loop                                                         */
        /* ------------------------------------------------------------------ */

        private void StartReceive()
        {
            if (_disposedValue) return;
            try { _client.BeginReceive(PacketReceive, null); }
            catch { /* shutting down */ }
        }

        private void PacketReceive(IAsyncResult result)
        {
            if (_disposedValue) return;

            IPEndPoint endpoint = null;
            byte[] received;
            try
            {
                received = _client.EndReceive(result, ref endpoint);
            }
            catch (SocketException e)
            {
                if (!_disposedValue)
                {
                    _logger?.LogFatal(e.Message);
                    _reason = ServerStopReason.Error;
                    Dispose(true);
                }
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception e)
            {
                if (!_disposedValue)
                {
                    _logger?.LogFatal($"Unknown receive error: {e.Message}");
                    _reason = ServerStopReason.Error;
                    Dispose(true);
                }
                return;
            }

            Message message;
            try
            {
                message = Message.Deserialize(received);
            }
            catch (Exception e)
            {
                _logger?.LogError($"Corrupt packet from {endpoint}: {e.Message}");
                StartReceive();
                return;
            }

            HandleMessage(message, endpoint);
            StartReceive();
        }

        /* ------------------------------------------------------------------ */
        /* Message dispatch                                                     */
        /* ------------------------------------------------------------------ */

        private void HandleMessage(Message message, IPEndPoint endpoint)
        {
            string epKey = endpoint.ToString();

            switch (message.Type)
            {
                /* ---------- new player wanting to join ---------- */
                case MessageType.PlayerJoin:
                {
                    string playerName;
                    try   { playerName = MessagePayload.DecodePlayerJoin(message.Data); }
                    catch { playerName = "Unknown"; }

                    lock (_clientsLock)
                    {
                        if (_clients.Count >= MaxPlayers)
                        {
                            SendTo(new Message { Type = MessageType.PlayerKick, ID = -1,
                                Data = MessagePayload.EncodeStringPayload("Server is full") }, endpoint);
                            return;
                        }

                        // Resend ack if reconnecting
                        if (_endpointToId.TryGetValue(epKey, out int existingId))
                        {
                            SendTo(new Message { Type = MessageType.PlayerJoinAck, ID = existingId,
                                Data = MessagePayload.EncodePlayerJoinAck(existingId, true) }, endpoint);
                            return;
                        }

                        int id = _nextClientId++;
                        var client = new ClientInfo(id, endpoint, playerName);
                        _clients[id]       = client;
                        _endpointToId[epKey] = id;

                        // Tell the new client its assigned ID
                        SendTo(new Message { Type = MessageType.PlayerJoinAck, ID = id,
                            Data = MessagePayload.EncodePlayerJoinAck(id, true) }, endpoint);

                        // Tell existing clients about the newcomer
                        BroadcastExcept(new Message { Type = MessageType.PlayerJoin, ID = id,
                            Data = MessagePayload.EncodePlayerJoin(playerName) }, id);

                        // Tell the newcomer about every player that is already connected
                        // so they can create remote avatars for each existing peer.
                        foreach (var existing in _clients)
                        {
                            if (existing.Key != id)
                            {
                                SendTo(new Message { Type = MessageType.PlayerJoin, ID = existing.Key,
                                    Data = MessagePayload.EncodePlayerJoin(existing.Value.PlayerName) }, endpoint);
                            }
                        }

                        _logger?.LogInfo($"Player '{playerName}' (ID {id}) joined from {endpoint}");
                        ClientJoined?.Invoke(this, new ClientEventArgs(client));

                        // Send the host's save data so the joiner loads into the same world
                        if (_hostSaveData != null && _hostSaveData.Length > 0)
                        {
                            SendSaveData(id, _hostSaveData, endpoint);
                        }
                    }
                    break;
                }

                /* ---------- player leaving gracefully ---------- */
                case MessageType.PlayerLeave:
                {
                    lock (_clientsLock)
                    {
                        if (!_endpointToId.TryGetValue(epKey, out int id)) return;
                        if (!_clients.TryGetValue(id, out ClientInfo client)) return;

                        _clients.Remove(id);
                        _endpointToId.Remove(epKey);
                        BroadcastAll(new Message { Type = MessageType.PlayerLeave, ID = id });

                        _logger?.LogInfo($"Player '{client.PlayerName}' (ID {id}) left");
                        ClientLeft?.Invoke(this, new ClientEventArgs(client));
                    }
                    break;
                }

                /* ---------- relay-only messages ---------- */
                case MessageType.PlayerPosition:
                case MessageType.PlayerState:
                case MessageType.PlayerHealth:
                case MessageType.PlayerAnimation:
                case MessageType.ChatMessage:
                case MessageType.WorldStateHeartbeat:
                case MessageType.SceneChange:
                {
                    int senderId;
                    lock (_clientsLock)
                    {
                        if (!_endpointToId.TryGetValue(epKey, out senderId)) return;
                        // Update last-seen
                        if (_clients.TryGetValue(senderId, out ClientInfo c))
                            c.LastSeen = DateTime.UtcNow;
                    }
                    BroadcastExcept(new Message { Type = message.Type, ID = senderId,
                        Data = message.Data }, senderId);
                    break;
                }

                /* ---------- save resync (host broadcasts updated save) ---------- */
                case MessageType.SaveResync:
                {
                    int senderId;
                    lock (_clientsLock)
                    {
                        if (!_endpointToId.TryGetValue(epKey, out senderId)) return;
                        if (_clients.TryGetValue(senderId, out ClientInfo c))
                            c.LastSeen = DateTime.UtcNow;

                        // Update the stored save so future joiners get the latest
                        _hostSaveData = message.Data;
                    }

                    // Send to every other client using the chunked mechanism
                    // (save data may exceed a single UDP packet).
                    List<KeyValuePair<int, ClientInfo>> others;
                    lock (_clientsLock)
                    {
                        others = new List<KeyValuePair<int, ClientInfo>>();
                        foreach (var kvp in _clients)
                            if (kvp.Key != senderId)
                                others.Add(kvp);
                    }
                    foreach (var kvp in others)
                        SendSaveData(kvp.Key, message.Data, kvp.Value.EndPoint);
                    break;
                }

                /* ---------- keepalive ---------- */
                case MessageType.Heartbeat:
                {
                    int id;
                    lock (_clientsLock)
                    {
                        if (_endpointToId.TryGetValue(epKey, out id) &&
                            _clients.TryGetValue(id, out ClientInfo c))
                            c.LastSeen = DateTime.UtcNow;
                    }
                    SendTo(new Message { Type = MessageType.HeartbeatAck, ID = id }, endpoint);
                    break;
                }

                case MessageType.Test:
                    _logger?.LogInfo("Test message received.");
                    break;
            }
        }

        /* ------------------------------------------------------------------ */
        /* Send helpers                                                         */
        /* ------------------------------------------------------------------ */

        // Max payload that fits safely in a single UDP datagram.
        // UDP max is 65507; leave room for message header overhead.
        private const int MaxSafePayload = 60000;

        private void SendTo(Message message, IPEndPoint endpoint)
        {
            try
            {
                byte[] data = message.Serialize();
                lock (_sendLock)
                {
                    if (!_disposedValue && _client != null)
                        _client.Send(data, data.Length, endpoint);
                }
            }
            catch (Exception e)
            {
                _logger?.LogError($"Send failed to {endpoint}: {e.Message}");
            }
        }

        /**
         * @brief
         * Sends host save data to a client.  If the data fits in a single
         * UDP packet it is sent as HostSaveData; otherwise it is split into
         * HostSaveDataChunk packets that the client reassembles.
         *
         * Chunk layout: [chunkIndex:int32][totalChunks:int32][totalSize:int32][data]
         */
        private void SendSaveData(int clientId, byte[] fullData, IPEndPoint endpoint)
        {
            _logger?.LogInfo($"Sending save data ({fullData.Length} bytes) to client {clientId}");

            if (fullData.Length <= MaxSafePayload)
            {
                SendTo(new Message { Type = MessageType.HostSaveData, ID = clientId,
                    Data = fullData }, endpoint);
                return;
            }

            int totalChunks = (fullData.Length + MaxSafePayload - 1) / MaxSafePayload;
            _logger?.LogInfo($"Sending save data in {totalChunks} chunks ({fullData.Length} bytes) to client {clientId}");

            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * MaxSafePayload;
                int length = Math.Min(MaxSafePayload, fullData.Length - offset);

                byte[] chunkPayload = new byte[12 + length];
                BitConverter.GetBytes(i).CopyTo(chunkPayload, 0);
                BitConverter.GetBytes(totalChunks).CopyTo(chunkPayload, 4);
                BitConverter.GetBytes(fullData.Length).CopyTo(chunkPayload, 8);
                Buffer.BlockCopy(fullData, offset, chunkPayload, 12, length);

                SendTo(new Message { Type = MessageType.HostSaveDataChunk, ID = clientId,
                    Data = chunkPayload }, endpoint);
            }
        }

        private void BroadcastAll(Message message)
        {
            List<ClientInfo> snapshot;
            lock (_clientsLock)
                snapshot = new List<ClientInfo>(_clients.Values);

            foreach (var c in snapshot)
                SendTo(message, c.EndPoint);
        }

        private void BroadcastExcept(Message message, int excludeId)
        {
            List<ClientInfo> snapshot;
            lock (_clientsLock)
            {
                snapshot = new List<ClientInfo>();
                foreach (var kvp in _clients)
                    if (kvp.Key != excludeId)
                        snapshot.Add(kvp.Value);
            }

            foreach (var c in snapshot)
                SendTo(message, c.EndPoint);
        }

        /* ------------------------------------------------------------------ */
        /* Dispose                                                              */
        /* ------------------------------------------------------------------ */

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _disposedValue = true;

                if (disposing)
                {
                    _discoveryServer?.Dispose();
                    _client?.Dispose();
                    _logger?.LogInfo("Server stopped.");
                }
                _client          = null;
                _logger          = null;
                _started         = false;
                ServerStopped?.Invoke(this, new ServerStoppedArgs(_reason));
            }
        }

        ~Server() { Dispose(false); }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /* ------------------------------------------------------------------ */
        /* Factory                                                              */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Creates and starts a new server instance.
         *
         * @param[in] port        UDP port to bind (0 = OS-assigned).
         * @param[in] logger      Optional logger.
         * @param[in] serverName  Name broadcast to the LAN server browser.
         * @param[in] maxPlayers  Maximum connected clients.
         * @param[in] gameMode    Game-mode string broadcast to the browser.
         *
         * @returns Server instance on success, null on failure.
         */
        public static Server Start(int port = 0, ILogger logger = null,
                                   string serverName = null, int maxPlayers = 12,
                                   string gameMode = "Standard")
        {
            if (_started)
                return null;
            _started = true;
            try
            {
                return new Server(port, logger, serverName, maxPlayers, gameMode);
            }
            catch (Exception e)
            {
                logger?.LogFatal(e.Message);
                _started = false;
                return null;
            }
        }
    }
}

/* EOF */
