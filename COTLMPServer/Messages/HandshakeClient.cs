/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define HandshakeClient class
 * COPYRIGHT:	Copyright 2026 Neco-Arc <neco-arc@inbox.ru>
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace COTLMPServer.Messages
{
    public class HandshakeClient
    {
        public int Skin;
        public string Username;
        public string GameVersion;
        public const int MagicNumber = 0xBE12475;

        public byte[] Serialize()
        {
            if (string.IsNullOrWhiteSpace(Username) || Skin < 0)
                throw new InvalidDataException("Invalid data in the object that is being serialized");

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(MagicNumber);
                writer.Write(Skin);
                byte[] userBytes = Encoding.UTF8.GetBytes(Username);
                writer.Write(userBytes.Length);
                writer.Write(userBytes);
                byte[] verBytes = Encoding.UTF8.GetBytes(GameVersion);
                writer.Write(verBytes.Length);
                writer.Write(verBytes);
                return stream.ToArray();
            }
        }

        public static HandshakeClient Deserialize(IReadOnlyList<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Count < (sizeof(int) * 4 + 2))
                throw new InvalidDataException("Data too small");

            byte[] buffer = data as byte[] ?? data.ToArray();

            using(MemoryStream stream = new MemoryStream(buffer, false))
            using(BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != MagicNumber)
                    throw new InvalidDataException("Magic number doesn't match");
                int skin = reader.ReadInt32();

                byte[] userBytes = Utils.ReadBytes(reader) ?? throw new InvalidDataException("Corrupt username string");
                string user = Encoding.UTF8.GetString(userBytes);

                byte[] verBytes = Utils.ReadBytes(reader) ?? throw new InvalidDataException("Corrupt version string");
                string ver = Encoding.UTF8.GetString(verBytes);

                if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(ver) || skin < 0)
                    throw new InvalidDataException("invalid data in bytes array");

                return new HandshakeClient(user, ver, skin);
            }
        }

        public HandshakeClient(string username, string version, int skin = 0)
        {
            Username = username;
            Skin = skin;
            GameVersion = version;
        }
    }
}
