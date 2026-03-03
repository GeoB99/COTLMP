/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Represents a connected client on the server
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using System;
using System.Net;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer
{
    /**
     * @brief
     * Stores per-client state tracked by the server
     *
     * @field ID
     * Unique integer ID assigned on join
     *
     * @field EndPoint
     * Remote UDP endpoint of this client
     *
     * @field PlayerName
     * Display name supplied by the client on join
     *
     * @field LastSeen
     * UTC timestamp of the last received heartbeat or data packet
     */
    public sealed class ClientInfo
    {
        public int        ID;
        public IPEndPoint EndPoint;
        public string     PlayerName;
        public DateTime   LastSeen;

        public ClientInfo(int id, IPEndPoint endPoint, string playerName)
        {
            ID         = id;
            EndPoint   = endPoint;
            PlayerName = playerName;
            LastSeen   = DateTime.UtcNow;
        }
    }
}

/* EOF */
