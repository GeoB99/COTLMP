/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Server password connect prompt implementation
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Api;
using COTLMP.Data;
using COTLMP.Debug;
using HarmonyLib;
using I2.Loc;
using BepInEx;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Ui
{
    internal static class PasswordPrompt
    {
        private static Image PromptBox;
        private static TMP_Text PromptDescription;
        private static Button OkButton;
        private static Button CancelButton;
        private static TMP_InputField PromptInput;

        /*
         * @brief
         * The Cancel button callback handler. This is called whenever the player
         * clicks on "Cancel". The callback destroys the password prompt dialog box.
         */
        private static void CancelButtonHandler()
        {
            PromptInput.gameObject.SetActive(false);
            Object.Destroy(PromptInput);

            PromptBox.gameObject.SetActive(false);
            Object.Destroy(PromptBox);
        }

        /*
         * @brief
         * The OK button callback handler. This is called whenever the player
         * clicks on "OK". This submits the password written from the input
         * and checks it against the server's password for matching comparison.
         */
        private static void OkButtonHandler()
        {
            // TODO: Retrieve the password from the target server and implement hash checksums with SHA256
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "OkButtonHandler() called");
            return;
        }

        /*
         * @brief
         * Localizes the server password prompt dialog box to the
         * specific chosen language locale in the game.
         */
        private static void LocalizeUi()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "LocalizeUi() called");

            /* Localize the buttons */
            OkButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.PasswordPrompt.PasswordPrompt_OkButton;
            CancelButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.PasswordPrompt.PasswordPrompt_CancelButton;

            /* Localize the prompt description header */
            PromptDescription.text = MultiplayerModLocalization.UI.PasswordPrompt.PasswordPrompt_Description;
        }

        /*
         * @brief
         * Initializes the UI of the server password prompt box.
         */
        private static IEnumerator UiInitializationWorker()
        {
            /* Wait at least one frame for the UI game objects to be initialized */
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "UiInitializationWorker() called");
            yield return null;

            /* Cache all the UI gameobjects of the prompt box */
            PromptBox = GameObject.Find("PromptBox").GetComponent<Image>();
            COTLMP.Debug.Assertions.Assert(PromptBox != null, false, "PromptBox gameobject returned NULL!", null);

            CancelButton = GameObject.Find("CancelButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(CancelButton != null, false, "CancelButton gameobject returned NULL!", null);
            CancelButton.onClick.AddListener(CancelButtonHandler);

            OkButton = GameObject.Find("OkButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(OkButton != null, false, "OkButton gameobject returned NULL!", null);
            CancelButton.onClick.AddListener(OkButtonHandler);

            PromptDescription = GameObject.Find("PromptDescription").GetComponent<TMP_Text>();
            COTLMP.Debug.Assertions.Assert(PromptDescription != null, false, "PromptDescription gameobject returned NULL!", null);

            PromptInput = GameObject.Find("PromptInput").GetComponent<TMP_InputField>();
            COTLMP.Debug.Assertions.Assert(PromptInput != null, false, "PromptInput gameobject returned NULL!", null);

            /*
             * Force focus on the input field. This is so that the player
             * doesn't have to click on the input field box to type the password.
             */
            PromptInput.ActivateInputField();

            /* Localize the prompt dialog box */
            LocalizeUi();
            yield break;
        }

        /*
         * @brief
         * Displays the server password prompt box when a player joins a
         * password-protected server.
         */
        public static void DisplayUi()
        {
            COTLMP.Api.Assets.ShowScene("PasswordPromptUI", true, null);
            Plugin.MonoInstance.StartCoroutine(UiInitializationWorker());
        }
    }
}

/* EOF */
