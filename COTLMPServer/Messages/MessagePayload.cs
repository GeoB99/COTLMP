/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Encode and decode typed message payloads
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using System.IO;
using System.Text;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer.Messages
{
    /**
     * @brief
     * Static helpers to encode and decode the Data field of a Message for each
     * specific MessageType.  All strings are length-prefixed UTF-8.
     */
    public static class MessagePayload
    {
        /* ------------------------------------------------------------------ */
        /* PlayerJoin                                                           */
        /* ------------------------------------------------------------------ */

        public static byte[] EncodePlayerJoin(string playerName)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                WriteString(w, playerName);
                return ms.ToArray();
            }
        }

        public static string DecodePlayerJoin(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var r = new BinaryReader(ms, Encoding.UTF8, true))
                return ReadString(r);
        }

        /* ------------------------------------------------------------------ */
        /* PlayerJoinAck                                                        */
        /* ------------------------------------------------------------------ */

        public static byte[] EncodePlayerJoinAck(int assignedId, bool success)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(assignedId);
                w.Write(success);
                return ms.ToArray();
            }
        }

        public static void DecodePlayerJoinAck(byte[] data, out int assignedId, out bool success)
        {
            using (var ms = new MemoryStream(data))
            using (var r = new BinaryReader(ms, Encoding.UTF8, true))
            {
                assignedId = r.ReadInt32();
                success    = r.ReadBoolean();
            }
        }

        /* ------------------------------------------------------------------ */
        /* PlayerPosition                                                       */
        /* ------------------------------------------------------------------ */

        public static byte[] EncodePlayerPosition(float x, float y, float facingAngle)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(x);
                w.Write(y);
                w.Write(facingAngle);
                return ms.ToArray();
            }
        }

        public static void DecodePlayerPosition(byte[] data, out float x, out float y, out float facingAngle)
        {
            using (var ms = new MemoryStream(data))
            using (var r = new BinaryReader(ms, Encoding.UTF8, true))
            {
                x           = r.ReadSingle();
                y           = r.ReadSingle();
                facingAngle = r.ReadSingle();
            }
        }

        /* ------------------------------------------------------------------ */
        /* PlayerState                                                          */
        /* ------------------------------------------------------------------ */

        public static byte[] EncodePlayerState(int state)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(state);
                return ms.ToArray();
            }
        }

        public static int DecodePlayerState(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var r = new BinaryReader(ms, Encoding.UTF8, true))
                return r.ReadInt32();
        }

        /* ------------------------------------------------------------------ */
        /* PlayerHealth                                                         */
        /* ------------------------------------------------------------------ */

        public static byte[] EncodePlayerHealth(float hp, float totalHp)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(hp);
                w.Write(totalHp);
                return ms.ToArray();
            }
        }

        public static void DecodePlayerHealth(byte[] data, out float hp, out float totalHp)
        {
            using (var ms = new MemoryStream(data))
            using (var r = new BinaryReader(ms, Encoding.UTF8, true))
            {
                hp      = r.ReadSingle();
                totalHp = r.ReadSingle();
            }
        }

        /* ------------------------------------------------------------------ */
        /* Generic string payload (PlayerAnimation, ChatMessage, PlayerKick)   */
        /* ------------------------------------------------------------------ */

        public static byte[] EncodeStringPayload(string value)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                WriteString(w, value);
                return ms.ToArray();
            }
        }

        public static string DecodeStringPayload(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;
            using (var ms = new MemoryStream(data))
            using (var r = new BinaryReader(ms, Encoding.UTF8, true))
                return ReadString(r);
        }

        /* ------------------------------------------------------------------ */
        /* DiscoveryResponse                                                    */
        /* ------------------------------------------------------------------ */

        public static byte[] EncodeDiscoveryResponse(string serverName, int playerCount, int maxPlayers, int port, string gameMode)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                WriteString(w, serverName);
                w.Write(playerCount);
                w.Write(maxPlayers);
                w.Write(port);
                WriteString(w, gameMode);
                return ms.ToArray();
            }
        }

        public static void DecodeDiscoveryResponse(byte[] data,
            out string serverName, out int playerCount, out int maxPlayers,
            out int port, out string gameMode)
        {
            using (var ms = new MemoryStream(data))
            using (var r = new BinaryReader(ms, Encoding.UTF8, true))
            {
                serverName  = ReadString(r);
                playerCount = r.ReadInt32();
                maxPlayers  = r.ReadInt32();
                port        = r.ReadInt32();
                gameMode    = ReadString(r);
            }
        }

        /* ------------------------------------------------------------------ */
        /* Internal helpers                                                     */
        /* ------------------------------------------------------------------ */

        private static void WriteString(BinaryWriter w, string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                w.Write(0);
                return;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            w.Write(bytes.Length);
            w.Write(bytes);
        }

        private static string ReadString(BinaryReader r)
        {
            int len = r.ReadInt32();
            if (len <= 0) return string.Empty;
            return Encoding.UTF8.GetString(r.ReadBytes(len));
        }
    }
}

/* EOF */
