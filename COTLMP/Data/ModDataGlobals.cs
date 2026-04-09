/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Globals data of the mod
 * COPYRIGHT:	Copyright 2025-2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains global mod data.
 *
 * @class ModDataGlobals
 * Main class of which mod data is stored.
 */
namespace COTLMP.Data
{
    internal sealed class ModDataGlobals
    {
        /* Enable or Disable the execution of the mod */
        public bool EnableMod;

        /* The current executing game-play mode */
        public string GameMode;

        /* The name of the player in-game */
        public string PlayerName;

        /* The name of the server */
        public string ServerName;

        /* The maximum allowed number of players */
        public int MaxNumPlayers;

        /* Enable or Disable voice chat */
        public bool EnableVoiceChat;

        /* The password of the server */
        public string ServerPassowrd;

        /* Should the server be protected with a password or not upon joining */
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
