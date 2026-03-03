/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the network class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMP.Data;
using COTLMP.Ui;
using UnityEngine;
using UnityEngine.SceneManagement;

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * The namespace for all network-related classes, enums and structs
 */
namespace COTLMP.Network
{
    /**
     * @brief
     * The main class for network features
     */
    internal class Network
    {
        /**
         * @brief
         * If the scene loaded is main menu, stop the integrated server and
         * disconnect the active client session.
         */
        private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            if (scene.name.Equals("Main Menu"))
            {
                // Disconnect client if connected
                if (PlayerSync.ActiveClient != null)
                {
                    PlayerSync.ActiveClient.Disconnect();
                    PlayerSync.SetClient(null);
                }

                // Stop the hosted server
                PauseMenuPatches.Quitting = true;
                PauseMenuPatches.Server?.Dispose();
                PauseMenuPatches.Quitting = false;

                InternalData.IsMultiplayerSession = false;
            }
        }

        /**
         * @brief
         * On game quitting, stop the integrated server and disconnect client.
         */
        private static void OnQuitting()
        {
            PlayerSync.ActiveClient?.Disconnect();
            PlayerSync.SetClient(null);

            PauseMenuPatches.Quitting = true;
            PauseMenuPatches.Server?.Dispose();

            InternalData.IsMultiplayerSession = false;
        }

        /**
         * @brief
         * Initialize the network components.
         * Attaches the PlayerSync MonoBehaviour to the mod's mono instance so
         * it ticks every frame alongside the plugin lifecycle.
         */
        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting    += OnQuitting;

            // Attach PlayerSync to the persistent Plugin MonoBehaviour
            if (Plugin.MonoInstance != null)
                Plugin.MonoInstance.gameObject.AddComponent<PlayerSync>();
        }
    }
}

/* EOF */
