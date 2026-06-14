/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define utility methods
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using System;
using System.IO;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer
{
    /// <summary>
    /// Contains static utility methods
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Read a byte array that is prefixed by its size from a stream
        /// </summary>
        /// <returns>
        /// The read byte array or null if the size is 0
        /// </returns>
        /// <param name="reader">
        /// The binary reader to use
        /// </param>
        /// <exception cref="InvalidDataException">
        /// If the data passed to the method is invalid
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If any of the arguments are null
        /// </exception>
        public static byte[] ReadBytes(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (reader.BaseStream.Length - reader.BaseStream.Position < sizeof(int))
                throw new InvalidDataException("stream is too small!");

            int size = reader.ReadInt32();
            if (size > 0 && size < 1500)
            {
                if (size > reader.BaseStream.Length - reader.BaseStream.Position)
                    throw new InvalidDataException($"Expected {size} bytes, got {reader.BaseStream.Length - reader.BaseStream.Position}");
                return reader.ReadBytes(size);
            }
            else
                return null;
        }

        /// <summary>
        /// Reverse the endianness of a uint
        /// </summary>
        /// <param name="val">
        /// The integer to be reversed
        /// </param>
        /// <returns>
        /// val with its byte order reversed
        /// </returns>
        public static uint ReverseEndianness(uint val)
        {
            return ((val & 0x000000FFU) << 24 |
                    (val & 0x0000FF00U) << 8 |
                    (val & 0x00FF0000U) >> 8 |
                    (val & 0xFF000000U) >> 24);
        }
    }
}

/* EOF */

