/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define HandshakeServer class
 * COPYRIGHT:	Copyright 2026 Neco-Arc <neco-arc@inbox.ru>
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace COTLMPServer.Messages
{
    public class HandshakeServer
    {
        public readonly struct Player
        {
            public readonly int ID;
            public readonly int Skin;
            public readonly string Username;
            public readonly PlayerState State;

            public Player(PlayerState state, string username = "", int id = -1, int skin = 0)
            {
                ID = id;
                State = state;
                Skin = skin;
                Username = username;
            }

            public byte[] Serialize()
            {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(ID);
                    writer.Write(Skin);
                    byte[] userbytes = Encoding.UTF8.GetBytes(Username);
                    writer.Write(userbytes.Length);
                    writer.Write(userbytes);
                    byte[] bytes = State.Serialize();
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                    return stream.ToArray();
                }
            }

            public static Player Deserialize(IReadOnlyList<byte> bytes)
            {
                if (bytes == null)
                    throw new ArgumentNullException(nameof(bytes));
                if (bytes.Count < sizeof(int) * 3)
                    throw new InvalidDataException("data too small!");

                byte[] data = bytes as byte[] ?? bytes.ToArray();

                using (MemoryStream stream = new MemoryStream(data, false))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int id = reader.ReadInt32();
                    int skin = reader.ReadInt32();
                    byte[] userbytes = Utils.ReadBytes(reader) ?? throw new InvalidDataException("invalid data");
                    byte[] statebytes = Utils.ReadBytes(reader) ?? throw new InvalidDataException("invalid data");
                    return new Player(PlayerState.Deserialize(statebytes), Encoding.UTF8.GetString(userbytes), id, skin);
                }
            }

            internal static Player FromInternal(COTLMPServer.Player source)
            {
                return new Player(source.State, source.Username, source.ID, source.Skin);
            }
        }

        public int ID;
        public string Message;
        public Player[] Players;
        public const int MagicNumber = 0x1B36458;

        internal HandshakeServer(Player[] players, string message = "", int id = -1)
        {
            ID = id;
            Message = message;
            Players = players;
        }

        public static HandshakeServer Deserialize(IReadOnlyList<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (bytes.Count < sizeof(int) * 4)
                throw new InvalidDataException("data too small!");

            byte[] data = bytes as byte[] ?? bytes.ToArray();

            using (MemoryStream stream = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != MagicNumber)
                    throw new InvalidDataException("magic number mismatch");
                int id = reader.ReadInt32();
                byte[] msg = Utils.ReadBytes(reader);
                string strmsg = msg == null ? null : Encoding.UTF8.GetString(msg);
                List<Player> players = new List<Player>();
                for (int i = 0; i < reader.ReadInt32(); ++i)
                {
                    byte[] plrbytes = Utils.ReadBytes(reader) ?? throw new InvalidDataException("corrupt player entry");
                    players.Add(Player.Deserialize(plrbytes));
                }
                return new HandshakeServer(players.ToArray(), strmsg, id);
            }
        }

        public byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(MagicNumber);
                writer.Write(ID);
                byte[] bytes = Encoding.UTF8.GetBytes(Message);
                writer.Write(bytes.Length);
                writer.Write(bytes);
                writer.Write(Players.Length);
                foreach (var player in Players)
                {
                    byte[] ser = player.Serialize();
                    writer.Write(ser.Length);
                    writer.Write(ser);
                }
                return stream.ToArray();
            }
        }
    }
}
