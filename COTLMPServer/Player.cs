/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the Player class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

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
     */
    internal class Player
    {
        public uint ID;
        public PlayerState State;
        public CancellationTokenSource Cancellation;

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
        public Player(uint id, PlayerState state, CancellationTokenSource cancellation)
        {
            ID = id;
            State = state;
            Cancellation = cancellation;
        }
    }
}
