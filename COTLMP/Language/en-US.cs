/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     English (United States) localization file
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Api.Localization;

/* TRANSLATION ****************************************************************/

namespace COTLMP.Language
{
    public class English
    {
        public static LocalizationTable[] StringsTable =
        [
            new("UI/DLC", "Multiplayer", true),
            new("Multiplayer/UI/TitleDialog", "Multiplayer", false),
            new("Multiplayer/UI/Settings", "Multiplayer Settings", false),
            new("Multiplayer/UI/Settings/DisableMod", "Mod Toggle", false),
            new("Multiplayer/UI/Settings/PlayerName", "Player Name", false),
            new("Multiplayer/UI/Settings/ServerName", "Server Name", false),
            new("Multiplayer/UI/Settings/GameMode", "Game Mode", false),
            new("Multiplayer/UI/Settings/PlayerCount", "Number of maximum players to join", false),
            new("Multiplayer/UI/Settings/VoiceChat", "Enable Voice Chat", false),
            new("Multiplayer/UI/Settings/SayChat", "Enable Say Chat", false),
            new("Multiplayer/Game/Join", "{0} has joined the server", false),
            new("Multiplayer/Game/Left", "{0} has left the server", false),
            new("Multiplayer/UI/WIP", "Multiplayer is currently not implemented yet", false)
        ];
    }
}

/* EOF */
