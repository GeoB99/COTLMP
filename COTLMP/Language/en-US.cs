/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     English (United States) localization file
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Localization;

/* TRANSLATION ****************************************************************/

namespace COTLMP.Localization.English
{
    public class Strings
    {
        public static LocalizationTable[] StringsTable =
        [
            new("UI/DLC", "Multiplayer", true),
            new("Multiplayer/UI/TitleDialog", "Multiplayer", false),
            new("Multiplayer/UI/Options", "Multiplayer Options", false),
            new("Multiplayer/UI/Options/PlayerName", "Player Name", false),
            new("Multiplayer/UI/Options/PlayerSkin", "Player Skin", false),
            new("Multiplayer/UI/Options/GameMode", "Game Mode", false),
            new("Multiplayer/Game/Join", "{0} has joined the server", false),
            new("Multiplayer/Game/Left", "{0} has left the server", false),
            new("Multiplayer/UI/WIP", "Multiplayer is currently not implemented yet", false)
        ];
    }
}

/* EOF */
