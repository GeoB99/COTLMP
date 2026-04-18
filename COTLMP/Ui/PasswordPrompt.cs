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

    /*
     * @brief
     * Type of the server password dialog box to be displayed
     * onto the screen.
     *
     * @field PromptPassword
     * The password prompt dialog box (asking the player to type
     * the password to join).
     *
     * @field InvalidPassword
     * The invalid password disclaimer dialog box.
     */
    public enum DIALOG_BOX_TYPE
    {
        PromptPassword = 0,
        InvalidPassword
    }

    internal static class PasswordPrompt
    {
        private static Image PromptBox, InvalidPasswordBox;
        private static TMP_Text PromptHeader, InvalidPasswordHeader;
        private static Button OkButton, OkButtonInvalidPassowrd;
        private static Button CancelButton;
        private static TMP_InputField PromptInput;

        /*
         * @brief
         * The Cancel button callback handler. This is called whenever the player
         * clicks on "Cancel". The callback destroys the password prompt dialog box.
         */
        private static void CancelButtonHandler()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "CancelButtonHandler() called");
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
         * The OK button callback handler. This is called whenever the player
         * clicks on "OK". The callback destroys the invalid password dialog box
         * and displays back the password prompt box.
         */
        private static void OkButtonHnadlerInvalidPassword()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "OkButtonHnadlerInvalidPassword() called");
            InvalidPasswordBox.gameObject.SetActive(false);
            Object.Destroy(InvalidPasswordBox);
            PromptInput.gameObject.SetActive(true);
            PromptBox.gameObject.SetActive(true);
            PromptInput.ActivateInputField();
        }

        /*
         * @brief
         * Localizes the server password prompt dialog box to the
         * specific chosen language locale in the game.
         *
         * @param[in] Type
         * The type of the server password dialog box to be localized,
         * at the time of the dialog box being displayed.
         */
        private static void LocalizeUi(DIALOG_BOX_TYPE Type)
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "LocalizeUi() called");

            /* Localize the invalid password dialog box */
            if (Type.Equals(DIALOG_BOX_TYPE.InvalidPassword))
            {
                OkButtonInvalidPassowrd.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.PasswordPrompt.PasswordPrompt_OkButton;
                InvalidPasswordHeader.text = MultiplayerModLocalization.UI.PasswordPrompt.PasswordPrompt_InvalidPassword;
                return;
            }

            /* Localize the password prompt dialog box instead */
            PromptHeader.text = MultiplayerModLocalization.UI.PasswordPrompt.PasswordPrompt_Description;
            OkButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.PasswordPrompt.PasswordPrompt_OkButton;
            CancelButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.PasswordPrompt.PasswordPrompt_CancelButton;
        }

        /*
         * @brief
         * Initializes the UI of the server password prompt box.
         *
         * @param[in] Type
         * The type of the server password dialog box of which
         * the worker is to initialize the gamne objects and
         * other stuff accordingly.
         */
        private static IEnumerator UiInitializationWorker(DIALOG_BOX_TYPE Type)
        {
            /* Wait at least one frame for the UI game objects to be initialized */
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "UiInitializationWorker() called");
            yield return null;

            /* Cache the gameobjects of the invalid password dialog box */
            if (Type.Equals(DIALOG_BOX_TYPE.InvalidPassword))
            {
                InvalidPasswordBox = GameObject.Find("DialogBox").GetComponent<Image>();
                COTLMP.Debug.Assertions.Assert(InvalidPasswordBox != null, false, "InvalidPasswordBox gameobject returned NULL!", null);

                InvalidPasswordHeader = GameObject.Find("HeaderText").GetComponent<TMP_Text>();
                COTLMP.Debug.Assertions.Assert(InvalidPasswordHeader != null, false, "InvalidPasswordHeader gameobject returned NULL!", null);

                OkButtonInvalidPassowrd = GameObject.Find("OkButtonInvalid").GetComponent<Button>();
                COTLMP.Debug.Assertions.Assert(OkButtonInvalidPassowrd != null, false, "OkButtonInvalidPassowrd gameobject returned NULL!", null);
                OkButtonInvalidPassowrd.onClick.AddListener(OkButtonHnadlerInvalidPassword);

                LocalizeUi(DIALOG_BOX_TYPE.InvalidPassword);

                /*
                 * Ensure that neither the passowrd prompt and the invalid password overlap
                 * with each other so hide the password prompt box from display view.
                 */
                PromptInput.gameObject.SetActive(false);
                PromptBox.gameObject.SetActive(false);
                yield break;
            }

            /* Cache all the UI gameobjects of the prompt box */
            PromptBox = GameObject.Find("PromptBox").GetComponent<Image>();
            COTLMP.Debug.Assertions.Assert(PromptBox != null, false, "PromptBox gameobject returned NULL!", null);

            CancelButton = GameObject.Find("CancelButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(CancelButton != null, false, "CancelButton gameobject returned NULL!", null);
            CancelButton.onClick.AddListener(CancelButtonHandler);

            OkButton = GameObject.Find("OkButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(OkButton != null, false, "OkButton gameobject returned NULL!", null);
            OkButton.onClick.AddListener(OkButtonHandler);

            PromptHeader = GameObject.Find("PromptDescription").GetComponent<TMP_Text>();
            COTLMP.Debug.Assertions.Assert(PromptHeader != null, false, "PromptHeader gameobject returned NULL!", null);

            PromptInput = GameObject.Find("PromptInput").GetComponent<TMP_InputField>();
            COTLMP.Debug.Assertions.Assert(PromptInput != null, false, "PromptInput gameobject returned NULL!", null);

            /*
             * Force focus on the input field. This is so that the player
             * doesn't have to click on the input field box to type the password.
             */
            PromptInput.ActivateInputField();

            /* Localize the prompt dialog box */
            LocalizeUi(DIALOG_BOX_TYPE.PromptPassword);
            yield break;
        }

        /*
         * @brief
         * Displays the server password prompt box when a player joins a
         * password-protected server.
         *
         * @param[in] Type
         * The type of the server password dialog box to be displayed.
         */
        public static void DisplayUi(DIALOG_BOX_TYPE Type)
        {
            bool ShowPasswordPromptUi = true;

            if (Type.Equals(DIALOG_BOX_TYPE.InvalidPassword))
            {
                ShowPasswordPromptUi = false;
            }

            COTLMP.Api.Assets.ShowScene(ShowPasswordPromptUi ? "PasswordPromptUI" : "InvalidPasswordUI", true, null);
            Plugin.MonoInstance.StartCoroutine(UiInitializationWorker(ShowPasswordPromptUi ? DIALOG_BOX_TYPE.PromptPassword : DIALOG_BOX_TYPE.InvalidPassword));
        }
    }
}

/* EOF */
