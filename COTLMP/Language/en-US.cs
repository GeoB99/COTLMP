/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     English (United States) localization file
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
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
            new("UI/DLC", "Multiplayer", true),
            new("UI/Banner", $"Multiplayer Edition v{COTLMP.Data.Version.CotlMpVer}", false),
            new("Multiplayer/UI/TitleDialog", "Multiplayer", false),
            new("Multiplayer/UI/Settings", "Multiplayer Settings", false),
            new("Multiplayer/UI/Settings/DisableMod", "Mod Toggle", false),
            new("Multiplayer/UI/Settings/PlayerName", "Player Name", false),
            new("Multiplayer/UI/Settings/ServerName", "Server Name", false),
            new("Multiplayer/UI/Settings/GameMode", "Game Mode", false),
            new("Multiplayer/UI/Settings/PlayerCount", "Number of maximum players to join", false),
            new("Multiplayer/UI/Settings/VoiceChat", "Enable Voice Chat", false),
            new("Multiplayer/UI/ServerList/BackButton", "Back", false),
            new("Multiplayer/UI/ServerList/ScanButton", "Scan LAN", false),
            new("Multiplayer/UI/ServerList/RefreshButton", "Refresh", false),
            new("Multiplayer/UI/ServerList/ConnectButton", "Connect", false),
            new("Multiplayer/UI/ServerList/DirectConnectButton", "Connect by IP", false),
            new("Multiplayer/UI/ServerList/IpPlaceholder", "Enter IP:Port (e.g. 192.168.1.5:7777)", false),
            new("Multiplayer/UI/ServerList/MainDescription", "Server Browser", false),
            new("Multiplayer/UI/ServerList/NoneFound", "No servers found on this network", false),
            new("Multiplayer/UI/ServerList/Scanning", "Scanning LAN...", false),
            new("Multiplayer/UI/ServerList/Found", "Found {0} server(s)", false),
            new("Multiplayer/UI/ServerList/Connecting", "Connecting...", false),
            new("Multiplayer/UI/ServerList/ConnectFailed", "Could not connect to server", false),
            new("Multiplayer/UI/ServerList/SelectServer", "Select a server or enter an IP address", false),
            new("Multiplayer/Game/Join", "{0} has joined the server", false),
            new("Multiplayer/Game/Left", "{0} has left the server", false),
            new("Multiplayer/UI/StartServer", "Open to LAN", false),
            new("Multiplayer/UI/ServerStarted", "Stop server and quit", false),
            new("Multiplayer/UI/ServerConfirm", "Are you sure you want to stop the server? This action will return you to the main menu without saving progress.", false),
            new("Multiplayer/UI/Disconnected", "Disconnected", false),
            new("Multiplayer/UI/DisconnectedError", "An error has ocurred (check console)", false),
            new("Multiplayer/UI/Welcome/Title", "Welcome to COTLMP Multiplayer!", false),
            new("Multiplayer/UI/Welcome/Body",
                "HOST: Open the pause menu in-game and click \"Open to LAN\" to share your game.\n\n" +
                "JOIN: Open this browser, click \"Scan LAN\" to find nearby servers, or type an IP:Port directly and click \"Connect by IP\".\n\n" +
                "NOTE: The Woolhaven DLC is disabled during all multiplayer sessions to ensure a fair experience for everyone.", false),
            new("Multiplayer/UI/Welcome/DontShow", "Don't show this again", false),
            new("Multiplayer/UI/Welcome/Confirm", "Got it!", false),
            new("Multiplayer/UI/Disconnect", "Disconnect", false),
            new("Multiplayer/UI/DisconnectConfirm", "Disconnect from the server? You will return to the main menu.", false)
        ];
    }
}

/* EOF */
