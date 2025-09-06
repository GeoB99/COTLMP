/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Globals data of the mod
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
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
    internal class ModDataGlobals
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

        public ModDataGlobals(bool Enable,
                              string Mode,
                              string PlName,
                              string SvName,
                              int PlNum,
                              bool EnableVC)
        {
            EnableMod = Enable;
            GameMode = Mode;
            PlayerName = PlName;
            ServerName = SvName;
            MaxNumPlayers = PlNum;
            EnableVoiceChat = EnableVC;
        }
    }
}

/* EOF */
