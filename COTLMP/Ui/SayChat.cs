/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Say chat box support
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
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Rewired;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Ui
{
    public class SayChat : MonoBehaviour
    {
        public static bool BoxSpawned;
        private static TMP_InputField SayInput;
        private static Image SayBox;
        private static Component SayComponent;
        private static Player PlayerInstance;

        /*
         * @brief
         * Called by the Update() method of the core saychat box mechanism whenever a
         * message is to be broadcasted to all players in a server.
         */
        private static IEnumerator BroadcastSayMessageWorker(string Message)
        {
            // TODO: Dispatch a network message to the server with the supplied message and player name
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "BroadcastSayMessage() called");

            /* Destroy the chatbox */
            Cleanup();
            yield break;
        }

        /*
         * @brief
         * The core initialization saychat box worker. It initializes the fields and other stuff
         * of the inputfield of the saychat box as it gets displayed to the screen.
         */
        private static IEnumerator SayChatDisplayWorker()
        {
            /* Wait for at least one frame for Unity to initialize the chatbox gameobject */
            COTLMP.Debug.PrintLogger.PrintVerbose(DebugLevel.MESSAGE_LEVEL, DebugComponent.UI_COMPONENT, "SayChatDisplayWorker() called");
            yield return null;

            /* Cache the say chatbox and the corresponding panel */
            SayInput = GameObject.Find("SayChat").GetComponent<TMP_InputField>();
            COTLMP.Debug.Assertions.Assert(SayInput != null, false, "SayInput gameobject returned NULL!", null);

            SayBox = GameObject.Find("SayPanel").GetComponent<Image>();
            COTLMP.Debug.Assertions.Assert(SayBox != null, false, "SayBox gameobject returned NULL!", null);

            /*
             * Set a limit of characters a player can type. This avoid naughty players
             * who spams very long messages in the say chat.
             */
            SayInput.characterLimit = COTLMP.Data.InternalData.MaxSayCharsLimit;

            /* Force focus on the chatbox */
            SayInput.ActivateInputField();

            /*
             * Disable all the controller maps, including the keyboard. This makes the
             * player stopping moving or showing any unwanted UI when typing a message.
             */
            PlayerInstance.controllers.maps.SetAllMapsEnabled(false);
            yield break;
        }

        /*
         * @brief
         * Destroys the saychat box that's been previously spawned by the player client.
         */
        private static void Cleanup()
        {
            /* Ensure we aren't called when no saychat box was ever spawned */
            COTLMP.Debug.Assertions.Assert(BoxSpawned != false, false, "Cleanup() called when no saychat box was ever spawned!", null);

            /* Destroy the input field of the saychat box */
            SayInput.gameObject.SetActive(false);
            Object.Destroy(SayInput);

            /* And that of the panel box as well */
            SayBox.gameObject.SetActive(false);
            Object.Destroy(SayBox);
            BoxSpawned = false;

            /* Reenable all the controller maps back */
            PlayerInstance.controllers.maps.SetAllMapsEnabled(true);
        }

        /*
         * @brief
         * Pools for key events so that the saychat box gets spawned and destroyed
         * at the right key press events. Unity calls this function on every game frame.
         */
        private void Update()
        {
            string CapturedMessage;

            /*
             * The player is currently not in session yet, do not display the say chat box,
             * but let Unity keep calling this method each frame to pool for key events.
             */
            if (!Plugin.GlobalsInternal.InGameSession)
            {
                return;
            }

            /* The player is in session, check if he actually pressed the Y key */
            if (Input.GetKeyDown(KeyCode.Y) &&
                BoxSpawned == false)
            {
                /* Display the say chat box */
                COTLMP.Api.Assets.ShowScene("SayChatUI", true, null);

                /* Acknowledge ourselves the saychat box has been spawned and setup the saychat worker */
                BoxSpawned = true;
                Plugin.MonoInstance.StartCoroutine(SayChatDisplayWorker());
                return;
            }

            /* Has the saychat box been spawned? */
            if (BoxSpawned == true)
            {
                /* It is, cleanup the saychat box if the player pressed ESC */
                if (Input.GetKeyDown(KeyCode.Escape) ||
                    Input.GetKeyDown(KeyCode.Return))
                {
                    /* The player pressed ENTER, send the message across all connected player clients */
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        CapturedMessage = SayInput.text;
                        Plugin.MonoInstance.StartCoroutine(BroadcastSayMessageWorker(CapturedMessage));
                        return;
                    }

                    /* Destroy the saychat box and its associated child objects */
                    Cleanup();
                }
            }
        }

        /*
         * @brief
         * Shuts down the saychat system mechanism, typically when a
         * player leaves a server.
         */
        public static void Shutdown()
        {
            /* Cease any coroutine execution if any */
            Plugin.MonoInstance.StopCoroutine(SayChatDisplayWorker());
            Plugin.MonoInstance.StopCoroutine(BroadcastSayMessageWorker(null));

            /* Close our attached component */
            Object.Destroy(SayComponent);

            /* Acknowledge ourselves as the saychat box is not spawned */
            BoxSpawned = false;
        }

        /*
         * @brief
         * Starts up the saychat system mechanism.
         */
        public static void StartSayChat()
        {
            /*
             * Create a component by attaching our SayChat class.
             * This will make the class as a registered instance of
             * MonoBehavior so we can do stuff like pooling for key
             * events each frame.
             *
             * Also get the input instance of the current client player.
             * This is so we can disable controller maps (like the keyboard)
             * later on.
             */
            BoxSpawned = false;
            SayComponent = Plugin.MonoInstance.gameObject.AddComponent<SayChat>();
            PlayerInstance = ReInput.players.GetPlayer(0);
        }
    }
}

/* EOF */
