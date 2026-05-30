/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Server connection (by IP or server entry) support
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
using HarmonyLib;
using BepInEx;
using I2.Loc;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Ui
{
    public static class JoinServer
    {
        private static Image DialogBox;
        private static Button ConnectButton;
        private static Button CancelButton;
        private static TMP_InputField IpInputField;
        private static TMP_Text Description;

        /// <summary>
        /// Main handler that performs connection with the server given
        /// its IP address.
        /// </summary>
        private static void ConnectToServer()
        {
            // TODO
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "ConnectToServer() called");
            return;
        }

        /// <summary>
        /// Main handler for the Cancel button, of which it closes the dialog
        /// box that's been displayed to the screen.
        /// </summary>
        private static void CancelButtonHandler()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "CancelButtonHandler() called");
            IpInputField.gameObject.SetActive(false);
            Object.Destroy(IpInputField);

            DialogBox.gameObject.SetActive(false);
            Object.Destroy(DialogBox);
        }

        /// <summary>
        /// Localizes the UI into the current used locale of the game.
        /// </summary>
        private static void LocalizeUi()
        {
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "LocalizeUi() called");

            Description.text = MultiplayerModLocalization.UI.ConnectByIp.ConnectByIp_Description;
            ConnectButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ConnectByIp.ConnectByIp_ConnectButton;
            CancelButton.GetComponentInChildren<TMP_Text>().text = MultiplayerModLocalization.UI.ConnectByIp.ConnectByIp_CancelButton;
        }

        /// <summary>
        /// Main worker that initializes the UI gameobjects of the scene.
        /// </summary>
        private static IEnumerator UiInitWorker()
        {
            /* Wait at least one frame for the UI game objects to be initialized */
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "UiInitWorker() called");
            yield return null;

            /* Initialize the UI gameobjects and localize the UI resources as well */
            DialogBox = GameObject.Find("DialogBox").GetComponent<Image>();
            COTLMP.Debug.Assertions.Assert(DialogBox != null, false, "DialogBox gameobject returned NULL!", null);

            ConnectButton = GameObject.Find("ConnectButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(ConnectButton != null, false, "ConnectButton gameobject returned NULL!", null);
            ConnectButton.onClick.AddListener(ConnectToServer);

            CancelButton = GameObject.Find("CancelButton").GetComponent<Button>();
            COTLMP.Debug.Assertions.Assert(CancelButton != null, false, "CancelButton gameobject returned NULL!", null);
            CancelButton.onClick.AddListener(CancelButtonHandler);

            Description = GameObject.Find("Description").GetComponent<TMP_Text>();
            COTLMP.Debug.Assertions.Assert(Description != null, false, "Description gameobject returned NULL!", null);

            IpInputField = GameObject.Find("IpInput").GetComponent<TMP_InputField>();
            COTLMP.Debug.Assertions.Assert(IpInputField != null, false, "IpInputField gameobject returned NULL!", null);
            IpInputField.ActivateInputField();

            LocalizeUi();
            yield break;
        }

        /// <summary>
        /// Shows the "Connect by IP" dialog box to the screen.
        /// </summary>
        public static void DisplayUi()
        {
            COTLMP.Api.Assets.ShowScene("JoinIpUI", true, null);
            Plugin.MonoInstance.StartCoroutine(UiInitWorker());
        }
    }
}

/* EOF */
