/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Multiplayer Settings UI support
 * COPYRIGHT:	Copyright 2025-2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
using static COTLMPServer.Data.GameModes;
using COTL_API.UI;
using COTL_API.UI.Helpers;
using COTL_API.CustomSettings;
using COTL_API.CustomSettings.Elements;
using I2.Loc;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Ui
{
    internal static class Settings
    {
        /// <summary>
        /// Setting type enumeration. This is used to categorize settings into different types.
        /// </summary>
        private enum SETTING_TYPE
        {
            /// <summary>
            /// Denotes a toggle type setting. A toggle represents a switch that can be toggled.
            /// </summary>
            Toggle = 0,

            /// <summary>
            /// Denotes a horizontal selector. Such selectors contain multiple options that can be changed.
            /// </summary>
            HorizontalSelector,

            /// <summary>
            /// Denotes a dropdown. Dropdowns are like selectors but instead the options are wrapped in a listbox.
            /// </summary>
            Dropdown,

            /// <summary>
            /// Denotes a slider. Sliders are controlled by moving an indicator of which it changes a specific value.
            /// </summary>
            Slider
        }

        /// <summary>
        /// Action callbacks structure. This is used to encapsulate different kinds of action callbacks
        /// of which they are executed when a value of a setting has changed.
        /// </summary>
        internal struct ActionCallbacks
        {
            /// <summary>
            /// Denotes a boolean type of callback. Generally used by toggle settings.
            /// </summary>
            public Action<bool> ActionBoolCallback;

            /// <summary>
            /// Denotes an integer type of callback. Generally used by selectors, dropdowns and sliders.
            /// </summary>
            public Action<int> ActionIntCallback;

            internal ActionCallbacks(Action<bool> BoolCallback, Action<int> IntCallback)
            {
                ActionBoolCallback = BoolCallback;
                ActionIntCallback = IntCallback;
            }
        }

        /// <summary>
        /// Adds a setting to the Mods Settings UI.
        /// </summary>
        /// <param name = "Type">The type of setting to be added.</param>
        /// <param name = "SettingName">The name of the setting, provided by the caller.</param>
        /// <param name = "Value">The default value of the setting initialized at startup, provided by the caller. This parameter can be optional only
        /// if the setting type is a Toggle type.</param>
        /// <param name = "Options">An array of options, denoted as strings. This is used to store multiple setting options of a setting.
        /// This parameter can be optional only if the setting type is a Toggle type.</param>
        /// <param name = "ToggleSwitch">The initial switch value of a toggle. If set to TRUE, the toggle is set, otherwise it's unset with FALSE.
        /// This parameter only applies for toggle settings.</param>
        /// <param name = "Callbacks">A list of action callbacks, provided by the caller. This is used to invoke the specific callbacks depending
        /// on the value of a setting that has changed.</param>
        /// <returns>Returns TRUE if the setting has been added successfully, FALSE otherwise.</returns>
        private static bool AddSetting(SETTING_TYPE Type, string SettingName, string Value, string[] Options, bool ToggleSwitch, ActionCallbacks Callbacks)
        {
            Toggle ToggleSetting;
            HorizontalSelector HorizontalSelectorSetting;
            bool Success = true;

            /* Sliders, dropdowns and selectors MUST expect an array of options and values! */
            if ((Type == SETTING_TYPE.HorizontalSelector ||
                 Type == SETTING_TYPE.Slider ||
                 Type == SETTING_TYPE.Dropdown) &&
                 (Value == null || Options == null))
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                               $"Value or Options parameter are NULL while they are expected on {Type} setting type!");
                return false;
            }

            /* Bail out if no setting name was not provided */
            if (SettingName == null)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT, "No setting name provided!");
                return false;
            }

            /* Invoke the specific API call to add the setting based on type */
            switch (Type)
            {
                /* Add a toggle switch setting */
                case SETTING_TYPE.Toggle:
                {
                        ToggleSetting = COTL_API.CustomSettings.CustomSettingsManager.AddToggle(MultiplayerModLocalization.UI.Settings.MultiplayerSettings_Title,
                                                                                                SettingName,
                                                                                                ToggleSwitch,
                                                                                                Callbacks.ActionBoolCallback);
                        if (ToggleSetting == null)
                        {
                            Success = false;
                        }

                        break;
                }

                /* Add a horizontal selector setting */
                case SETTING_TYPE.HorizontalSelector:
                {
                        HorizontalSelectorSetting = COTL_API.CustomSettings.CustomSettingsManager.AddHorizontalSelector(MultiplayerModLocalization.UI.Settings.MultiplayerSettings_Title,
                                                                                                                        SettingName,
                                                                                                                        Value,
                                                                                                                        Options,
                                                                                                                        Callbacks.ActionIntCallback);
                        if (HorizontalSelectorSetting == null)
                        {
                            Success = false;
                        }

                        break;
                }

                default:
                {
                        COTLMP.Debug.PrintLogger.Print(DebugLevel.WARNING_LEVEL, DebugComponent.UI_COMPONENT,
                                                       $"The {Type} setting type is currently not implemented yet!");
                        Success = false;
                        break;
                }
            }

            return Success;
        }

        /// <summary>
        /// Initializes the Settings UI of the mod during the startup of the mod.
        /// </summary>
        public static bool InitializeUI()
        {
            bool Success;
            ActionCallbacks Callbacks;

            /* Add the "Game Mode" setting */
            Callbacks = new ActionCallbacks(null, COTLMP.Game.Callbacks.GameModeCallback);
            string[] GameModes = System.Enum.GetNames(typeof(GameMode));
            Success = AddSetting(SETTING_TYPE.HorizontalSelector,
                                 MultiplayerModLocalization.UI.Settings.MultiplayerSettings_GameMode,
                                 TranslateGameModeToString(Plugin.Globals.Mode),
                                 GameModes,
                                 false,
                                 Callbacks);
            if (!Success)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                               "Failed to add the Game Modes setting, expect problems with mod initialization!");
                return Success;
            }

            /* Add the "Players Count" setting */
            Callbacks = new ActionCallbacks(null, COTLMP.Game.Callbacks.PlayerCountCallback);
            string[] MaxPlayersCount = {"1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"};
            Success = AddSetting(SETTING_TYPE.HorizontalSelector,
                                 MultiplayerModLocalization.UI.Settings.MultiplayerSettings_PlayerCount,
                                 Plugin.Globals.MaxNumPlayers.ToString(),
                                 MaxPlayersCount,
                                 false,
                                 Callbacks);
            if (!Success)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                               "Failed to add the Players Count setting, expect problems with mod initialization!");
                return Success;
            }

            /* Add the "Enable Voice Chat" setting */
            Callbacks = new ActionCallbacks(COTLMP.Game.Callbacks.VoiceChatCallback, null);
            Success = AddSetting(SETTING_TYPE.Toggle,
                                 MultiplayerModLocalization.UI.Settings.MultiplayerSettings_VoiceChat,
                                 null,
                                 null,
                                 Plugin.Globals.EnableVoiceChat,
                                 Callbacks);
            if (!Success)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                               "Failed to add the Voice Chat setting, expect problems with mod initialization!");
                return Success;
            }

            /* Add the "Protect Server" setting */
            Callbacks = new ActionCallbacks(COTLMP.Game.Callbacks.ProtectServerCallback, null);
            Success = AddSetting(SETTING_TYPE.Toggle,
                                 MultiplayerModLocalization.UI.Settings.MultiplayerSettings_ProtectServer,
                                 null,
                                 null,
                                 Plugin.Globals.ProtectServer,
                                 Callbacks);
            if (!Success)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                               "Failed to add the Protect Server setting, expect problems with mod initialization!");
                return Success;
            }

            return Success;
        }
    }
}

/* EOF */
