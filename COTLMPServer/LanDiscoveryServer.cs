/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     LAN UDP broadcast discovery listener
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using COTLMPServer.Messages;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer
{
    /**
     * @brief
     * Listens on DiscoveryPort for LAN broadcast probe packets and responds
     * with server metadata so clients can populate the server browser.
     *
     * Protocol (both frames are raw UDP, NOT wrapped in the Message header):
     *   Request  -> 4-byte magic 0x44495343 ("DISC")
     *   Response -> 4-byte magic 0x44495354 ("DIST") + MessagePayload.DiscoveryResponse bytes
     */
    public sealed class LanDiscoveryServer : IDisposable
    {
        /** Port used exclusively for LAN discovery broadcasts */
        public const int DiscoveryPort = 7779;

        /** Request magic: ASCII "DISC" as little-endian int32 */
        private const int MagicRequest  = 0x43534944;

        /** Response magic: ASCII "DIST" as little-endian int32 */
        private const int MagicResponse = 0x54534944;

        private UdpClient      _client;
        private bool           _disposed;
        private readonly string _serverName;
        private readonly int    _gamePort;
        private readonly int    _maxPlayers;
        private readonly string _gameMode;
        private readonly Func<int> _getPlayerCount;

        public LanDiscoveryServer(string serverName, int gamePort, int maxPlayers,
                                   string gameMode, Func<int> getPlayerCount)
        {
            _serverName     = serverName ?? "COTL Server";
            _gamePort       = gamePort;
            _maxPlayers     = maxPlayers;
            _gameMode       = gameMode ?? "Standard";
            _getPlayerCount = getPlayerCount;

            _client = new UdpClient(DiscoveryPort);
            _client.EnableBroadcast = true;
            StartReceive();
        }

        private void StartReceive()
        {
            if (_disposed) return;
            try { _client.BeginReceive(OnReceive, null); }
            catch { /* server shutting down */ }
        }

        private void OnReceive(IAsyncResult result)
        {
            if (_disposed) return;

            IPEndPoint from = null;
            byte[] data;
            try
            {
                data = _client.EndReceive(result, ref from);
            }
            catch
            {
                if (!_disposed) StartReceive();
                return;
            }

            if (data != null && data.Length >= 4)
            {
                using (var ms = new MemoryStream(data))
                using (var r  = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    if (r.ReadInt32() == MagicRequest)
                        SendDiscoveryResponse(from);
                }
            }

            StartReceive();
        }

        private void SendDiscoveryResponse(IPEndPoint to)
        {
            try
            {
                int   playerCount = _getPlayerCount?.Invoke() ?? 0;
                byte[] payload    = MessagePayload.EncodeDiscoveryResponse(
                    _serverName, playerCount, _maxPlayers, _gamePort, _gameMode);

                using (var ms = new MemoryStream())
                using (var w  = new BinaryWriter(ms))
                {
                    w.Write(MagicResponse);
                    w.Write(payload);
                    byte[] frame = ms.ToArray();
                    _client.Send(frame, frame.Length, to);
                }
            }
            catch { /* best-effort */ }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _client?.Close(); } catch { }
            _client = null;
        }
    }
}

/* EOF */
