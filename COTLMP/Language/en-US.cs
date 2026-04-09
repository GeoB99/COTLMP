/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     English (United States) localization file
 * COPYRIGHT:	Copyright 2025-2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Api;
using COTLMP.Data;

/* TRANSLATION ****************************************************************/

namespace COTLMP.Language
{
    public class English
    {
        public static LocalizationTable[] StringsTable =
        [
            /* Main Multiplayer header resource strings */
            new("UI/DLC", "Multiplayer", true),
            new("UI/Banner", $"Multiplayer Edition v{COTLMP.Data.Version.CotlMpVer}", false),
            new("Multiplayer/UI/TitleDialog", "Multiplayer", false),

            /* Multiplayer settings resource strings */
            new("Multiplayer/UI/Settings", "Multiplayer Settings", false),
            new("Multiplayer/UI/Settings/DisableMod", "Mod Toggle", false),
            new("Multiplayer/UI/Settings/PlayerName", "Player Name", false),
            new("Multiplayer/UI/Settings/ServerName", "Server Name", false),
            new("Multiplayer/UI/Settings/GameMode", "Game Mode", false),
            new("Multiplayer/UI/Settings/PlayerCount", "Number of maximum players to join", false),
            new("Multiplayer/UI/Settings/VoiceChat", "Enable Voice Chat", false),
            new("Multiplayer/UI/Settings/ProtectServer", "Protect the server with a password", false),

            /* Servers browser UI resource strings */
            new("Multiplayer/UI/ServerList/BackButton", "Back", false),
            new("Multiplayer/UI/ServerList/PlayerNameDescription", "Enter your player name", false),
            new("Multiplayer/UI/ServerList/ServerNameDescription", "Enter the server name", false),
            new("Multiplayer/UI/ServerList/MainDescription", "Browse the servers list and click Connect to join a server", false),
            new("Multiplayer/UI/ServerList/NoneFound", "No servers could be found", false),
            new("Multiplayer/UI/ServerList/ConnectButton", "Connect", false),

            /* Game mod resource strings */
            new("Multiplayer/Game/Join", "{0} has joined the server", false),
            new("Multiplayer/Game/Left", "{0} has left the server", false),

            /* General UI resource strings */
            new("Multiplayer/UI/StartServer", "Open to LAN", false),
            new("Multiplayer/UI/ServerStarted", "Stop server and quit", false),
            new("Multiplayer/UI/ServerConfirm", "Are you sure you want to stop the server? This action will return you to the main menu without saving progress.", false),
            new("Multiplayer/UI/Disconnected", "Disconnected", false),
            new("Multiplayer/UI/DisconnectedError", "An error has ocurred (check console)", false)
        ];
    }
}

/* EOF */
