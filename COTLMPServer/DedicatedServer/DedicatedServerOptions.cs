/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Dedicated server command line options
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using System;
using CommandLine;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer.DedicatedServer
{
    /// <summary>
    /// Options class that contains the dedicated server command line options to be
    /// passed to the dedicated server command line interface.
    /// </summary>
    internal class DedicatedServerOptions
    {
        [Value(0, Required = false, HelpText = "Port number used to create and estabilish server connection.")]
        public ushort PortNumber { get; set; }

        [Option('n', "Name", Required = false, HelpText = "The name of the server used to setup the dedicated server.")]
        public string ServerName { get; set; }

        [Option('c', "Count", Required = false, HelpText = "Number of allowed players to join the server.")]
        public uint MaxPlayers { get; set; }

        [Option('p', "Password", Required = false, HelpText = "(Optional) Setup a password to protect the server from players joining your server.")]
        public string Password { get; set; }

        [Option('g', "Game", Required = false, HelpText = "(Optional) The game-mode to use when creating the server. The following supported game modes are: Standard, Deathmatch, Boss Fight and Zombies (only Standard is the supported game mode at the moment). Default = Standard.")]
        public string GameMode { get; set; }
    }
}

/* EOF */
