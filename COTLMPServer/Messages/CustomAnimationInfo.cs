/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define HandshakeClient class
 * COPYRIGHT:	Copyright 2026 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using System;
using System.IO;
using System.Text;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer.Messages
{
    /// <summary>
    /// The handshake message that the client sends
    /// </summary>
    public class CustomAnimationInfo
    {
        public uint ID;
        public bool Loop;
        public string Name;
        public Vector3 Position;
        /// <summary>
        /// The magic number to be used for verification when sent over the network
        /// </summary>
        public const int MagicNumber = 0xAFE3423;
        /// <summary>
        /// The minimum amount of bytes the structure will take up serialized
        /// </summary>
        public const int SerializedSize = sizeof(int) * 3 + 2 + Vector3.SerializedSize;

        /// <summary>
        /// Serialize the object into a byte array
        /// </summary>
        /// <returns>
        /// The resulting byte array
        /// </returns>
        /// <exception cref="InvalidDataException"/>
        public byte[] Serialize()
        {
            if (string.IsNullOrEmpty(Name))
                throw new InvalidDataException("Name is null or empty!");

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(MagicNumber);
                writer.Write(ID);
                writer.Write(Loop);
                byte[] nameBytes = Encoding.UTF8.GetBytes(Name);
                writer.Write(nameBytes.Length);
                writer.Write(nameBytes);
                byte[] vectorBytes = Position.Serialize();
                writer.Write(vectorBytes.Length);
                writer.Write(vectorBytes);
                return stream.GetBuffer();
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
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidDataException"/>
        public static CustomAnimationInfo Deserialize(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length < SerializedSize)
                throw new InvalidDataException("data too small!");

            using (MemoryStream stream = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != MagicNumber)
                    throw new InvalidDataException("Magic number mismatch");
                uint id = reader.ReadUInt32();
                bool loop = reader.ReadBoolean();
                byte[] nameBytes = Utils.ReadBytes(reader) ?? throw new InvalidDataException("Corrupt name string");
                byte[] vectorBytes = Utils.ReadBytes(reader) ?? throw new InvalidDataException("Corrupt position bytes");
                return new CustomAnimationInfo(Encoding.UTF8.GetString(nameBytes), id, loop, Vector3.Deserialize(vectorBytes, 0, out _));
            }
        }

        public CustomAnimationInfo(string name, uint id = 0, bool loop = false, Vector3 pos = new Vector3())
        {
            Name = name;
            ID = id;
            Loop = loop;
            Position = pos;
        }
    }
}

/* EOF */
