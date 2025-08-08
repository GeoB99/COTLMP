/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Globals data of the mod
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Data
{
    internal class ModDataGlobals
    {
        public bool EnableMod;
        public string GameMode;
        public string PlayerName;
        public string ServerName;
        public int MaxNumPlayers;
        public bool EnableVoiceChat;
        public bool EnableSayChat;

        internal bool VerboseDebug = false;
        internal const int MaxPlayersPerServerInternal = 8;

        public ModDataGlobals(bool Enable,
                              string Mode,
                              string PlName,
                              string SvName,
                              int PlNum,
                              bool EnableVC,
                              bool EnableSC)
        {
            EnableMod = Enable;
            GameMode = Mode;
            PlayerName = PlName;
            ServerName = SvName;
            MaxNumPlayers = PlNum;
            EnableVoiceChat = EnableVC;
            EnableSayChat = EnableSC;
        }
    }
}

/* EOF */
