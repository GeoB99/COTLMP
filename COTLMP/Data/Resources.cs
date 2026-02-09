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
        }
    }
}

/* EOF */
