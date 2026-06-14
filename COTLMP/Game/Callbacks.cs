/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Setting callbacks methods
 * COPYRIGHT:	Copyright 2025-2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
using static COTLMPServer.Data.GameModes;
using COTLMP.Api;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Game
{
    internal static class Callbacks
    {
        /// <summary>
        /// A callback that gets called when the Game Mode setting's value has changed.
        /// </summary>
        /// <param name = "Value">An integer value representing the value of the setting that has changed.</param>
        public static void GameModeCallback(int Value)
        {
            string Section;
            ConfigDefinition Definition;
            ConfigEntry<int> SettingEntry;

            /* Retrieve the section name for the setting */
            Section = COTLMP.Api.Configuration.GetSectionName(CONFIGURATION_SECTION.ServerSettings);

            /* Get the Game Mode setting */
            Definition = new ConfigDefinition(Section, "Game Mode");
            SettingEntry = COTLMP.Api.Configuration.GetSettingEntry<int>(Definition);
            COTLMP.Debug.Assertions.Assert(SettingEntry != null, false, null, null);

            /* HACK: Always force the game mode to Standard because we don't support any other modes atm */
            if (Value != 0)
            {
                Value = 0;
            }

            /* Cache the new value to the globals store */
            Plugin.Globals.Mode = (GameMode)Value;

            /* Overwrite the current value of the setting and flush it */
            SettingEntry.BoxedValue = (GameMode)Value;
            COTLMP.Api.Configuration.FlushSettings();
        }

        /// <summary>
        /// A callback that gets called when the Max Players Count setting's value has changed.
        /// </summary>
        /// <param name = "Value">An integer value representing the value of the setting that has changed.</param>
        public static void PlayerCountCallback(int Value)
        {
            string Section;
            ConfigDefinition Definition;
            ConfigEntry<int> SettingEntry;

            /* Retrieve the section name for the setting */
            Section = COTLMP.Api.Configuration.GetSectionName(CONFIGURATION_SECTION.ServerSettings);

            /* Get the Max Players Count setting */
            Definition = new ConfigDefinition(Section, "Max Players");
            SettingEntry = COTLMP.Api.Configuration.GetSettingEntry<int>(Definition);
            COTLMP.Debug.Assertions.Assert(SettingEntry != null, false, null, null);

            /*
             * Cache the new value to the globals store.
             * The horizontal selector begins its first element at index of 0
             * which is which why we increment the value by one to make up the
             * real count of max number of players.
             */
            Plugin.Globals.MaxNumPlayers = Value + 1;

            /* Overwrite the current value of the setting and flush it */
            SettingEntry.BoxedValue = Value + 1;
            COTLMP.Api.Configuration.FlushSettings();
        }

        /// <summary>
        /// A callback that gets called when the Max Toggle Voice Chat setting's value has changed.
        /// </summary>
        /// <param name = "Value">An boolean value representing the value of the setting that has changed.</param>
        public static void VoiceChatCallback(bool Value)
        {
            string Section;
            ConfigDefinition Definition;
            ConfigEntry<bool> SettingEntry;

            /* Retrieve the section name for the setting */
            Section = COTLMP.Api.Configuration.GetSectionName(CONFIGURATION_SECTION.ServerSettings);

            /* Get the Voice Chat Toggle setting */
            Definition = new ConfigDefinition(Section, "Toggle Voice Chat");
            SettingEntry = COTLMP.Api.Configuration.GetSettingEntry<bool>(Definition);
            COTLMP.Debug.Assertions.Assert(SettingEntry != null, false, null, null);

            /* Cache the new value to the globals store */
            Plugin.Globals.EnableVoiceChat = Value;

            /* FIXME: Enable/Disable the voice chat subsystem here */

            /* Overwrite the current value of the setting and flush it */
            SettingEntry.BoxedValue = Value;
            COTLMP.Api.Configuration.FlushSettings();
        }

        /// <summary>
        /// A callback that gets called when the Protect Server setting's value has changed.
        /// </summary>
        /// <param name = "Value">An boolean value representing the value of the setting that has changed.</param>
        public static void ProtectServerCallback(bool Value)
        {
            string Section;
            ConfigDefinition Definition;
            ConfigEntry<bool> SettingEntry;

            /* Retrieve the section name for the setting */
            Section = COTLMP.Api.Configuration.GetSectionName(CONFIGURATION_SECTION.ServerSettings);

            /* Get the Protect Server setting */
            Definition = new ConfigDefinition(Section, "Protect Server");
            SettingEntry = COTLMP.Api.Configuration.GetSettingEntry<bool>(Definition);
            COTLMP.Debug.Assertions.Assert(SettingEntry != null, false, null, null);

            /* Cache the new value to the globals store */
            Plugin.Globals.ProtectServer = Value;

            /* Overwrite the current value of the setting and flush it */
            SettingEntry.BoxedValue = Value;
            COTLMP.Api.Configuration.FlushSettings();
        }
    }
}

/* EOF */
