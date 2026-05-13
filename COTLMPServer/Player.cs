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
     * 
     * @field Skin
     * The player skin
     * 
     * @field Username
     * The player username
     * 
     * @field Biome
     * The biome that the player is currently in
     * 
     * @field Lock
     * A lock object for the player
     * 
     * @field Lag
     * Whether the player is inresponsive or not
     * 
     * @field Sequence
     * The sequence number that should be used for the next message
     */
    internal class Player
    {
        public uint ID;
        public int Skin;
        public string Username;
        public string Biome;
        public PlayerState State;
        public CancellationTokenSource Cancellation;
        public readonly object Lock;
        public bool Lag;
        public uint Sequence;

        /**
         * @brief
         * The constructor
         * 
         * @param[in] id
         * The server-size player ID
         * 
         * @param[in] state
         * The player state
         * 
         * @param[in] cancellation
         * The cancellation token for everything related to the player
         * 
         * @param[in] skin
         * The player skin
         * 
         * @param[in] username
         * The player username
         * 
         * @param[in] biome
         * The player biome
         */
        public Player(uint id, int skin, string username, string biome, PlayerState state, CancellationTokenSource cancellation)
        {
            ID = id;
            Skin = skin;
            State = state;
            Biome = biome;
            Username = username;
            Cancellation = cancellation;
            Lock = new object();
            Lag = false;
            Sequence = 3; // start at 3 because of handshake
        }
    }
}
