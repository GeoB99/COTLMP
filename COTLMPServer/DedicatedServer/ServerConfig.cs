/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Main entry point dedicated server launcher
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using System;
using Newtonsoft.Json;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer.DedicatedServer
{
    /// <summary>
    /// Server configuration class used to denote a JSON object containing server data.
    /// </summary>
    internal class ServerConfig
    {
        [JsonProperty]
        internal ushort PortNumber { get; set; }

        [JsonProperty]
        internal string ServerName { get; set; }

        [JsonProperty]
        internal uint MaxPlayers { get; set; }

        [JsonProperty]
        internal string Password { get; set; }

        [JsonProperty]
        internal string GameMode { get; set; }
    }
}

/* EOF */
