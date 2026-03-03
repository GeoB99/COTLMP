/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Client-side LAN server discovery via UDP broadcast
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using COTLMPServer;
using COTLMPServer.Messages;
using COTLMP.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Network
{
    /**
     * @brief
     * Sends a broadcast probe on the LAN discovery port and collects
     * ServerEntry responses for the duration of the scan window.
     *
     * Protocol matches LanDiscoveryServer:
     *   Request  -> int32 0x43534944 ("DISC")
     *   Response -> int32 0x54534944 ("DIST") + MessagePayload.DiscoveryResponse bytes
     */
    internal static class LanDiscovery
    {
        private const int MagicRequest  = 0x43534944;
        private const int MagicResponse = 0x54534944;

        /** How long (ms) to wait for responses after broadcasting */
        public const int ScanWindowMs = 2000;

        /* ------------------------------------------------------------------ */
        /* Async scan                                                           */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Runs a LAN scan on a background thread and invokes
         * <paramref name="onComplete"/> with the result list when done.
         *
         * @param[in] onComplete  Called on the thread-pool when scanning finishes.
         */
        public static void ScanAsync(Action<List<ServerEntry>> onComplete)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                List<ServerEntry> results;
                try   { results = Scan(); }
                catch { results = new List<ServerEntry>(); }
                try   { onComplete?.Invoke(results); }
                catch { }
            });
        }

        /* ------------------------------------------------------------------ */
        /* Synchronous scan (call from background thread only)                 */
        /* ------------------------------------------------------------------ */

        public static List<ServerEntry> Scan()
        {
            var results = new List<ServerEntry>();

            using (var udp = new UdpClient(0))
            {
                udp.EnableBroadcast = true;

                /* -- send broadcast probe -- */
                byte[] probe;
                using (var ms = new MemoryStream())
                using (var w  = new BinaryWriter(ms))
                {
                    w.Write(MagicRequest);
                    probe = ms.ToArray();
                }
                udp.Send(probe, probe.Length, new IPEndPoint(IPAddress.Broadcast, LanDiscoveryServer.DiscoveryPort));

                /* Also probe the loopback address so the host finds its own server */
                try
                {
                    udp.Send(probe, probe.Length, new IPEndPoint(IPAddress.Loopback, LanDiscoveryServer.DiscoveryPort));
                }
                catch { /* best-effort */ }

                /* -- collect responses for ScanWindowMs -- */
                udp.Client.ReceiveTimeout = 300; // short per-receive timeout
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(ScanWindowMs);

                while (DateTime.UtcNow < deadline)
                {
                    IPEndPoint from = null;
                    byte[] response;
                    try
                    {
                        response = udp.Receive(ref from);
                    }
                    catch (SocketException)
                    {
                        continue; // timeout on this receive; loop until deadline
                    }

                    if (response == null || response.Length < 8) continue;

                    using (var ms = new MemoryStream(response))
                    using (var r  = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                    {
                        if (r.ReadInt32() != MagicResponse) continue;

                        byte[] payload = r.ReadBytes((int)(ms.Length - ms.Position));
                        try
                        {
                            MessagePayload.DecodeDiscoveryResponse(payload,
                                out string name, out int players, out int maxP,
                                out int port, out string mode);

                            results.Add(new ServerEntry
                            {
                                Name        = name,
                                Address     = from.Address,
                                Port        = port,
                                PlayerCount = players,
                                MaxPlayers  = maxP,
                                GameMode    = mode
                            });
                        }
                        catch { /* malformed response */ }
                    }
                }
            }

            return results;
        }
    }
}

/* EOF */
