/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Setting callbacks methods
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
using COTLMP.Api;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains the classes and code for the mod game related stuff.
 * 
 * @class Callbacks
 * The callbacks class which contains callback methods for the
 * mod game settings.
 */
namespace COTLMP.Game
{
    internal static class Callbacks
    {
        /*
         * @brief
         * A callback that gets called when the Game Mode
         * setting's value has changed.
         * 
         * @param[in] Value
         * An integer value representing the value of the
         * setting that has changed.
         */
        public static void GameModeCallback(int Value)
        {
            string Section;
            string GameMode;
            ConfigDefinition Definition;
            ConfigEntry<string> SettingEntry;

            /* Retrieve the section name for the setting */
            Section = COTLMP.Api.Configuration.GetSectionName(CONFIGURATION_SECTION.ServerSettings);

            /* Get the Game Mode setting */
            Definition = new ConfigDefinition(Section, "Game Mode");
            SettingEntry = COTLMP.Api.Configuration.GetSettingEntry<string>(Definition);
            COTLMP.Debug.Assertions.Assert(SettingEntry != null, false, null, null);

            /* FIXME: This is a placeholder code, the game modes should be declared in a dedicated enum */
            switch (Value)
            {
                case 0:
                {
                    GameMode = "Standard";
                    break;
                }

                case 1:
                {
                    GameMode = "Boss Fight";
                    break;
                }

                case 2:
                {
                    GameMode = "Deathmatch";
                    break;
                }

                case 3:
                {
                    GameMode = "Zombies!";
                    break;
                }

                /* Always default the game mode to Standard on bogus values */
                default:
                {
                    GameMode = "Standard";
                    break;
                }
            }

            /* Cache the new value to the globals store */
            Plugin.Globals.GameMode = GameMode;

            /* Overwrite the current value of the setting and flush it */
            SettingEntry.BoxedValue = GameMode;
            COTLMP.Api.Configuration.FlushSettings();
        }

        /*
         * @brief
         * A callback that gets called when the Max Players
         * Count setting's value has changed.
         * 
         * @param[in] Value
         * An integer value representing the value of the
         * setting that has changed.
         */
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

        /*
         * @brief
         * A callback that gets called when the Toggle Voice
         * Chat setting's value has changed.
         * 
         * @param[in] Value
         * A boolean value representing the value of the
         * setting that has changed.
         */
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
    }
}

/* EOF */
