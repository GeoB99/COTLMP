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

        /*
         * @brief
         * Main UI element handler binding routine which is executed when
         * the server list UI is displayed.
         */
        private static IEnumerator BindUiElementsToHandlers()
        {
            /*
             * Wait for at least one frame for Unity to initialize all the UI elements
             * and then proceed to bind them to respective handlers.
             */
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "BindUiElements() called");
            yield return null;

            /* Bind the "Back" button to its handler */
            BackButton = GameObject.Find("BackButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(BackButton != null, false, "BackButton gameobject returned NULL!", null);
            BackButton.onClick.AddListener(BackButtonHandler);
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
             * Setup a coroutine to bind each UI element (buttons, list views, etc.) to
             * their respective handlers. The reason we use a coroutine here is because
             * ShowScene() might not initialize all the UI elements of a scene immediately
             * and therefore we could incur in a runtime error if we were to bind the UI
             * elements right away.
             */
            Plugin.MonoInstance.StartCoroutine(BindUiElementsToHandlers());
        }
    }
}

/* EOF */
