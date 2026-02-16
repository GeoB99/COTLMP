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

/**
 * @brief
 * Contains all classes/structs/enums associated with network messages
 */
namespace COTLMPServer.Messages
{
    /**
     * @brief
     * Contains static utility methods
     */
    public static class Utils
    {
        /**
         * @brief
         * Read a byte array that is prefixed by its size from a stream
         * 
         * @return
         * The read byte array or null if the size is 0
         * 
         * @param[in] reader
         * The binary reader to use
         * 
         * @throws InvalidDataException
         * If the data passed to it is invalid
         * 
         * @throws ArgumentNullException
         * If any of the arguments are null
         */
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
    }
}

/* EOF */

