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

/**
 * @brief
 * Contains all classes/structs associated with network messages
 */
namespace COTLMPServer.Messages
{
    /**
     * @brief
     * The handshake message that the client sends
     * 
     * @field ID
     * The player ID
     * 
     * @field Loop
     * Whether the animation should loop
     * 
     * @field Name
     * The name of the animation
     * 
     * @field SerializedSize
     * The minimum amount of bytes the structure will take up serialized
     * 
     * @field MagicNumber
     * The magic number to be used for verification when sent over the network
     */
    public class CustomAnimationInfo
    {
        public uint ID;
        public bool Loop;
        public string Name;
        public Vector3 Position;
        public const int MagicNumber = 0xAFE3423;
        public const int SerializedSize = sizeof(int) * 3 + 2 + Vector3.SerializedSize;

        /**
         * @brief
         * Serialize the object into a byte array
         * 
         * @returns
         * The resulting byte array
         * 
         * @throws InvalidDataException
         * If the data in the object is invalid
         */
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

        /**
         * @brief
         * Deserializes the byte array back into an object
         * 
         * @param[in] data
         * The byte array to be processed
         * 
         * @returns
         * The resulting object
         * 
         * @throws ArgumentNullException
         * When data is null
         * 
         * @throws InvalidDataException
         * When the data contained in the byte array is invalid
         */
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

        /**
         * @brief
         * The constructor for CustomAnimationInfo
         * 
         * @param[in] name
         * The name of the animation
         * 
         * @param[in] id
         * The player ID
         * 
         * @param[in] loop
         * Whether to loop the animation
         */
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
