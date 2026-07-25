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
using COTLMP.Network;
using static COTLMP.Data.Network;
using COTLMPServer.Messages;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Ui
{
    public static class ServerList
    {
        private static ScrollRect ListView;
        private static Image ServerUiEntry;
        private static Button BackButton;
        private static Button ConnectIpButton;
        private static TMP_Text MainDescription;
        private static TMP_InputField PlayerNameInput;
        private static TMP_Text PlayerNameDescription;
        private static TMP_InputField ServerNameInput;
        private static TMP_Text ServerNameDescription;
        private static TMP_Text ServerBrowserStatus;
        private static TMP_Dropdown ServerCategories;
        private static LinkedList<ServerEntry> ServerEntries;
        private static LinkedList<ServerEntry> ServerLanEntries;
        private static LinkedList<ServerEntry> CurrentServerList;
        private static UdpClient ClientReceiver;
        private static IPAddress MulticastIp;
        private static IPEndPoint MulticastEndPoint;
        private static CancellationTokenSource LanToken = null;
        private static readonly object ServerListLock = new object();

        /// <summary>
        /// Global server browser status to be displayed to the player.
        /// </summary>
        private enum SERVER_BROWSER_STATUS
        {
            /// <summary>
            /// No reachable servers could be found within the network.
            /// </summary>
            NoneFound = 0,

            /// <summary>
            /// Failed to connect to the masterserver in order to search
            /// for reachable game servers.
            /// </summary>
            MasterserverConnectFail,

            /// <summary>
            /// The searching of servers is in progress.
            /// </summary>
            ScanInProgress
        }

        /// <summary>
        /// Enumeration used to determine which kind of server list
        /// is to be refreshed (Internet or LAN).
        /// </summary>
        private enum REFRESH_WHAT
        {
            /// <summary>
            /// The Internet servers list to be refreshed.
            /// </summary>
            InternetList = 0,

            /// <summary>
            /// The LAN servers list to be refreshed.
            /// </summary>
            LanList
        }

        /// <summary>
        /// Button handler that displays the "Connect by IP" dialog
        /// box which allows the player to join a server by typing its IP.
        /// </summary>
        private static void JoinByIPButtonHandler()
        {
            COTLMP.Ui.JoinServer.DisplayUi();
        }

        /// <summary>
        /// Main UI element handler for the "Back" button.
        /// It gets executed whenever the button is clicked.
        /// </summary>
        private static void BackButtonHandler()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "BackButtonHandler() called");

            /*
             * Iterate over the Internet and LAN server entries and free each of the
             * inserted entry. We cannot remove the entry that was linked in the list
             * due to the nature of the foreach loop as we get a modified Collection
             * exception, so we have to punt the entire linked list after the iteration.
             */
            lock (ServerListLock)
            {
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
            }

            /* Cleanup the serverlist client */
            ClientReceiver.DropMulticastGroup(MulticastIp);
            ClientReceiver.Close();

            /* Cleanup the serverlist's LAN CTS as we no longer need it */
            if (LanToken != null)
            {
                LanToken.Cancel();
                LanToken.Dispose();
            }

            /* Return to the main menu of the game */
            COTLMP.Api.Assets.ShowScene("Main Menu", false, null);
        }

        /// <summary>
        /// Handler that gets invoked by Unity whenever the player selects
        /// a different server category from the dropdown.
        /// </summary>
        /// <param name = "Dropdown">The dropdown of which the player selected a different server category.</param>
        private static void OnValueChangeDropdownHandler(TMP_Dropdown Dropdown)
        {
            /* 0 translates to the first item from the dropdown which is Internet */
            if (Dropdown.value == 0)
            {
                RefreshServersList(REFRESH_WHAT.InternetList);
                return;
            }

            /*
             * At the moment there is Internet and LAN categories in the dropdown.
             * I might implement the third category, Favorites, for favorite servers
             * in the future when I can. So at this stage there are only two items
             * in the dropdown with the first starting at index 0, the last 1.
             * If we get anything else then this is some serious bug.
             */
            COTLMP.Debug.Assertions.Assert(Dropdown.value == 1, false, "Expected the last dropdown item to be at 1 index, got something else!", null);
            RefreshServersList(REFRESH_WHAT.LanList);
        }

        /// <summary>
        /// Sets a global server browser status indicating the state
        /// of the server browser (e.g. No Servers could be found).
        /// </summary>
        /// <param name = "Status">The status message to be displayed to the server browser.</param>
        /// <param name = "DisplayStatus">Set this to TRUE if the status message should be displayed, otherwise set this to FALSE.</param>
        private static void SetServerBrowserStatus(SERVER_BROWSER_STATUS Status, bool DisplayStatus)
        {
            string StatusMessage;
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "SetServerStatus() called");

            /* Don't update the browser status if it's already been hidden and the caller wants to hide it again */
            if (ServerBrowserStatus.gameObject.activeSelf == false && !DisplayStatus)
            {
                return;
            }

            /* Determine which server status message to be displayed */
            switch (Status)
            {
                case SERVER_BROWSER_STATUS.NoneFound:
                {
                    StatusMessage = MultiplayerModLocalization.UI.ServerList.ServerList_NoneFound;
                    break;
                }

                case SERVER_BROWSER_STATUS.MasterserverConnectFail:
                {
                    StatusMessage = MultiplayerModLocalization.UI.ServerList.ServerList_MasterFail;
                    break;
                }

                case SERVER_BROWSER_STATUS.ScanInProgress:
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

        /// <summary>
        /// Creates a server entry and displays it to the browser scroll listview.
        /// </summary>
        /// <param name = "ServerName">The name of the server.</param>
        /// <param name = "IP">The IP address of the server.</param>
        /// <param name = "IsLan">If the following server is bound to the LAN network, this must be set to TRUE. Otherwise set this to FALSE.</param>
        /// <param name = "Port">The port number that is used to create the server.</param>
        /// <param name = "GameMode">The game mode of the server.</param>
        /// <param name = "ActivePlayers">The count of active playing players.</param>
        /// <param name = "MaxPlayers">The count of maximum players the server can take.</param>
        /// <returns>Returns the newly allocated server entry.</returns>
        /// <remarks>The caller MUST use ReleaseServerEntry to free the allocated server entry that is returned to him.</remarks>
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

            lock (ServerListLock)
            {
                if (IsLan)
                {
                    ServerLanEntries.AddLast(Entry);
                }
                else
                {
                    ServerEntries.AddLast(Entry);
                }
            }

            /* Get the UI side component of the server entry and fill it with server data */
            UiEntry = InstanceObject.GetComponent<Image>();
            UiEntry.transform.Find("ServerNameText").GetComponent<TMP_Text>().text = ServerName;
            UiEntry.transform.Find("IPAddress").GetComponent<TMP_Text>().text = IP.ToString() + ":" + Port.ToString();
            PlayersCount = ActivePlayers + "/" + MaxPlayers;
            UiEntry.transform.Find("PlayersCount").GetComponent<TMP_Text>().text = PlayersCount;
            ConnectButton = UiEntry.transform.Find("ConnectButton").GetComponent<Button>();
            ConnectButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ServerList.ServerList_ConnectButton;

            /* And finally show it to the browser */
            UiEntry.gameObject.SetActive(true);
            return Entry;
        }

        /// <summary>
        /// Releases a server entry from memory that was being allocated by a method call to CreateServerEntry.
        /// </summary>
        /// <param name = "Entry">The server entry to be freed.</param>
        /// <remarks>This method doesn't remove the server entry from the linked list, it assumes the caller is responsible to do that!</remarks>
        private static void ReleaseServerEntry(ServerEntry Entry)
        {
            Image UiEntry;
            GameObject InstanceObject;

            InstanceObject = Entry.InstanceObject;
            UiEntry = InstanceObject.GetComponent<Image>();
            UiEntry.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(UiEntry);
            UnityEngine.Object.Destroy(InstanceObject);
        }

        /// <summary>
        /// Refreshes the servers list. The scroll view list gets populated with
        /// server entries each time the user receives a heartbeat from active servers.
        /// Data is fetched from the server as the user received the heartbeat.
        /// </summary>
        /// <param name = "WhatToRefresh">Determine which kind of server list is to be refreshed.</param>
        private static void RefreshServersList(REFRESH_WHAT WhatToRefresh)
        {
            /*
             * Erase the entire servers list and repopulate it again based on what
             * exactly should we refresh (aka scan) for the servers.
             */
            if (WhatToRefresh == REFRESH_WHAT.InternetList)
            {
                CurrentServerList = ServerEntries;
                if (ServerLanEntries != null)
                {
                    lock (ServerListLock)
                    {
                        foreach (ServerEntry Entry in ServerLanEntries)
                        {
                            ReleaseServerEntry(Entry);
                        }

                        ServerLanEntries.Clear();
                    }
                }

                /* FIXME: Internet servers lookup (also no masterserver connection) isn't implemented yet */
                SetServerBrowserStatus(SERVER_BROWSER_STATUS.MasterserverConnectFail, true);

                /* We switched away from the LAN list, cancel any related operations to LAN querying */
                if (LanToken != null)
                {
                    LanToken.Cancel();
                    LanToken.Dispose();
                    LanToken = null;
                }
            }
            else
            {
                CurrentServerList = ServerLanEntries;
                if (ServerEntries != null)
                {
                    lock (ServerListLock)
                    {
                        foreach (ServerEntry Entry in ServerEntries)
                        {
                            ReleaseServerEntry(Entry);
                        }

                        ServerEntries.Clear();
                    }
                }

                /* Recreate the LAN CTS if we switched away from the LAN list */
                if (LanToken == null)
                {
                    LanToken = new CancellationTokenSource();
                }

                /*
                 * Start listening to any broadcast signal the LAN servers might
                 * send us and fetch their server info so we can populate the
                 * serverlist GUI. The scan progress status will be updated
                 * as soon as it finds a reachable LAN server.
                 *
                 * TODO: We should periodically ping every cached server entry to
                 * ensure they're still reachable and discard any entry that's
                 * no longer online. Maybe for a future PR.
                 */
                SetServerBrowserStatus(SERVER_BROWSER_STATUS.ScanInProgress, true);
                _ = System.Threading.Tasks.Task.Run(QueryLanResponse);
            }
        }

        /// <summary>
        /// Periodically queries for broadcast response from any reacheable server
        /// within the LAN network and fetches their info data into the servers
        /// browser GUI.
        /// </summary>
        private static async System.Threading.Tasks.Task QueryLanResponse()
        {
            UdpReceiveResult Result;

            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "QueryLanResponse() called");

            while (CurrentServerList == ServerLanEntries)
            {
                try
                {
                    /* Let the task wait indefinitely for a response, abort it if the server suddenly stopped communicating with us */
                    using var Cts = CancellationTokenSource.CreateLinkedTokenSource(LanToken.Token);
                    var TimeOut = System.Threading.Tasks.Task.Delay(Timeout.Infinite, Cts.Token);

                    /* Wait for a response from the server to broadcast its presence */
                    var Recv = ClientReceiver.ReceiveAsync();
                    if (await System.Threading.Tasks.Task.WhenAny(TimeOut, Recv) == TimeOut)
                    {
                        return;
                    }

                    /* Grab the response result from the server */
                    Cts.Cancel();
                    Result = await Recv;

                    /*
                     * The user suddenly switched server category while we were querying
                     * for LAN server responses in the middle. Abort this task.
                     */
                    if (CurrentServerList != ServerLanEntries)
                    {
                        return;
                    }

                    /* Ofc bail out if this server has already been added and continue querying for response */
                    if (IsServerEntryPresent(Result.RemoteEndPoint, true))
                    {
                        COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.WARNING_LEVEL, DebugComponent.UI_COMPONENT,
                                                              $"Server with port {Result.RemoteEndPoint.Port} has already been inserted into serverlist, bail out this one!");
                        return;
                    }

                    /* Service this LAN response from the server with a Unity coroutine and fetch its server info */
                    Plugin.MonoInstance.StartCoroutine(FetchServerInfo(Result, true));
                }
                catch (TaskCanceledException)
                {
                    COTLMP.Debug.PrintLogger.Print(DebugLevel.WARNING_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, "Stop querying for LAN response, we are no longer in the LAN list");
                }
                catch (Exception ex)
                {
                    COTLMP.Debug.PrintLogger.Print(DebugLevel.FATAL_LEVEL, DebugComponent.UI_COMPONENT,
                                                   $"Broadcast response ABORTED: {ex.GetType()} + ERROR MESSAGE: {ex.Message} + GUILTY METHOD: {ex.TargetSite}");
                    return;
                }
            }
        }

        /// <summary>
        /// Checks if a server entry has already been created.
        /// </summary>
        /// <param name = "ServerEndPoint">The IP endpoint of the target server to look for.</param>
        /// <param name = "SearchInLan">Set it to TRUE if the search should be done in the LAN server entries, FALSE otherwise.</param>
        /// <returns>Returns TRUE if the server entry is already created, otherwise FALSE.</returns>
        private static bool IsServerEntryPresent(IPEndPoint ServerEndPoint, bool SearchInLan)
        {
            bool IsPresent = false;

            lock (ServerListLock)
            {
                if (SearchInLan)
                {
                    foreach (ServerEntry Entry in ServerLanEntries)
                    {
                        if (Entry.Address.Equals(ServerEndPoint.Address))
                        {
                            IsPresent = true;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (ServerEntry Entry in ServerEntries)
                    {
                        if (Entry.Address.Equals(ServerEndPoint.Address))
                        {
                            IsPresent = true;
                            break;
                        }
                    }
                }
            }

            return IsPresent;
        }

        /// <summary>
        /// Fetches the server info from the retrieved server response and
        /// creates a server GUI entry.
        /// </summary>
        /// <param name = "Result">The result response we got from a server of which we have to fetch its info from.</param>
        /// <param name = "IsLan">Set it to TRUE if the server is bound within LAN network and it should be added in LAN servers list, FALSE otherwise.</param>
        private static IEnumerator FetchServerInfo(UdpReceiveResult Result, bool IsLan)
        {
            ServerEntry Entry;
            IPEndPoint ServerEndPoint;
            Message SrvInfoMsgRequest, Msg;
            ServerInfo SrvInfo;
            UdpReceiveResult InfoResult;
            uint ServerPort;
            var Wait = new WaitForTask(null);

            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "FetchServerInfo() called");

            /*
             * The server we got a response from is alive and reachable.
             * Not all platforms return a little endian bit so convert the
             * server port into little endian before using it.
             */
            ServerPort = BitConverter.ToUInt32(Result.Buffer, 0);
            if (!BitConverter.IsLittleEndian)
            {
                ServerPort = COTLMPServer.Utils.ReverseEndianness(ServerPort);
            }

            /*
             * We must send a message request telling the server we need their info
             * so we can populate the serverlist.
             */
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, $"Sending server info request to port -> {ServerPort}");
            ServerEndPoint = new IPEndPoint(Result.RemoteEndPoint.Address, (int)ServerPort);
            SrvInfoMsgRequest = new Message(MessageType.ServerInfo, 0, null);

            /*
             * We cannot use the multicast client (aka the ClientReceiver) of the serverlist
             * because it's already being used by another thread from a task. Two threads waiting
             * to receive datagrams can lead to race conditions and we might receive whatever
             * packet we get. So setup a temporary client for the serverlist that ONLY connects
             * to the reachable server and request its info from.
             */
            using var FetchClient = new UdpClient();
            FetchClient.Client.Connect(ServerEndPoint);

            Wait.what = FetchClient.SendAsync(SrvInfoMsgRequest.Serialize(), SrvInfoMsgRequest.Serialize().Length);
            yield return Wait;
            if (Wait.what.IsFaulted || Wait.what.IsCanceled)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT, "Sending server info request message failed!");
                yield break;
            }

            /*
             * As we shipped the server info request message we now wait to receive an
             * answer back. Put a timeout of 10 seconds, if the server doesn't respond
             * to us in time then punt this operation.
             */
            using var Cts = CancellationTokenSource.CreateLinkedTokenSource(LanToken.Token);
            var Timeout = System.Threading.Tasks.Task.Delay(10000, Cts.Token);

            var Recv = FetchClient.ReceiveAsync();
            var WaitResult = System.Threading.Tasks.Task.WhenAny(Timeout, Recv);
            Wait.what = WaitResult;
            yield return Wait;

            if (Recv.IsFaulted || Recv.IsCanceled)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT, "Receiving server info failed!");
                yield break;
            }

            if (WaitResult.Result == Timeout)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.UI_COMPONENT, "Receiving server info has timed out!");
                yield break;
            }

            /* Cache the info result from the packet we just received */
            Cts.Cancel();
            InfoResult = Recv.Result;

            /* FIXME: Check that the IP address is within LAN range before we trust it, this is for a future PR */

            try
            {
                /* We received a message from the server, make sure the sequence isn't botched */
                COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, $"Serverinfo received back form port -> {ServerPort}, fetching data...");
                Msg = COTLMPServer.Messages.Message.Deserialize(InfoResult.Buffer);
                if (Msg.Sequence != 1)
                {
                    COTLMP.Debug.PrintLogger.Print(DebugLevel.WARNING_LEVEL, DebugComponent.UI_COMPONENT, $"The mesage has invalid sequence (expected 1, got {Msg.Sequence}!");
                    yield break;
                }

                /* Fetch the info from the message buffer and create a server entry with it */
                SrvInfo = ServerInfo.Deserialize(Msg.Data);
            }
            catch (InvalidDataException)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.WARNING_LEVEL, DebugComponent.UI_COMPONENT, "Invalid server data, bail out!");
                yield break;
            }
            catch (Exception ex)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.FATAL_LEVEL, DebugComponent.UI_COMPONENT,
                                               $"Fetching server info FAILED: {ex.GetType()} + ERROR MESSAGE: {ex.Message} + GUILTY METHOD: {ex.TargetSite}");
                yield break;
            }

            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, $"Server name -> {SrvInfo.ServerName}");
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, $"Active players -> {SrvInfo.ActivePlayers}");
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, $"Max players -> {SrvInfo.MaxPlayers}");
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, $"Server port -> {ServerEndPoint.Port}");
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, $"Server IP -> {ServerEndPoint.Address}");

            /* Hide the browser server status, we got a server entry */
            SetServerBrowserStatus(SERVER_BROWSER_STATUS.ScanInProgress, false);

            Entry = CreateServerEntry(SrvInfo.ServerName,
                                      ServerEndPoint.Address,
                                      IsLan,
                                      (ushort)ServerEndPoint.Port,
                                      COTLMPServer.Data.GameModes.TranslateGameModeToString(SrvInfo.Mode),
                                      SrvInfo.ActivePlayers,
                                      SrvInfo.MaxPlayers);
            if (Entry == null)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.FATAL_LEVEL, DebugComponent.UI_COMPONENT,
                                               $"Failed to create a server entry (IP: {ServerEndPoint.Address}, Port: {ServerEndPoint.Port}");
            }

            yield break;
        }

        /// <summary>
        /// Main worker handler that is executed each time the player changes their name.
        /// </summary>
        private static void PlayerNameSubmitHandler()
        {
            string Section;
            ConfigDefinition Definition;
            ConfigEntry<string> SettingEntry;

            /* Retrieve the section name for the setting */
            Section = COTLMP.Api.Configuration.GetSectionName(CONFIGURATION_SECTION.ModSettings);

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

        /// <summary>
        /// Main worker handler that is executed each time the player changes the name of their server.
        /// </summary>
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

        /// <summary>
        /// Localizes the servers list UI to different language that's currently being chosen in the game.
        /// </summary>
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
            SetServerBrowserStatus(SERVER_BROWSER_STATUS.MasterserverConnectFail, true);
        }

        /// <summary>
        /// Main UI initialization worker, of which is responsible to bind
        /// every game object to their listeners, setup localization, estabilish
        /// server connection and refresh servers list, etc.
        /// </summary>
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

            /* Retrieve the "Connect by IP" button */
            ConnectIpButton = GameObject.Find("ConnectByIpButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(ConnectIpButton != null, false, "ConnectIpButton gameobject returned NULL!", null);
            ConnectIpButton.onClick.AddListener(JoinByIPButtonHandler);

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
            UnityEngine.Object.Destroy(ServerUiEntry);

            /* Get the scroll list view of the server browser */
            ListView = GameObject.Find("ServerListView").GetComponent<ScrollRect>();
            COTLMP.Debug.Assertions.Assert(ListView != null, false, "ListView gameobject returned NULL!", null);

            /* Get the server categories dropdown */
            ServerCategories = GameObject.Find("ServerCategories").GetComponent<TMP_Dropdown>();
            COTLMP.Debug.Assertions.Assert(ServerCategories != null, false, "ServerCategories gameobject returned NULL!", null);
            ServerCategories.onValueChanged.AddListener(delegate { OnValueChangeDropdownHandler(ServerCategories); });

            /* All the UI elements binded to their listeners, now localize them */
            LocalizeUi();

            /* Estabilish connection with the masterserver and look for available servers */
            RefreshServersList(REFRESH_WHAT.InternetList);
            yield break;
        }

        /// <summary>
        /// Displays the server list UI.
        /// </summary>
        public static void DisplayUi()
        {
            /* Load the server list UI scene, the asset bundle should be already loaded */
            COTLMP.Api.Assets.ShowScene("ServerListUI", false, null);

            /* Setup the LAN CTS for the serverlist for any cancelable operations for LAN servers */
            LanToken = new CancellationTokenSource();

            /* Initialize the server entries list heads */
            ServerEntries = new LinkedList<ServerEntry>();
            ServerLanEntries = new LinkedList<ServerEntry>();
            CurrentServerList = ServerEntries;

            /* Initialize the broadcast scanning constructs */
            ClientReceiver = new UdpClient(AddressFamily.InterNetwork);
            MulticastIp = IPAddress.Parse("239.15.7.11");
            MulticastEndPoint = new IPEndPoint(IPAddress.Any, 1175);

            /*
             * Setup the server list client listener. This will be used to listen
             * to upcoming servers thorough heartbeats between client and server
             * and populate the browser list.
             */
            try
            {
                ClientReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                ClientReceiver.ExclusiveAddressUse = false;
                ClientReceiver.Client.Bind(MulticastEndPoint);
                ClientReceiver.JoinMulticastGroup(MulticastIp);
            }
            catch (Exception ex)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.FATAL_LEVEL, DebugComponent.UI_COMPONENT,
                                               $"Serverlist client init FAILED: {ex.GetType()} + ERROR MESSAGE: {ex.Message} + GUILTY METHOD: {ex.TargetSite}");
                return;
            }

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
