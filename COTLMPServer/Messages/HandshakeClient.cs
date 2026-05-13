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
     * @field Skin
     * The skin that the client is using
     * 
     * @field Username
     * The client's username (max. 35 characters)
     * 
     * @field GameVersion
     * The client's game version (max. 45 characters)
     * 
     * @field SerializedSize
     * The minimum amount of bytes the structure will take up serialized
     * 
     * @field MagicNumber
     * The magic number to be used for verification when sent over the network
     */
    public class HandshakeClient
    {
        public int Skin;
        public string Username;
        public string GameVersion;
        public const int SerializedSize = (sizeof(int) * 4) + 2;
        public const int MagicNumber = 0xBE12475;

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
        public static HandshakeClient Deserialize(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length < SerializedSize)
                throw new InvalidDataException("Data too small");

            using(MemoryStream stream = new MemoryStream(data, false))
            using(BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != MagicNumber)
                    throw new InvalidDataException("Magic number doesn't match");
                int skin = reader.ReadInt32();

                byte[] userBytes = Utils.ReadBytes(reader) ?? throw new InvalidDataException("Corrupt username string");
                if (userBytes.Length > 35)
                    throw new InvalidDataException("Username string too long");
                string user = Encoding.UTF8.GetString(userBytes);

                byte[] verBytes = Utils.ReadBytes(reader) ?? throw new InvalidDataException("Corrupt version string");
                if (verBytes.Length > 45)
                    throw new InvalidDataException("Version string too long");
                string ver = Encoding.UTF8.GetString(verBytes);

                if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(ver) || skin < 0)
                    throw new InvalidDataException("invalid data in bytes array");

                return new HandshakeClient(user, ver, skin);
            }
        }

        /**
         * @brief
         * The constructor for HandshakeClient
         * 
         * @param[in] username
         * The username to send
         * 
         * @param[in] version
         * The game version to send
         * 
         * @param[in] skin
         * The skin id to send
         */
        public HandshakeClient(string username, string version, int skin = 0)
        {
            Username = username;
            Skin = skin;
            GameVersion = version;
        }
    }
}
