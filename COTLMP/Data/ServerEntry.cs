/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Data class representing a discovered server
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using System.Net;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Data
{
    /**
     * @brief
     * A server entry discovered via LAN broadcast or direct-connect attempt,
     * used to populate the server browser.
     */
    public sealed class ServerEntry
    {
        /** Human-readable server name */
        public string     Name;

        /** Game mode string (e.g. "Standard") */
        public string     GameMode;

        /** IP address of the host */
        public IPAddress  Address;

        /** UDP game port */
        public int        Port;

        /** Current connected player count */
        public int        PlayerCount;

        /** Maximum allowed players */
        public int        MaxPlayers;

        /**
         * @brief
         * One-line display string for the server list, e.g.
         * "My Server  (3/12)  Standard  192.168.1.5:7777"
         */
        public string DisplayLine =>
            $"{Name}   ({PlayerCount}/{MaxPlayers})   {GameMode}   {Address}:{Port}";
    }
}

/* EOF */
