/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Main Menu UI management support — base-game and DLC-menu compatible
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
using HarmonyLib;
using I2.Loc;
using Lamb.UI;
using Lamb.UI.MainMenu;
using src.UI;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Ui
{
    internal static class Mainmenu
    {
        [HarmonyPatch]
        internal static class MainMenuPatches
        {
            /**
             * @brief
             * Intercepts the DLC button click on the base-game MainMenu and
             * opens our server browser overlay instead.
             *
             * This patch fires on both the standard main-menu scene AND the
             * Woolhaven DLC main-menu scene because `AuthenticateMajorDLC()`
             * always returns false while the mod is loaded, ensuring the base-
             * game scene (and class) is always used.
             */
            [HarmonyPatch(typeof(MainMenu), "OnDLCButtonClicked")]
            [HarmonyPrefix]
            private static bool OnMultiplayerButtonClickedPatch(MainMenu __instance)
            {
                PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT,
                    "Multiplayer button clicked");
                ServerList.DisplayUi();
                return false;
            }

            /**
             * @brief
             * Show a disconnection message when returning to the main menu
             * from a game session.
             */
            [HarmonyPatch(typeof(MainMenu), "Start")]
            [HarmonyPostfix]
            private static void Start(MainMenu __instance)
            {
                if (PauseMenuPatches.Message != null)
                {
                    __instance.Push<UIMenuConfirmationWindow>(
                            MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate)
                        .Configure(MultiplayerModLocalization.UI.Disconnected,
                                   PauseMenuPatches.Message, true);
                    PauseMenuPatches.Message = null;
                }
            }
        }
    }
}

/* EOF */
