/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the PlayerInfo struct
 * COPYRIGHT:	Copyright 2026 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using System;
using System.IO;
using System.Text;
using COTLMPServer.Data;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer.Messages
{
    /// <summary>
    /// This struct represents all of the information about a player that a client needs, ready to send over the netrwork.
    /// </summary>
    /// <remarks>
    /// This struct doesn't have a magic number because the inner PlayerState object has one and the chances that if random junk is sent and everything aligns are very low
    /// </remarks>
    public readonly struct PlayerInfo
    {
        public readonly uint ID;
        public readonly int Skin;
        public readonly string Username;
        public readonly PlayerState State;

        /// <summary>
        /// The minimum amount of bytes the structure will take up serialized
        /// </summary>
        public const int SerializedSize = (sizeof(int) * 3) + 1;

        public PlayerInfo(PlayerState state, string username = "", uint id = 9999, int skin = 0)
        {
            ID = id;
            State = state;
            Skin = skin;
            Username = username;
        }

        /// <summary>
        /// Serialize the object into a byte array
        /// </summary>
        /// <returns>
        /// The resulting byte array
        /// </returns>
        /// <exception cref="InvalidDataException">
        /// If the data in the object is invalid
        /// </exception>
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

        /// <summary>
        /// Deserializes the byte array back into an object
        /// </summary>
        /// <param name="data">
        /// The byte array to be processed
        /// </param>
        /// <returns>
        /// The resulting object
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// When data is null
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// When the data contained in the byte array is invalid
        /// </exception>
        public static PlayerInfo Deserialize(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length < SerializedSize)
                throw new InvalidDataException("data too small!");

            using (MemoryStream stream = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                uint id = reader.ReadUInt32();
                int skin = reader.ReadInt32();
                byte[] userbytes = Utils.ReadBytes(reader) ?? throw new InvalidDataException("invalid data");
                if (userbytes.Length > 35)
                    throw new InvalidDataException("username too long!");
                byte[] statebytes = Utils.ReadBytes(reader) ?? throw new InvalidDataException("invalid data");
                return new PlayerInfo(PlayerState.Deserialize(statebytes), Encoding.UTF8.GetString(userbytes), id, skin);
            }
        }

        /// <summary>
        /// Convert the internal Player object into a network PlayerInfo
        /// </summary>
        /// <param name="source">
        /// The source Player object
        /// </param>
        /// <returns>
        /// The resulting PlayerInfo object
        /// </returns>
        internal static PlayerInfo FromInternal(Player source)
        {
            return new PlayerInfo(source.State, source.Username, source.ID, source.Skin);
        }
    }
}
