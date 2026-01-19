/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the Message class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * Contains all classes/structs associated with network messages
 */
namespace COTLMPServer.Messages
{
    /**
     * @brief
     * Message header
     * 
     * @field Type
     * Message type
     * 
     * @field Data
     * Actual message data (depends on the message type)
     * 
     * @field MagicNumber
     * Magic number to identify messages
     */
    public sealed class Message
    {
        public MessageType Type;
        public byte[] Data;
        public const int MagicNumber = 0x173495;

        /**
         * @brief
         * Serialize the message into a byte array
         * 
         * @return
         * A byte array that represents the message
         * 
         * @remarks
         * Type must be a value that the MessageType enum defines
         * Data can be null or an empty array
         * 
         * @throws InvalidDataException
         * If any of the data in the class is invalid
         */
        public byte[] Serialize()
        {
            if (!Enum.IsDefined(typeof(MessageType), Type))
                throw new InvalidDataException("Message type is not defined in the enum");
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream)) // ensure the stream and writer are disposed of once no longer needed
            {
                writer.Write(MagicNumber);
                writer.Write((int)Type);
//                writer.Write(ID);
                if (Data?.Length > 0) // check if data is null or zero length
                {
                    writer.Write(Data.Length);
                    writer.Write(Data);
                }
                else
                    writer.Write(-1);
                return stream.ToArray();
            }
        }

        /**
         * @brief
         * Deserialize byte array back into a Message object
         * 
         * @param[in] data
         * The byte array
         * 
         * @return
         * The resulting Message object
         * 
         * @throws InvalidDataException
         * If the data in the array is invalid
         * 
         * @throws ArgumentNullException
         * If any of the arguments are null
         */
        public static Message Deserialize(IReadOnlyList<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException("data is null!");
            if (data.Count < (sizeof(int) * 3))
                throw new InvalidDataException("data is too small!");

            using (MemoryStream stream = new MemoryStream(data.ToArray()))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != MagicNumber)
                    throw new InvalidDataException("data is not a message");
                MessageType type = (MessageType)reader.ReadInt32();
                if (!Enum.IsDefined(typeof(MessageType), type))
                    throw new InvalidDataException("Invalid message type");
                return new Message()
                {
                    Type = type,
//                    ID = reader.ReadInt32(),
                    Data = Utils.ReadBytes(stream)
                };
            }
        }
    }
}

/* EOF */
