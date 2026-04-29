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

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * Contains the classes/structs/enums for the server
 */
namespace COTLMPServer.Messages
{
    /**
     * @brief
     * This struct represents all of the information about a player.
     * 
     * @field ID
     * The player ID
     * 
     * @field Skin
     * The skin that the player is using
     * 
     * @field Username
     * The player's username (max. 35 characters)
     * 
     * @field State
     * The player state
     * 
     * @field SerializedSize
     * The minimum amount of bytes the structure will take up serialized
     * 
     * @remarks
     * This struct doesn't have a magic number because the inner PlayerState object has one and the chances that if random junk is sent and everything aligns
     * are very low
     */
    public readonly struct PlayerInfo
    {
        public readonly uint ID;
        public readonly int Skin;
        public readonly string Username;
        public readonly PlayerState State;
        public const int SerializedSize = (sizeof(int) * 3) + 1;
        
        /**
         * @brief
         * The struct contstructor
         * 
         * @param[in] state
         * The PlayerState object to send
         * 
         * @param[in] username
         * The username to send
         * 
         * @param[in] id
         * The player ID to send
         * 
         * @param[in] skin
         * The player skin to send
         */
        public PlayerInfo(PlayerState state, string username = "", uint id = 9999, int skin = 0)
        {
            ID = id;
            State = state;
            Skin = skin;
            Username = username;
        }

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

        /**
         * @brief
         * Convert the internal Player object into a network PlayerInfo to send it
         * 
         * @param[in] source
         * The source Player object
         * 
         * @returns
         * The resulting PlayerInfo object
         */
        internal static PlayerInfo FromInternal(COTLMPServer.Player source)
        {
            return new PlayerInfo(source.State, source.Username, source.ID, source.Skin);
        }
    }
}
