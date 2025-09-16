/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Main Menu UI management support
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
using BepInEx;
using HarmonyLib;
using I2.Loc;
using Lamb.UI;
using Lamb.UI.MainMenu;
using src.UI;
using UnityEngine.UI;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains the classes and code for the main menu interface
 * of the Multiplayer functionality.
 * 
 * @class MainMenuPatches
 * Contains harmony patches of which hook up with the original
 * game source code methods and data. Specifically, the patches
 * serve to change the aspect of the main menu.
 */
namespace COTLMP.Ui
{
    internal static class Mainmenu
    {
        [HarmonyPatch]
        internal static class MainMenuPatches
        {
            /*
             * @brief
             * Patches the private DLC on-click button private method, of which
             * we hook up our Multiplayer dialog. 
             * 
             * @param[in] __instance
             * The current instance value of the method being patched.
             * 
             * @return
             * Returns TRUE if tthe original method of the game is to be executed.
             * FALSE if our method is to be executed instead.
             */
            [HarmonyPatch(typeof(MainMenu), "OnDLCButtonClicked")]
            [HarmonyPrefix]
            private static bool OnMultiplayerButtonClickedPatch(MainMenu __instance)
            {
                /* Throw a dialog box warning the user Multiplayer is currently WIP */
                COTLMP.Debug.PrintLogger.Print(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "The Multiplayer button has been clicked!");
                UIMenuConfirmationWindow ConfirmDialog = __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate);
                ConfirmDialog.Configure(MultiplayerModLocalization.UI.Multiplayer_Title, MultiplayerModLocalization.UI.Multiplayer_Text, true);
                return false;
            }

            /**
             * @brief
             * Show a message when returning to main menu, when needed
             */
            [HarmonyPatch(typeof(MainMenu), "Start")]
            [HarmonyPostfix]
            private static void Start(MainMenu __instance)
            {
                if(PauseMenuPatches.Message != null)
                {
                    __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate).Configure(MultiplayerModLocalization.UI.Disconnected, PauseMenuPatches.Message, true);
                    PauseMenuPatches.Message = null;
                }
            }
        }
    }
}

/* EOF */
