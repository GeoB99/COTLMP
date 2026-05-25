/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Globals data of the mod
 * COPYRIGHT:	Copyright 2025-2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Data
{
    /// <summary>
    /// Main class of which mod data is stored.
    /// </summary>
    internal sealed class ModDataGlobals
    {
        /// <summary>
        /// Enable or Disable the execution of the mod.
        /// </summary>
        public bool EnableMod;

        /// <summary>
        /// The current executing game-play mode.
        /// </summary>
        public string GameMode;

        /// <summary>
        /// The name of the player in-game
        /// </summary>
        public string PlayerName;

        /// <summary>
        /// The name of the server.
        /// </summary>
        public string ServerName;

        /// <summary>
        /// The maximum allowed number of players.
        /// </summary>
        public int MaxNumPlayers;

        /// <summary>
        /// Enable or Disable voice chat.
        /// </summary>
        public bool EnableVoiceChat;

        /// <summary>
        /// The password of the server.
        /// </summary>
        public string ServerPassowrd;

        /// <summary>
        /// Should the server be protected with a password or not upon joining.
        /// </summary>
        public bool ProtectServer;

        public ModDataGlobals(bool Enable,
                              string Mode,
                              string PlName,
                              string SvName,
                              int PlNum,
                              bool EnableVC,
                              string Pw,
                              bool Protect)
        {
            EnableMod = Enable;
            GameMode = Mode;
            PlayerName = PlName;
            ServerName = SvName;
            MaxNumPlayers = PlNum;
            EnableVoiceChat = EnableVC;
            ServerPassowrd = Pw;
            ProtectServer = Protect;
        }
    }
}

/* EOF */
