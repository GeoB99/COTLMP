using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace COTLMPServer.Messages
{
    public readonly struct PlayerInfo
    {
        public readonly int ID;
        public readonly int Skin;
        public readonly string Username;
        public readonly PlayerState State;

        public PlayerInfo(PlayerState state, string username = "", int id = -1, int skin = 0)
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

        public static PlayerInfo Deserialize(IReadOnlyList<byte> bytes)
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
                return new PlayerInfo(PlayerState.Deserialize(statebytes), Encoding.UTF8.GetString(userbytes), id, skin);
            }
        }

        internal static PlayerInfo FromInternal(COTLMPServer.Player source)
        {
            return new PlayerInfo(source.State, source.Username, source.ID, source.Skin);
        }
    }
}
