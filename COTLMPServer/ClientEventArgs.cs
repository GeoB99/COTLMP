/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     EventArgs for client join/leave events
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using System;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer
{
    /**
     * @brief
     * EventArgs that carry a ClientInfo snapshot for ClientJoined/ClientLeft
     */
    public sealed class ClientEventArgs : EventArgs
    {
        /**
         * @brief
         * Snapshot of the client that joined or left
         */
        public readonly ClientInfo Client;

        public ClientEventArgs(ClientInfo client)
        {
            Client = client;
        }
    }
}

/* EOF */
