/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Pause menu patches
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMPServer;
using HarmonyLib;
using I2.Loc;
using Lamb.UI;
using Lamb.UI.PauseMenu;
using MMTools;
using src.UI;
using src.UINavigator;
using System.Threading;
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
        private static CancellationTokenSource tokenSource = new();

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
                ____coopButtonText.text = (Server == null) ? MultiplayerModLocalization.UI.StartServer : MultiplayerModLocalization.UI.ServerStarted;
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
                ____coopButtonText.text = (Server == null) ? MultiplayerModLocalization.UI.StartServer : MultiplayerModLocalization.UI.ServerStarted;
            }
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
            if(Server != null)
            {
                UIMenuConfirmationWindow window = __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate);
                window.Configure(MultiplayerModLocalization.UI.ServerStarted, MultiplayerModLocalization.UI.ServerStopConfirm);
                window.OnConfirm += StopServer;
            } 
            else
            {
                try
                {
                    Server = new Server(cancellationToken:tokenSource.Token, log:new ServerLogger());
                }
                catch
                {
                    __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate).Configure("Failed to start server!", "", true);
                    return false;
                }

                __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate).Configure("Started server!", $"Port: {Server.Port}", true);
                Server.ServerStopped += ServerStopped;
                _ = Server.Run();
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

        public static void StopServer()
        {
            tokenSource.Cancel();
            tokenSource = new();
            Server = null;
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
    }
}

/* EOF */
