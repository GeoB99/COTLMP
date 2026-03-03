/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Main file containing global resource localization strings
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using I2.Loc;
using System;

/* CLASSES & CODE *************************************************************/

namespace I2.Loc
{
    public static class MultiplayerModLocalization
    {
        public static class UI
        {
            public static string Multiplayer_Banner
            {
                get
                {
                    return LocalizationManager.GetTranslation("UI/Banner");
                }
            }

            public static string Multiplayer_Title
            {
                get
                {
                    return LocalizationManager.GetTranslation("Multiplayer/UI/TitleDialog");
                }
            }

            public static string StartServer
            {
                get
                {
                    return LocalizationManager.GetTranslation("Multiplayer/UI/StartServer");
                }
            }

            public static string ServerStarted
            {
                get
                {
                    return LocalizationManager.GetTranslation("Multiplayer/UI/ServerStarted");
                }
            }

            public static string Disconnected
            {
                get
                {
                    return LocalizationManager.GetTranslation("Multiplayer/UI/Disconnected");
                }
            }

            public static string DisconnectedError
            {
                get
                {
                    return LocalizationManager.GetTranslation("Multiplayer/UI/DisconnectedError");
                }
            }

            public static string ServerStopConfirm
            {
                get
                {
                    return LocalizationManager.GetTranslation("Multiplayer/UI/ServerConfirm");
                }
            }

            public static string Disconnect
            {
                get
                {
                    return LocalizationManager.GetTranslation("Multiplayer/UI/Disconnect");
                }
            }

            public static string DisconnectConfirm
            {
                get
                {
                    return LocalizationManager.GetTranslation("Multiplayer/UI/DisconnectConfirm");
                }
            }

            public static class Welcome
            {
                public static string Title
                {
                    get { return LocalizationManager.GetTranslation("Multiplayer/UI/Welcome/Title"); }
                }

                public static string Body
                {
                    get { return LocalizationManager.GetTranslation("Multiplayer/UI/Welcome/Body"); }
                }

                public static string DontShow
                {
                    get { return LocalizationManager.GetTranslation("Multiplayer/UI/Welcome/DontShow"); }
                }

                public static string Confirm
                {
                    get { return LocalizationManager.GetTranslation("Multiplayer/UI/Welcome/Confirm"); }
                }
            }

            public static class Settings
            {
                public static string MultiplayerSettings_Title
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/Settings");
                    }
                }

                public static string MultiplayerSettings_DisableMod
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/Settings/DisableMod");
                    }
                }

                public static string MultiplayerSettings_PlayerName
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/Settings/PlayerName");
                    }
                }

                public static string MultiplayerSettings_ServerName
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/Settings/ServerName");
                    }
                }

                public static string MultiplayerSettings_GameMode
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/Settings/GameMode");
                    }
                }

                public static string MultiplayerSettings_PlayerCount
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/Settings/PlayerCount");
                    }
                }

                public static string MultiplayerSettings_VoiceChat
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/Settings/VoiceChat");
                    }
                }
            }

            public static class ServerList
            {
                public static string ServerList_BackButton
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/BackButton");
                    }
                }

                public static string ServerList_ScanButton
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/ScanButton");
                    }
                }

                public static string ServerList_RefreshButton
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/RefreshButton");
                    }
                }

                public static string ServerList_ConnectButton
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/ConnectButton");
                    }
                }

                public static string ServerList_DirectConnectButton
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/DirectConnectButton");
                    }
                }

                public static string ServerList_IpPlaceholder
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/IpPlaceholder");
                    }
                }

                public static string ServerList_MainDescription
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/MainDescription");
                    }
                }

                public static string ServerList_NoneFound
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/NoneFound");
                    }
                }

                public static string ServerList_Scanning
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/Scanning");
                    }
                }

                public static string ServerList_Found
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/Found");
                    }
                }

                public static string ServerList_Connecting
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/Connecting");
                    }
                }

                public static string ServerList_ConnectFailed
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/ConnectFailed");
                    }
                }

                public static string ServerList_SelectServer
                {
                    get
                    {
                        return LocalizationManager.GetTranslation("Multiplayer/UI/ServerList/SelectServer");
                    }
                }
            }
        }
    }
}

/* EOF */
