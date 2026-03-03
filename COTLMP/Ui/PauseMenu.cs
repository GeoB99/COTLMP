/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Pause menu patches
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMPServer;
using COTLMP.Data;
using COTLMP.Network;
using HarmonyLib;
using I2.Loc;
using Lamb.UI;
using Lamb.UI.PauseMenu;
using System.IO;
using System.IO.Compression;
using System.Net;
using MMTools;
using src.UI;
using src.UINavigator;
using TMPro;

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * Contains the classes and code for the main menu interface
 * of the Multiplayer functionality.
 */
namespace COTLMP.Ui
{
    /**
     * @brief
     * Contains the pause menu patches
     * 
     * @field Server
     * The server object
     * 
     * @field Message
     * The message to show when quitting to main menu
     * 
     * @field Quitting
     * Whether the game is quitting
     */
    [HarmonyPatch]
    internal static class PauseMenuPatches
    {
        public static Server Server = null;
        public static string Message = null;
        public static bool Quitting = false;

        /**
         * @brief
         * Refresh the coop button
         * 
         * @param[in] __instance
         * The instance of the patched class
         * 
         * @param[in] ____coopButton
         * The coop button
         * 
         * @param[in] ____coopButtonText
         * The coop button text
         * 
         * @param[in] ____photoModeButton
         * The photo mode button
         * 
         * @return
         * true to execute the original method, false to not
         */
        [HarmonyPatch(typeof(UIPauseMenuController), "RefreshCoopText")]
        [HarmonyPrefix]
        private static bool RefreshCoopText(UIPauseMenuController __instance, MMButton ____coopButton, TextMeshProUGUI ____coopButtonText, MMButton ____photoModeButton, bool ___CoopButtonSelected)
        {
            PlayerFarming player = PlayerFarming.players[0];
            StateMachine.State playerState = player.state.CURRENT_STATE;
            if(playerState == StateMachine.State.InActive || playerState == StateMachine.State.CustomAnimation || player.GoToAndStopping)
            {
                __instance.DenyCoop = true;
                ____coopButton.interactable = false;
                ____coopButtonText.text = ScriptLocalization.UI.CoopDisabled_Interacting;
            }
            else
            {
                __instance.DenyCoop = false;
                ____coopButton.interactable = true;

                /* Client connected (not host): show Disconnect */
                if (InternalData.IsMultiplayerSession && !InternalData.IsHost)
                    ____coopButtonText.text = MultiplayerModLocalization.UI.Disconnect;
                /* Host with server running: show stop label */
                else if (Server != null)
                    ____coopButtonText.text = MultiplayerModLocalization.UI.ServerStarted;
                /* Not in a session: show Open to LAN */
                else
                    ____coopButtonText.text = MultiplayerModLocalization.UI.StartServer;
            }
            if (__instance.DenyCoop && ___CoopButtonSelected)
                MonoSingleton<UINavigatorNew>.Instance.NavigateToNew(____photoModeButton);
            return false;
        }

        /**
         * @brief
         * On pause menu open
         * 
         * @param[in] __instance
         * The instance of the patched class
         * 
         * @param[in] ____coopButton
         * The coop button
         * 
         * @param[in] ____coopButtonText
         * The coop button text
         */
        [HarmonyPatch(typeof(UIPauseMenuController), "OnEnable")]
        [HarmonyPostfix]
        private static void OnEnable(UIPauseMenuController __instance, MMButton ____coopButton, TextMeshProUGUI ____coopButtonText, MMButton ____saveButton)
        {
            PlayerFarming player = PlayerFarming.players[0];
            StateMachine.State playerState = player.state.CURRENT_STATE;
            if (playerState == StateMachine.State.InActive || playerState == StateMachine.State.CustomAnimation || player.GoToAndStopping)
            {
                __instance.DenyCoop = true;
                ____coopButton.interactable = false;
                ____coopButtonText.text = ScriptLocalization.UI.CoopDisabled_Interacting;
            }
            else
            {
                __instance.DenyCoop = false;
                ____coopButton.interactable = true;

                if (InternalData.IsMultiplayerSession && !InternalData.IsHost)
                    ____coopButtonText.text = MultiplayerModLocalization.UI.Disconnect;
                else if (Server != null)
                    ____coopButtonText.text = MultiplayerModLocalization.UI.ServerStarted;
                else
                    ____coopButtonText.text = MultiplayerModLocalization.UI.StartServer;
            }

            /* Disable the save button for clients — the host's save is
               authoritative and the client uses a disposable temp slot. */
            if (InternalData.IsMultiplayerSession && !InternalData.IsHost)
                ____saveButton.interactable = false;
        }

        /**
         * @brief
         * On coop button pressed
         * 
         * @param[in] __instance
         * The instance of the patched class
         * 
         * @return
         * true to execute the original method, false to not
         */
        [HarmonyPatch(typeof(UIPauseMenuController), "OnCoopButtonPressed")]
        [HarmonyPrefix]
        private static bool OnCoopButtonPressed(UIPauseMenuController __instance)
        {
            /* ---- Client: Disconnect from server ---- */
            if (InternalData.IsMultiplayerSession && !InternalData.IsHost)
            {
                UIMenuConfirmationWindow window = __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate);
                window.Configure(MultiplayerModLocalization.UI.Disconnect, MultiplayerModLocalization.UI.DisconnectConfirm);
                window.OnConfirm += () =>
                {
                    PlayerSync.ActiveClient?.Disconnect();
                    PlayerSync.SetClient(null);
                    InternalData.IsMultiplayerSession = false;

                    // Clean up and return to main menu
                    SimulationManager.Pause();
                    DeviceLightingManager.Reset();
                    FollowerManager.Reset();
                    StructureManager.Reset();
                    UIDynamicNotificationCenter.Reset();
                    MonoSingleton<UIManager>.Instance.ResetPreviousCursor();
                    TwitchManager.Abort();
                    MMTransition.Play(MMTransition.TransitionType.ChangeSceneAutoResume,
                        MMTransition.Effect.BlackFade, "Main Menu", 1f, "", null);
                };
                return false;
            }

            /* ---- Host: Start / Stop server ---- */
            if(Server != null)
            {
                UIMenuConfirmationWindow window = __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate);
                window.Configure(MultiplayerModLocalization.UI.ServerStarted, MultiplayerModLocalization.UI.ServerStopConfirm);
                window.OnConfirm += () => { 
                    Server.Dispose();
                    Server = null;
                    InternalData.IsHost = false;
                };
            } 
            else
            {
                Server = Server.Start(
                    port:       0,
                    logger:     new ServerLogger(),
                    serverName: Plugin.Globals?.ServerName ?? "COTL Server",
                    maxPlayers: Plugin.Globals?.MaxNumPlayers ?? 12,
                    gameMode:   Plugin.Globals?.GameMode ?? "Standard");

                if (Server == null)
                    __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate).Configure("Failed to start server!", "", true);
                else
                {
                    InternalData.IsMultiplayerSession = true;
                    InternalData.IsHost = true;
                    __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate)
                        .Configure("Server started!", $"Port: {Server.Port}  |  Name: {Server.ServerName}", true);
                    Server.ServerStopped += ServerStopped;

                    // Read the host's save file, compress it, and store it
                    // on the server so that joining clients receive it.
                    try
                    {
                        string savesDir  = Path.Combine(UnityEngine.Application.persistentDataPath, "saves");
                        string slotName  = SaveAndLoad.MakeSaveSlot(SaveAndLoad.SAVE_SLOT);
                        // The game stores saves as .mp (MessagePack) or .json — try both
                        string mpPath    = Path.Combine(savesDir, Path.ChangeExtension(slotName, ".mp"));
                        string jsonPath  = Path.Combine(savesDir, slotName);
                        string savePath  = File.Exists(mpPath) ? mpPath : jsonPath;

                        if (File.Exists(savePath))
                        {
                            byte[] rawSave   = File.ReadAllBytes(savePath);
                            byte[] compressed = CompressSaveData(rawSave);
                            Server.SetHostSaveData(compressed);
                            Debug.PrintLogger.Print(Debug.DebugLevel.MESSAGE_LEVEL,
                                Debug.DebugComponent.NETWORK_STACK_COMPONENT,
                                $"Host save data stored ({rawSave.Length} -> {compressed.Length} bytes) from {savePath}");
                        }
                        else
                        {
                            Debug.PrintLogger.Print(Debug.DebugLevel.WARNING_LEVEL,
                                Debug.DebugComponent.NETWORK_STACK_COMPONENT,
                                $"Host save file not found at {mpPath} or {jsonPath}");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.PrintLogger.Print(Debug.DebugLevel.WARNING_LEVEL,
                            Debug.DebugComponent.NETWORK_STACK_COMPONENT,
                            $"Failed to read host save data: {ex.Message}");
                    }

                    // Connect the host as a client to their own server so that
                    // position/state/health data is relayed to and from other players.
                    // Wire handlers BEFORE Connect() to avoid a race where the
                    // loopback server responds before handlers are subscribed.
                    var hostClient = new Client();
                    PlayerSync.SetClient(hostClient);
                    if (!hostClient.Connect(IPAddress.Loopback, Server.Port,
                        Data.InternalData.GetLocalPlayerName()))
                    {
                        PlayerSync.SetClient(null);
                        Debug.PrintLogger.Print(Debug.DebugLevel.WARNING_LEVEL,
                            Debug.DebugComponent.NETWORK_STACK_COMPONENT,
                            "Host failed to self-connect as client");
                    }
                }
            }
            return false;
        }
        
        /**
         * @brief
         * On server stopped
         * 
         * @param[in] sender
         * The sender of the event
         * 
         * @param[in] e
         * The reason why the server was stopped
         */
        private static void ServerStopped(object sender, ServerStoppedArgs e)
        {
            Server = null;
            InternalData.IsMultiplayerSession = false;
            InternalData.IsHost = false;
            if (Quitting)
                return;
            SimulationManager.Pause();
            DeviceLightingManager.Reset();
            FollowerManager.Reset();
            StructureManager.Reset();
            UIDynamicNotificationCenter.Reset();
            MonoSingleton<UIManager>.Instance.ResetPreviousCursor();
            TwitchManager.Abort();
            Message = (e.Reason == ServerStopReason.Error) ? MultiplayerModLocalization.UI.DisconnectedError : "";
            MMTransition.Play(MMTransition.TransitionType.ChangeSceneAutoResume, MMTransition.Effect.BlackFade, "Main Menu", 1f, "", null);
        }

        /**
         * @brief
         * The logger class for the server
         */
        private class ServerLogger : COTLMPServer.ILogger
        {
            /**
             * @brief 
             * Log an error
             * 
             * @param[in] message
             * The message to log
             */
            public void LogError(string message)
            {
                Debug.PrintLogger.Print(Debug.DebugLevel.ERROR_LEVEL, Debug.DebugComponent.NETWORK_STACK_COMPONENT, message);
            }

            /**
             * @brief
             * Log a fatal error
             * 
             * @param[in] message
             * The message to log
             */
            public void LogFatal(string message)
            {
                Debug.PrintLogger.Print(Debug.DebugLevel.FATAL_LEVEL, Debug.DebugComponent.NETWORK_STACK_COMPONENT, message);
            }

            /**
             * @brief
             * Log information
             * 
             * @param[in] message
             * The message to log
             */
            public void LogInfo(string message)
            {
                Debug.PrintLogger.Print(Debug.DebugLevel.INFO_LEVEL, Debug.DebugComponent.NETWORK_STACK_COMPONENT, message);
            }

            /**
             * @brief
             * Log a warning
             * 
             * @param[in] message
             * The message to log
             */
            public void LogWarning(string message)
            {
                Debug.PrintLogger.Print(Debug.DebugLevel.WARNING_LEVEL, Debug.DebugComponent.NETWORK_STACK_COMPONENT, message);
            }
        }

        /**
         * @brief
         * Compresses raw save data using GZip for network transfer.
         */
        private static byte[] CompressSaveData(byte[] raw)
        {
            using (var output = new MemoryStream())
            {
                using (var gz = new GZipStream(output, CompressionMode.Compress, true))
                    gz.Write(raw, 0, raw.Length);
                return output.ToArray();
            }
        }
    }
}

/* EOF */
