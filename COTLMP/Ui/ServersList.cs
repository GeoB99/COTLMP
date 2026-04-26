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
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static COTLMP.Data.Network;

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
        private static ScrollRect ListView;
        private static Image ServerUiEntry;
        private static Button BackButton;
        private static TMP_Text MainDescription;
        private static TMP_InputField PlayerNameInput;
        private static TMP_Text PlayerNameDescription;
        private static TMP_InputField ServerNameInput;
        private static TMP_Text ServerNameDescription;
        private static TMP_Text ServerBrowserStatus;
        private static LinkedList<ServerEntry> ServerEntries;
        private static LinkedList<ServerEntry> ServerLanEntries;

        /*
         * @brief
         * Global server browser status to be displayed to the player.
         *
         * @field NoneFound
         * No reachable servers could be found within the network.
         *
         * @field MasterserverConnectFail
         * Failed to connect to the masterserver in order to search
         * for reachable game servers.
         *
         * @field ScanInProgress
         * The searching of servers is in progress.
         */
        private enum SERVER_STATUS
        {
            NoneFound = 0,
            MasterserverConnectFail,
            ScanInProgress
        }

        /*
         * @brief
         * Main UI element handler for the "Back" button.
         * It gets executed whenever the button is clicked.
         */
        private static void BackButtonHandler()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "BackButtonHandler() called");

            /*
             * Iterate over the Internet and LAN server entries and free each of the
             * inserted entry. We cannot remove the entry that was linked in the list
             * due to the nature of the foreach loop as we get a modified Collection
             * exception, so we have to punt the entire linked list after the iteration.
             */
            foreach (ServerEntry Entry in ServerEntries)
            {
                ReleaseServerEntry(Entry);
            }

            ServerEntries.Clear();

            foreach (ServerEntry Entry in ServerLanEntries)
            {
                ReleaseServerEntry(Entry);
            }

            ServerLanEntries.Clear();

            /* Return to the main menu of the game */
            COTLMP.Api.Assets.ShowScene("Main Menu", false, null);
        }

        /*
         * @brief
         * Sets a global server browser status indicating the state
         * of the server browser (e.g. No Servers could be found).
         *
         * @param[in] Status
         * The type of setting to be added.
         *
         * @param[in] DisplayStatus
         * Set this to TRUE if the status message should be displayed,
         * otherwise set this to FALSE.
         */
        private static void SetServerStatus(SERVER_STATUS Status, bool DisplayStatus)
        {
            string StatusMessage;
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "SetServerStatus() called");

            /* Determine which server status message to be displayed */
            switch (Status)
            {
                case SERVER_STATUS.NoneFound:
                {
                    StatusMessage = MultiplayerModLocalization.UI.ServerList.ServerList_NoneFound;
                    break;
                }

                case SERVER_STATUS.MasterserverConnectFail:
                {
                    StatusMessage = MultiplayerModLocalization.UI.ServerList.ServerList_MasterFail;
                    break;
                }

                case SERVER_STATUS.ScanInProgress:
                {
                    StatusMessage = MultiplayerModLocalization.UI.ServerList.ServerList_ScanProgress;
                    break;
                }

                default:
                    StatusMessage = null;
                    break;
            }

            /* Overwrite the previous status message and display it, whether or not */
            ServerBrowserStatus.text = StatusMessage;
            ServerBrowserStatus.gameObject.SetActive(DisplayStatus);
        }

        /*
         * @brief
         * Creates a server entry and displays it to the browser scroll listview.
         *
         * @param[in] ServerName
         * The name of the server.
         *
         * @param[in] IP
         * The IP address of the server.
         *
         * @param[in] IsLan
         * If the following server is bound to the LAN network, this must be set
         * to TRUE. Otherwise set this to FALSE.
         *
         * @param[in] Port
         * The port number that is used to create the server.
         *
         * @param[in] GameMode
         * The game mode of the server.
         *
         * @param[in] ActivePlayers
         * The count of active playing players.
         *
         * @param[in] MaxPlayers
         * The count of maximum players the server can take.
         *
         * @return
         * Returns the newly allocated server entry.
         *
         * @remarks
         * The caller MUST use ReleaseServerEntry to free the allocated server
         * entry that is returned to him.
         */
        private static ServerEntry CreateServerEntry(string ServerName, IPAddress IP, bool IsLan, ushort Port, string GameMode, int ActivePlayers, int MaxPlayers)
        {
            GameObject Prefab, InstanceObject;
            ServerEntry Entry;
            Image UiEntry;
            Button ConnectButton;
            string PlayersCount;

            /* Get the server entry prefab template from the prefabs bundle asset */
            Prefab = Plugin.ModPrefabsBundle.LoadAsset<GameObject>("ServerEntryPrefab");
            if (Prefab == null)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                               "Failed to create server entry, couldn't load the prefab resource!");
                return null;
            }

            /* Create a gameobject for the server entry based on the prefab template */
            InstanceObject = GameObject.Instantiate<GameObject>(Prefab);
            InstanceObject.transform.SetParent(ListView.content.transform, false);
            if (InstanceObject == null)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT,
                                               "Failed to create server entry, couldn't get instantiate the instance object from prefab!");
                return null;
            }

            /* Allocate a server entry and add it to the server linked list accordingly (see IsLan parameter) */
            Entry = new ServerEntry(ServerName,
                                    ActivePlayers,
                                    MaxPlayers,
                                    GameMode,
                                    false,
                                    false,
                                    IP,
                                    Port,
                                    InstanceObject);
            if (IsLan)
            {
                ServerLanEntries.AddLast(Entry);
            }
            else
            {
                ServerEntries.AddLast(Entry);
            }

            /* Get the UI side component of the server entry and fill it with server data */
            UiEntry = InstanceObject.GetComponent<Image>();
            UiEntry.transform.Find("ServerNameText").GetComponent<TMP_Text>().text = ServerName;
            UiEntry.transform.Find("IPAddress").GetComponent<TMP_Text>().text = IP.ToString();
            PlayersCount = ActivePlayers + "/" + MaxPlayers;
            UiEntry.transform.Find("PlayersCount").GetComponent<TMP_Text>().text = PlayersCount;
            ConnectButton = UiEntry.transform.Find("ConnectButton").GetComponent<Button>();
            ConnectButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ServerList.ServerList_ConnectButton;

            /* And finally show it to the browser */
            UiEntry.gameObject.SetActive(true);
            return Entry;
        }

        /*
         * @brief
         * Releases a server entry from memory that was being allocated by
         * a method call to CreateServerEntry.
         *
         * @param[in] Entry
         * The server entry to be freed.
         *
         * @remarks
         * This method doesn't remove the server entry from the linked, it
         * assumes the caller is responsible to do that!
         */
        private static void ReleaseServerEntry(ServerEntry Entry)
        {
            Image UiEntry;
            GameObject InstanceObject;

            InstanceObject = Entry.InstanceObject;
            UiEntry = InstanceObject.GetComponent<Image>();
            UiEntry.gameObject.SetActive(false);
            Object.Destroy(UiEntry);
            Object.Destroy(InstanceObject);
        }

        /*
         * @brief
         * Refreshes the servers list. The scroll view list gets populated with
         * server entries each time the user receives a heartbeat from active servers.
         * Data is fetched from the server as the user received the heartbeat.
         */
        private static void RefreshServersList()
        {
            /* TODO: Implement this when the network stack is implemented */
            return;
        }

        /*
         * @brief
         * Main worker handler that is executed each time the player
         * changes their name.
         */
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

        /*
         * @brief
         * Main worker handler that is executed each time the player
         * changes the name of their server.
         */
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

        /*
         * @brief
         * Localizes the servers list UI to different language that's currently
         * being chosen in the game.
         */
        private static void LocalizeUi()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "LocalizeUi() called");

            /* Localize the Back button */
            BackButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ServerList.ServerList_BackButton;

            /* Localize the description header */
            MainDescription.text = MultiplayerModLocalization.UI.ServerList.ServerList_MainDescription;

            /* Localize the player and server name descriptions header */
            PlayerNameDescription.text = MultiplayerModLocalization.UI.ServerList.ServerList_EnterPlayerNameDescription;
            ServerNameDescription.text = MultiplayerModLocalization.UI.ServerList.ServerList_EnterServerNameDescription;

            /* Set the servers browser status to Master Fail for now */
            SetServerStatus(SERVER_STATUS.MasterserverConnectFail, true);
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
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "UiInitializationWorker() called");
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

            /* Retrieve the main description of the servers browser */
            MainDescription = GameObject.Find("MainDescription").GetComponent<TMP_Text>();
            COTLMP.Debug.Assertions.Assert(MainDescription != null, false, "MainDescription gameobject returned NULL!", null);

            /* Retrieve the server browser status */
            ServerBrowserStatus = GameObject.Find("ServerStatus").GetComponent<TMP_Text>();
            COTLMP.Debug.Assertions.Assert(ServerBrowserStatus != null, false, "ServerBrowserStatus gameobject returned NULL!", null);

            /*
             * Get the original server entry element from the UI (that's been created
             * in the editor) and disable it. We will create server entries dinamically
             * as we scan for reachable servers.
             */
            ServerUiEntry = GameObject.Find("ServerEntry").GetComponent<Image>();
            COTLMP.Debug.Assertions.Assert(ServerUiEntry != null, false, "ServerUiEntry gameobject returned NULL!", null);
            ServerUiEntry.gameObject.SetActive(false);
            Object.Destroy(ServerUiEntry);

            /* Get the scroll list view of the server browser */
            ListView = GameObject.Find("ServerListView").GetComponent<ScrollRect>();
            COTLMP.Debug.Assertions.Assert(ListView != null, false, "ListView gameobject returned NULL!", null);

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

            /* Initialize the server entries list head */
            ServerEntries = new LinkedList<ServerEntry>();
            ServerLanEntries = new LinkedList<ServerEntry>();

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
