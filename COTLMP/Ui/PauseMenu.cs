/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Pause menu patches
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMPServer;
using static COTLMPServer.Data.GameModes;
using HarmonyLib;
using I2.Loc;
using Lamb.UI;
using Lamb.UI.PauseMenu;
using MMTools;
using src.UI;
using src.UINavigator;
using System.Threading;
using TMPro;
using UnityEngine;
using System.Net;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Ui
{
    /// <summary>
    /// Contains the pause menu patches
    /// </summary>
    [HarmonyPatch]
    internal static class PauseMenuPatches
    {
        /// <summary>
        /// The server object
        /// </summary>
        public static Server Server = null;
        /// <summary>
        /// The message to show when quitting to main menu
        /// </summary>
        public static string Message = null;
        /// <summary>
        /// Whether the game is quitting
        /// </summary>
        public static bool Quitting = false;
        public static CancellationTokenSource tokenSource = new();

        /// <summary>
        /// Refresh the coop button
        /// </summary>
        /// <param name="__instance">
        /// The instance of the patched class
        /// </param>
        /// <param name="____coopButton">
        /// The coop button
        /// </param>
        /// <param name="____coopButtonText">
        /// The coop button text
        /// </param>
        /// <param name="____photoModeButton">
        /// The photo mode button
        /// </param>
        /// <returns>
        /// true to execute the original method, false to not
        /// </returns>
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

                /* Overwite the Coop button based on the state of the client */
                if (Server == null &&
                   !Plugin.GlobalsInternal.InGameSession)
                {
                    ____coopButtonText.text = MultiplayerModLocalization.UI.StartServer;
                }
                else if (Server != null &&
                        (Plugin.GlobalsInternal.IsServerCreator &&
                         Plugin.GlobalsInternal.InGameSession))
                {
                    ____coopButtonText.text = MultiplayerModLocalization.UI.ServerStarted;
                }
                else // Plugin.GlobalsInternal.InGameSession == true
                {
                    ____coopButtonText.text = MultiplayerModLocalization.UI.LeaveServer;
                }
            }
            if (__instance.DenyCoop && ___CoopButtonSelected)
                MonoSingleton<UINavigatorNew>.Instance.NavigateToNew(____photoModeButton);
            return false;
        }

        /// <summary>
        /// On pause menu open
        /// </summary>
        /// <param name="__instance">
        /// The instance of the patched class
        /// </param>
        /// <param name="____coopButton">
        /// The coop button
        /// </param>
        /// <param name="____coopButtonText">
        /// The coop button text
        /// </param>
        [HarmonyPatch(typeof(UIPauseMenuController), "OnEnable")]
        [HarmonyPostfix]
        private static void OnEnable(UIPauseMenuController __instance, MMButton ____coopButton, TextMeshProUGUI ____coopButtonText)
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

                /*
                 * Overwite the Coop button based on the state of the client.
                 * Force disable the Save button on clients who have joined a server
                 * since they don't have any save file loaded anyway.
                 */
                if (Server == null &&
                   !Plugin.GlobalsInternal.InGameSession)
                {
                    ____coopButtonText.text = MultiplayerModLocalization.UI.StartServer;
                }
                else if (Server != null &&
                        (Plugin.GlobalsInternal.IsServerCreator &&
                         Plugin.GlobalsInternal.InGameSession))
                {
                    ____coopButtonText.text = MultiplayerModLocalization.UI.ServerStarted;
                }
                else // Plugin.GlobalsInternal.InGameSession == true
                {
                    ____coopButtonText.text = MultiplayerModLocalization.UI.LeaveServer;
                    MonoSingleton<UIManager>.Instance.ForceDisableSaving = true;
                }
            }
        }

        /// <summary>
        /// On pause menu open
        /// </summary>
        /// <param name="__instance">
        /// The instance of the patched class
        /// </param>
        /// <returns>
        /// true to execute the original method, false to not
        /// </returns>
        [HarmonyPatch(typeof(UIPauseMenuController), "OnCoopButtonPressed")]
        [HarmonyPrefix]
        private static bool OnCoopButtonPressed(UIPauseMenuController __instance)
        {
            IPEndPoint LanPoint;

            /* The server has already been created, ask the player if they want to shut it down */
            if (Server != null &&
               (Plugin.GlobalsInternal.IsServerCreator &&
                Plugin.GlobalsInternal.InGameSession))
            {
                UIMenuConfirmationWindow window = __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate);
                window.Configure(MultiplayerModLocalization.UI.ServerStarted, MultiplayerModLocalization.UI.ServerStopConfirm);
                window.OnConfirm += StopServer;
            }
            else if (Plugin.GlobalsInternal.InGameSession)
            {
                UIMenuConfirmationWindow window = __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate);
                window.Configure(MultiplayerModLocalization.UI.LeaveServer, MultiplayerModLocalization.UI.LeaveConfirm);
                window.OnConfirm += LeaveServer;
            }
            else
            {
                try
                {
                    /*
                     * The server has been started, the player is currently in session.
                     * Acknowledge the player hosts the server through LAN which makes
                     * themselves a server so to speak.
                     */
                    Plugin.GlobalsInternal.InGameSession = true;
                    Plugin.GlobalsInternal.IsServerCreator = true;

                    /* Start the saychat mechanism */
                    COTLMP.Ui.SayChat.StartSayChat();

                    LanPoint = new IPEndPoint(0, 0);
                    Server = new Server(Application.version, Plugin.Globals.MaxNumPlayers, Plugin.Globals.ServerName, Plugin.Globals.Mode, LanPoint, cancellationToken: tokenSource.Token, log: new ServerLogger());
                    Server.ServerStopped += ServerStopped;
                }
                catch
                {
                    __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate).Configure("Failed to start server!", "", true);
                    return false;
                }

                /* Run the main Server thread and let the host connect to it immediately */
                __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate).Configure("Started server!", $"Port: {Server.Port}\nServer Name: {Server.serverName}\nGamemode: {TranslateGameModeToString(Server.gameMode)}", true);
                Server.ServerStopped += ServerStopped;
                _ = System.Threading.Tasks.Task.Run(Server.Run);
                _ = COTLMP.Network.Network.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), Server.Port), tokenSource.Token);
            }

            return false;
        }

        /// <summary>
        /// Cleans up the game structures and stuff that have been initialized
        /// upon joining a server game or playing the game on LAN, etc.
        /// </summary>
        private static void RundownGame()
        {
            /* Shutdown all the game related stuff */
            SimulationManager.Pause();
            DeviceLightingManager.Reset();
            FollowerManager.Reset();
            StructureManager.Reset();

            /* Reset the game display */
            UIDynamicNotificationCenter.Reset();
            MonoSingleton<UIManager>.Instance.ResetPreviousCursor();
            TwitchManager.Abort();

            /* Shutdown the saychat mechanism */
            COTLMP.Ui.SayChat.Shutdown();
        }

        /// <summary>
        /// Method that is called whenever a client wants to leave the server.
        /// </summary>
        private static void LeaveServer()
        {
            /* Disconnect the client and cleanup game related stuff before transitioning to main menu */
            tokenSource.Cancel();
            tokenSource = new();
            RundownGame();

            /* The client fully disconnected from the server, the player is no longer in session */
            MMTransition.Play(MMTransition.TransitionType.ChangeSceneAutoResume, MMTransition.Effect.BlackFade, "Main Menu", 1f, "", null);
            Plugin.GlobalsInternal.InGameSession = false;
        }

        /// <summary>
        /// On server stopped
        /// </summary>
        /// <param name="sender">
        /// The sender of the event
        /// </param>
        /// <param name="e">
        /// The reason why the server was stopped
        /// </param>
        private static void ServerStopped(object sender, ServerStoppedArgs e)
        {
            /* The server is already quitting, bail out */
            Server = null;
            if (Quitting)
                return;

            /* Cleanup all the game related stuff */
            RundownGame();

            /* Show a reason message to the player why the server has stopped and return to main menu scene */
            Message = (e.Reason == ServerStopReason.Error) ? MultiplayerModLocalization.UI.DisconnectedError : "";
            MMTransition.Play(MMTransition.TransitionType.ChangeSceneAutoResume, MMTransition.Effect.BlackFade, "Main Menu", 1f, "", null);

            /* The server has been stopped, the player is no longer in session */
            Plugin.GlobalsInternal.InGameSession = false;
            Plugin.GlobalsInternal.IsServerCreator = false;
        }

        /// <summary>
        /// Stops the integrated server.
        /// </summary>
        public static void StopServer()
        {
            tokenSource.Cancel();
            tokenSource = new();
            Server = null;
        }

        /// <summary>
        /// The logger class for the server
        /// </summary>
        private class ServerLogger : COTLMPServer.ILogger
        {
            /// <summary>
            /// Log an error
            /// </summary>
            /// <param name="message">
            /// The message to log
            /// </param>
            public void LogError(string message)
            {
                Debug.PrintLogger.Print(Debug.DebugLevel.ERROR_LEVEL, Debug.DebugComponent.NETWORK_STACK_COMPONENT, message);
            }

            /// <summary>
            /// Log a fatal error
            /// </summary>
            /// <param name="message">
            /// The message to log
            /// </param>
            public void LogFatal(string message)
            {
                Debug.PrintLogger.Print(Debug.DebugLevel.FATAL_LEVEL, Debug.DebugComponent.NETWORK_STACK_COMPONENT, message);
            }

            /// <summary>
            /// Log information
            /// </summary>
            /// <param name="message">
            /// The message to log
            /// </param>
            public void LogInfo(string message)
            {
                Debug.PrintLogger.Print(Debug.DebugLevel.INFO_LEVEL, Debug.DebugComponent.NETWORK_STACK_COMPONENT, message);
            }

            /// <summary>
            /// Log a warning
            /// </summary>
            /// <param name="message">
            /// The message to log
            /// </param>
            public void LogWarning(string message)
            {
                Debug.PrintLogger.Print(Debug.DebugLevel.WARNING_LEVEL, Debug.DebugComponent.NETWORK_STACK_COMPONENT, message);
            }
        }
    }
}

/* EOF */
