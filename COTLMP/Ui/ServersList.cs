/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Servers List UI management code
 * COPYRIGHT:	Copyright 2025-2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Api;
using COTLMP.Data;
using COTLMP.Debug;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using I2.Loc;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains the classes and code for the server list user interface.
 * 
 * @class ServerList
 * The main server list UI class of which it contains UI management
 * code and related stuff.
 */
namespace COTLMP.Ui
{
    public static class ServerList
    {
        private static Image ServerEntry;
        private static Button BackButton;
        private static Button ConnectButton;
        private static TMP_Text MainDescription;
        private static TMP_InputField PlayerNameInput;
        private static TMP_Text PlayerNameDescription;
        private static TMP_InputField ServerNameInput;
        private static TMP_Text ServerNameDescription;
        private static TMP_Text NoneFoundDisclaimer;

        /*
         * @brief
         * Main UI element handler for the "Back" button.
         * It gets executed whenever the button is clicked.
         */
        private static void BackButtonHandler()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "BackButtonHandler() called");

            /* Return to the main menu of the game */ 
            COTLMP.Api.Assets.ShowScene("Main Menu", false, null);
        }

        private static void RefreshServersList()
        {
            /* TODO: Implement this when the network stack is implemented */
            /* Cache the server entry item and hide it when no servers were found */
            ServerEntry = GameObject.Find("ServerEntry").GetComponent<Image>();
            ServerEntry.gameObject.SetActive(false);
            return;
        }

        private static void PlayerNameSubmitHandler()
        {
            string Section;
            ConfigDefinition Definition;
            ConfigEntry<string> SettingEntry;

            /* Retrieve the section name for the setting */
            Section = COTLMP.Api.Configuration.GetSectionName(CONFIGURATION_SECTION.ServerSettings);

            /* Get the Player Name setting */
            Definition = new ConfigDefinition(Section, "Player Name");
            SettingEntry = COTLMP.Api.Configuration.GetSettingEntry<string>(Definition);
            COTLMP.Debug.Assertions.Assert(SettingEntry != null, false, null, null);

            /* Cache the new value to the globals store */
            Plugin.Globals.PlayerName = PlayerNameInput.text;

            /* Overwrite the current value of the setting and flush it */
            SettingEntry.BoxedValue = PlayerNameInput.text;
            COTLMP.Api.Configuration.FlushSettings();
        }

        private static void ServerNameSubmitHandler()
        {
            string Section;
            ConfigDefinition Definition;
            ConfigEntry<string> SettingEntry;

            /* Retrieve the section name for the setting */
            Section = COTLMP.Api.Configuration.GetSectionName(CONFIGURATION_SECTION.ServerSettings);

            /* Get the Server Name setting */
            Definition = new ConfigDefinition(Section, "Server Name");
            SettingEntry = COTLMP.Api.Configuration.GetSettingEntry<string>(Definition);
            COTLMP.Debug.Assertions.Assert(SettingEntry != null, false, null, null);

            /* Cache the new value to the globals store */
            Plugin.Globals.ServerName = ServerNameInput.text;

            /* Overwrite the current value of the setting and flush it */
            SettingEntry.BoxedValue = ServerNameInput.text;
            COTLMP.Api.Configuration.FlushSettings();
        }

        private static void LocalizeUi()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "LocalizeUi() called");

            /* Localize all the buttons */
            BackButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ServerList.ServerList_BackButton;
            ConnectButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ServerList.ServerList_ConnectButton;

            /* Localize the description header */
            MainDescription.text = MultiplayerModLocalization.UI.ServerList.ServerList_MainDescription;

            /* Localize the player and server name descriptions header */
            PlayerNameDescription.text = MultiplayerModLocalization.UI.ServerList.ServerList_EnterPlayerNameDescription;
            ServerNameDescription.text = MultiplayerModLocalization.UI.ServerList.ServerList_EnterServerNameDescription;

            /* Localize the "no servers found" disclaimer */
            NoneFoundDisclaimer.text = MultiplayerModLocalization.UI.ServerList.ServerList_NoneFound;
        }

        /*
         * @brief
         * Main UI initialization worker, of which is responsible to bind
         * every game object to their listeners, setup localization, estabilish
         * server connection and refresh servers list, etc.
         */
        private static IEnumerator UiInitializationWorker()
        {
            /*
             * Wait for at least one frame for Unity to initialize all the UI elements
             * and then proceed to initialize the rest of the UI in our own.
             */
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "BindUiElements() called");
            yield return null;

            /* Bind the "Back" button to its handler */
            BackButton = GameObject.Find("BackButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(BackButton != null, false, "BackButton gameobject returned NULL!", null);
            BackButton.onClick.AddListener(BackButtonHandler);

            /* Retrieve the "Player Name" input field and bind a handler to it */
            PlayerNameInput = GameObject.Find("PlayerNameField").GetComponent<TMP_InputField>();
            COTLMP.Debug.Assertions.Assert(PlayerNameInput != null, false, "PlayerNameInput gameobject returned NULL!", null);
            PlayerNameInput.onValueChanged.AddListener(delegate { PlayerNameSubmitHandler(); });

            /*
             * Populate the player name input field with the name of the player
             * from the mod configuration settings.
             */
            PlayerNameInput.text = Plugin.Globals.PlayerName;

            /* Retrieve the player name description */
            PlayerNameDescription = GameObject.Find("PlayerNameDescriptionInput").GetComponent<TMP_Text>();
            COTLMP.Debug.Assertions.Assert(PlayerNameDescription != null, false, "PlayerNameDescription gameobject returned NULL!", null);

            /* Retrieve the "Server Name" input field and bind a handler to it */
            ServerNameInput = GameObject.Find("ServerNameField").GetComponent<TMP_InputField>();
            COTLMP.Debug.Assertions.Assert(ServerNameInput != null, false, "ServerNameInput gameobject returned NULL!", null);
            ServerNameInput.onValueChanged.AddListener(delegate { ServerNameSubmitHandler(); });

            /*
             * Populate the player name input field with the name of the player
             * from the mod configuration settings.
             */
            ServerNameInput.text = Plugin.Globals.ServerName;

            /* Retrieve the server name description */
            ServerNameDescription = GameObject.Find("ServerNameDescriptionInput").GetComponent<TMP_Text>();
            COTLMP.Debug.Assertions.Assert(ServerNameDescription != null, false, "ServerNameDescription gameobject returned NULL!", null);

            /* Retrieve the "Connect" button */
            ConnectButton = GameObject.Find("ConnectButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(ConnectButton != null, false, "ConnectButton gameobject returned NULL!", null);

            /* Retrieve the main description of the servers browser */
            MainDescription = GameObject.Find("MainDescription").GetComponent<TMP_Text>();
            COTLMP.Debug.Assertions.Assert(MainDescription != null, false, "MainDescription gameobject returned NULL!", null);

            /* Retrieve the servers list container */
            NoneFoundDisclaimer = GameObject.Find("NoServersFoundDisclaimer").GetComponent<TMP_Text>();
            COTLMP.Debug.Assertions.Assert(NoneFoundDisclaimer != null, false, "NoServersFoundDisclaimer gameobject returned NULL!", null);

            /* All the UI elements binded to their listeners, now localize them */
            LocalizeUi();

            /* Estabilish connection with the masterserver and look for available servers */
            RefreshServersList();
            yield break;
        }

        /*
         * @brief
         * Displays the server list UI.
         */
        public static void DisplayUi()
        {
            /* Load the server list UI scene, the asset bundle should be already loaded */
            COTLMP.Api.Assets.ShowScene("ServerListUI", false, null);

            /*
             * Invoke the UI initialization worker with a coroutine. Unity loads
             * the scene after the method exits therefore initialization cannot occur
             * until every single UI game component is initialized first.
             */
            Plugin.MonoInstance.StartCoroutine(UiInitializationWorker());
        }
    }
}

/* EOF */
