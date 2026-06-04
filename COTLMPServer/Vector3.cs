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

namespace COTLMPServer
{
    /// <summary>
    ///     Represents a point in 3d space
    /// </summary>
    public readonly struct Vector3
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        /// <summary>
        /// The minimum amount of bytes the structure will take up serialized
        /// </summary>
        /// <remarks>
        /// It is only the minimum, the structure may be larger than this when serialized.
        /// </remarks>
        public const int SerializedSize = (sizeof(float) * 3) + sizeof(int);

        /// <summary>
        /// The magic number to be used for verification when sent over the network
        /// </summary>
        public const int MagicNumber = 0xDE33789;

        /// <summary>
        /// Serialize the object into a byte array
        /// </summary>
        /// <returns>
        /// The resulting byte array
        /// </returns>
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

        /// <summary>
        /// Deserializes the byte array back into an object
        /// </summary>
        /// <param name="data">
        /// The byte array to be processed
        /// </param>
        /// <param name="offset">
        /// The offset to use when processing the byte array
        /// </param>
        /// <param name="after">
        /// The offset that follows immediately after the object in the array
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// When data is null
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// When the data contained in the byte array is invalid
        /// </exception>
        /// <remarks>
        /// This is the only Deserialize() method to accept an offset and after parameter because a vector3 can be encountered in contexts where you might want to read it from the middle of a byte array
        /// </remarks>
        /// <returns>
        /// The resulting object
        /// </returns>
        public static Vector3 Deserialize(byte[] data, int offset, out int after)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length - offset < SerializedSize)
                throw new InvalidDataException("Data is too small!");

            using (MemoryStream stream = new MemoryStream(data, offset, SerializedSize, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != MagicNumber)
                    throw new InvalidDataException("Magic number doesn't match");
                after = offset + SerializedSize;
                return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
        }

        /// <summary>
        /// The constructor of the struct
        /// </summary>
        /// <param name="x">
        /// The x coordinate
        /// </param>
        /// <param name="y">
        /// The y coordinate
        /// </param>
        /// <param name="z">
        /// The z coordinate
        /// </param>
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}

/* EOF */
