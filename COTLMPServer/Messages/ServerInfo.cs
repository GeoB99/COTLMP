/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Server info broadcasting
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using System;
using System.IO;
using System.Net;
using System.Text;
using static COTLMPServer.Data.GameModes;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer.Messages
{
    public class ServerInfo
    {
        public readonly string ServerName;
        public readonly GameMode Mode;
        public readonly int MaxPlayers;
        public readonly int ActivePlayers;

        /// <summary>
        /// The magic number to be used for verification when sent over the network
        /// </summary>
        private const int MagicNumber = 0xCEB7211;

        /// <summary>
        /// The minimum amount of bytes the structure will take up serialized
        /// </summary>
        private const int SerializedSize = (sizeof(int) * 5) + 4;

        public ServerInfo(string serverName, GameMode mode, int maxPlayers, int activePlayers)
        {
            if (string.IsNullOrEmpty(serverName) ||
                !Enum.IsDefined(typeof(GameMode), mode) ||
                maxPlayers > 12 ||
                activePlayers > 12)
            {
                throw new ArgumentException("One of the parameters are not valid!");
            }

            ServerName = serverName;
            Mode = mode;
            MaxPlayers = maxPlayers;
            ActivePlayers = activePlayers;
        }

        /// <summary>
        /// Serializes the server info data into an array of bytes.
        /// </summary>
        /// <returns>
        /// The resulting byte array.
        /// </returns>
        public byte[] Serialize()
        {
            byte[] NameBytes;

            using (MemoryStream Stream = new MemoryStream())
            using (BinaryWriter Writer = new BinaryWriter(Stream))
            {
                Writer.Write(MagicNumber);

                NameBytes = Encoding.UTF8.GetBytes(ServerName);
                Writer.Write(NameBytes.Length);
                Writer.Write(NameBytes);

                Writer.Write((int)Mode);
                Writer.Write(MaxPlayers);
                Writer.Write(ActivePlayers);
                return Stream.ToArray();
            }
        }

        /// <summary>
        /// Deserializes the byte array back into a server info object.
        /// </summary>
        /// <param name="Data">
        /// The byte array to be processed
        /// </param>
        /// <returns>
        /// The resulting object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// When data is null.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// When the data contained in the byte array is invalid.
        /// </exception>
        public static ServerInfo Deserialize(byte[] Data)
        {
            byte[] NameBytes;
            string Name;
            int MaxPls, ActivePls;
            GameMode gameMode;

            if (Data == null)
            {
                throw new ArgumentNullException(nameof(Data));
            }

            if (Data.Length < SerializedSize)
            {
                throw new InvalidDataException("Data too small!");
            }

            using (MemoryStream Stream = new MemoryStream(Data, false))
            using (BinaryReader Reader = new BinaryReader(Stream))
            {
                if (Reader.ReadInt32() != MagicNumber)
                {
                    throw new InvalidDataException("Magic number not matching!");
                }

                NameBytes = Utils.ReadBytes(Reader) ?? throw new InvalidDataException("Invalid server name data");
                if (NameBytes.Length > 40)
                {
                    throw new InvalidDataException("Server name too long!");
                }

                Name = Encoding.UTF8.GetString(NameBytes);
                gameMode = (GameMode)Reader.ReadInt32();

                MaxPls = Reader.ReadInt32();
                ActivePls = Reader.ReadInt32();
                return new ServerInfo(Name, gameMode, MaxPls, ActivePls);
            }
        }
    }
}

/* EOF */
