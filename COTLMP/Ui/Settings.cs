/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Multiplayer Settings UI support
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
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

/*
 * @brief
 * Contains the classes and code for the settings UI interface.
 * 
 * @class SettingsUIMultiplayer
 * The main Settings UI class of which it contains settings UI
 * management and initialization code.
 */
namespace COTLMP.Ui.Settings
{
    internal static class SettingsUIMultiplayer
    {
        /*
         * @brief
         * Setting type enumeration. This is used to categorize
         * settings into different types.
         * 
         * @field Toggle
         * Denotes a toggle type setting. A toggle represents a switch
         * that can be toggled.
         * 
         * @field HorizontalSelector
         * Denotes a horizontal selector. Such selectors contain multiple
         * options that can be changed.
         * 
         * @field Dropdown
         * Denotes a dropdown. Dropdowns are like selectors but instead
         * the options are wrapped in a listbox.
         * 
         * @field Slider
         * Denotes a slider. Sliders are controlled by moving an indicator
         * of which it changes a specific value.
         */
        private enum SETTING_TYPE
        {
            Toggle = 0,
            HorizontalSelector,
            Dropdown,
            Slider
        }

        /*
         * @brief
         * Action callbacks structure. This is used to encapsulate
         * different kinds of action callbacks of which they are
         * executed when a value of a setting has changed.
         * 
         * @field ActionBoolCallback
         * Denotes a boolean type of callback. Generally used by
         * toggle settings.
         * 
         * @field ActionIntCallback
         * Denotes an integer type of callback. Generally used by
         * selectors, dropdowns and sliders.
         */
        internal struct ActionCallbacks
        {
            public Action<bool> ActionBoolCallback;
            public Action<int> ActionIntCallback;

            internal ActionCallbacks(Action<bool> BoolCallback, Action<int> IntCallback)
            {
                ActionBoolCallback = BoolCallback;
                ActionIntCallback = IntCallback;
            }
        }

        /*
         * @brief
         * Adds a setting to the Mods Settings UI.
         * 
         * @param[in] Type
         * The type of setting to be added.
         * 
         * @param[in] SettingName
         * The name of the setting, provided by the caller.
         * 
         * @param[in] Value
         * The default value of the setting initialized at startup,
         * provided by the caller. This parameter can be optional only
         * if the setting type is a Toggle type.
         * 
         * @param[in] Options
         * An array of options, denoted as strings. This is used to
         * store multiple setting options of a setting. This parameter
         * can be optional only if the setting type is a Toggle type.
         * 
         * @param[in] ToggleSwitch
         * The initial switch value of a toggle. If set to TRUE, the
         * toggle is set, otherwise it's unset with FALSE. This parameter
         * only applies for toggle settings.
         * 
         * @param[in] Callbacks
         * A list of action callbacks, provided by the caller. This is used
         * to invoke the specific callbacks depending on the value of a setting
         * that has changed.
         * 
         * @return
         * Returns TRUE if the setting has been added successfully, FALSE otherwise.
         */
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
                COTLMP.Debug.Log.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                           $"Value or Options parameter are NULL while they are expected on {Type} setting type!");
                return false;
            }

            /* Bail out if no setting name was not provided */
            if (SettingName == null)
            {
                COTLMP.Debug.Log.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT, "No setting name provided!");
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
                        COTLMP.Debug.Log.Print(DebugLevel.WARNING_LEVEL, DebugComponent.UI_COMPONENT,
                                                   $"The {Type} setting type is currently not implemented yet!");
                        Success = false;
                        break;
                }
            }

            return Success;
        }

        /*
         * @brief
         * Initializes the Settings UI of the mod during the
         * startup of the mod.
         */
        public static void InitializeUI()
        {
            bool Success;
            ActionCallbacks Callbacks;

            /* Add the "Mod Toggle" setting */
            Callbacks = new ActionCallbacks(null, null);
            Success = AddSetting(SETTING_TYPE.Toggle,
                                 MultiplayerModLocalization.UI.Settings.MultiplayerSettings_DisableMod,
                                 null,
                                 null,
                                 true,
                                 Callbacks);
            if (!Success)
            {
                COTLMP.Debug.Log.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                           "Failed to add the Mod Toggle setting, expect problems with mod initialization!");
                return;
            }

            /* Add the "Game Mode" setting */
            Callbacks = new ActionCallbacks(null, null);
            string[] GameModes = {"Standard", "Boss Fight", "Deathmatch", "Zombies!"}; // FIXME: This is a placeholder, this must be gathered from configuration files!
            Success = AddSetting(SETTING_TYPE.HorizontalSelector,
                                 MultiplayerModLocalization.UI.Settings.MultiplayerSettings_GameMode,
                                 "Standard",
                                 GameModes,
                                 false,
                                 Callbacks);
            if (!Success)
            {
                COTLMP.Debug.Log.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                           "Failed to add the Game Modes setting, expect problems with mod initialization!");
                return;
            }

            /* Add the "Players Count" setting */
            Callbacks = new ActionCallbacks(null, null);
            string[] MaxPlayersCount = {"1", "2", "3", "4"}; // FIXME: This is a placeholder, this must be gathered from configuration files!
            Success = AddSetting(SETTING_TYPE.HorizontalSelector,
                                 MultiplayerModLocalization.UI.Settings.MultiplayerSettings_PlayerCount,
                                 "1",
                                 MaxPlayersCount,
                                 false,
                                 Callbacks);
            if (!Success)
            {
                COTLMP.Debug.Log.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                           "Failed to add the Players Count setting, expect problems with mod initialization!");
                return;
            }

            /* Add the "Enable Voice Chat" setting */
            Callbacks = new ActionCallbacks(null, null);
            Success = AddSetting(SETTING_TYPE.Toggle,
                                 MultiplayerModLocalization.UI.Settings.MultiplayerSettings_VoiceChat,
                                 null,
                                 null,
                                 false,
                                 Callbacks);
            if (!Success)
            {
                COTLMP.Debug.Log.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                           "Failed to add the Voice Chat setting, expect problems with mod initialization!");
                return;
            }

            /* Add the "Enable Say Chat" setting */
            Callbacks = new ActionCallbacks(null, null);
            Success = AddSetting(SETTING_TYPE.Toggle,
                                 MultiplayerModLocalization.UI.Settings.MultiplayerSettings_SayChat,
                                 null,
                                 null,
                                 false,
                                 Callbacks);
            if (!Success)
            {
                COTLMP.Debug.Log.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                           "Failed to add the Voice Chat setting, expect problems with mod initialization!");
                return;
            }
        }
    }
}

/* EOF */
