/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the Player class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMPServer.Messages;
using System.Threading;

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * Contains the classes/structs/enums for the server
 */
namespace COTLMPServer
{
    /**
     * @brief
     * The server-side representation of a player
     * 
     * @field ID
     * The server-size player ID
     * 
     * @field State
     * The player state
     * 
     * @field Cancellation
     * The cancellation token for everything related to the player
     * 
     * @field Mutex
     * The mutex for this instance of the class
     */
    internal class Player
    {
        public int ID;
        public int Skin;
        public string Username;
        public string Biome;
        public PlayerState State;
        public CancellationTokenSource Cancellation;
        public object Lock;
        public bool Lag;
        public uint Sequence;

        /**
         * @brief
         * The constructor
         * 
         * @param[in] ID
         * The server-size player ID
         * 
         * @param[in] State
         * The player state
         * 
         * @param[in] Cancellation
         * The cancellation token for everything related to the player
         */
        public Player(int id, int skin, string username, string biome, PlayerState state, CancellationTokenSource cancellation)
        {
            ID = id;
            Skin = skin;
            State = state;
            Biome = biome;
            Username = username;
            Cancellation = cancellation;
            Lock = new object();
            Lag = false;
            Sequence = 2; // start at 2 because of handshake
        }
    }
}
