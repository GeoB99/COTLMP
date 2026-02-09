/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Servers List UI management code
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Api;
using COTLMP.Data;
using COTLMP.Debug;
using HarmonyLib;
using BepInEx;
using I2.Loc;
using TMPro;
using System.Collections;
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
        private static Button BackButton;
        private static Button PlayerNameButton;
        private static Button ServerNameButton;
        private static Button ConnectButton;
        private static TMP_Text MainDescription;
        private static Image NoneFoundDisclaimer;

        /*
         * @brief
         * Main UI element handler for the "Back" button.
         * It gets executed whenever the button is clicked.
         */
        private static void BackButtonHandler()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "BackButtonHandler() called");

            /* Return to the main menu of the game */ 
            COTLMP.Api.Assets.ShowScene("Main Menu", null);
        }

        private static void LocalizeUi()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "LocalizeUi() called");

            /* Localize all the buttons */
            BackButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ServerList.ServerList_BackButton;
            PlayerNameButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ServerList.ServerList_PlayerNameButton;
            ServerNameButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ServerList.ServerList_ServerNameButton;
            ConnectButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ServerList.ServerList_ConnectButton;

            /* Localize the description header */
            MainDescription.text = MultiplayerModLocalization.UI.ServerList.ServerList_MainDescription;

            /* Localize the "no servers found" disclaimer */
            NoneFoundDisclaimer.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ServerList.ServerList_NoneFound;
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

            /* Retrieve the "Player Name" button */
            PlayerNameButton = GameObject.Find("PlayerNameButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(PlayerNameButton != null, false, "PlayerNameButton gameobject returned NULL!", null);

            /* Retrieve the "Server Name" button */
            ServerNameButton = GameObject.Find("ServerNameButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(PlayerNameButton != null, false, "ServerNameButton gameobject returned NULL!", null);

            /* Retrieve the "Connect" button */
            ConnectButton = GameObject.Find("ConnectButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(ConnectButton != null, false, "ConnectButton gameobject returned NULL!", null);

            /* Retrieve the main description of the servers browser */
            MainDescription = GameObject.Find("MainDescription").GetComponent<TMP_Text>();
            COTLMP.Debug.Assertions.Assert(MainDescription != null, false, "MainDescription gameobject returned NULL!", null);

            /* Retrieve the servers list container */
            NoneFoundDisclaimer = GameObject.Find("NotFoundDisclaimer").GetComponent<Image>();
            COTLMP.Debug.Assertions.Assert(NoneFoundDisclaimer != null, false, "NoneFoundDisclaimer gameobject returned NULL!", null);

            /* All the UI elements binded to their listeners, now localize them */
            LocalizeUi();
            yield break;
        }

        /*
         * @brief
         * Displays the server list UI.
         */
        public static void DisplayUi()
        {
            /* Load the server list UI scene, the asset bundle should be already loaded */
            COTLMP.Api.Assets.ShowScene("ServerListUI", null);

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
