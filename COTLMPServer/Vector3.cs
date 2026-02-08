/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the Vector3 enum
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
 * Contains the classes/structs/enums for the server
 */
namespace COTLMPServer
{
    /**
     * @brief
     * Represents a point in 3d space
     * 
     * @field X
     * The X coordinate
     * 
     * @field Y
     * The Y coordinate
     * 
     * @field Z
     * The Z coordinate
     * 
     * @field MagicNumber
     * The magic number to be used for verification when sent over the network
     */
    public readonly struct Vector3
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public const int MagicNumber = 0xDE33789;

        /**
         * @brief
         * Serialize the object into a byte array
         * 
         * @returns
         * The resulting byte array
         */
        public byte[] Serialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(MagicNumber);
                writer.Write(X);
                writer.Write(Y);
                writer.Write(Z);
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
        public static Vector3 Deserialize(IReadOnlyList<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Count < (sizeof(float) * 3 + sizeof(int)))
                throw new InvalidDataException("Data is too small!");

            byte[] buffer = data as byte[] ?? data.ToArray();

            using (MemoryStream stream = new MemoryStream(buffer, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != MagicNumber)
                    throw new InvalidDataException("Magic number doesn't match");
                return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
        }

        /**
         * @brief
         * The constructor of the struct
         * 
         * @param[in] x
         * The x coordinate
         * 
         * @param[in] y
         * The y coordinate
         * 
         * @param[in] z
         * The z coordinate
         */
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}

/* EOF */
