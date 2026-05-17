/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Main entry point dedicated server launcher
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using System;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer.DedicatedServer
{
    internal static class Launcher
    {
        /// <summary>
        /// Main entry point for COTLMP Standalone Dedicated Server.
        /// </summary>
        /// <param name = "args">Command line arguments for the server.</param>
        public static void Main(string[] args)
        {
            COTLMPServer.DedicatedServer.Cli.Initialize(args);
        }
    }
}

/* EOF */
