/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Assets management
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
using HarmonyLib;
using BepInEx;
using MMTools;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains the classes and code for the Unity Assets management.
 * 
 * @class Assets
 * The class for Scenes API management.
 */
namespace COTLMP.Api
{
    internal static class Assets
    {
        /*
         * @brief
         * Opens an assets bundle file and loads it into memory.
         * 
         * @param[in] BundleName
         * A string that points to the name of the bundle file to be
         * opened.
         * 
         * @returns
         * Returns the asset bundle object to the caller. NULL is returned
         * if said scene asset couldn't be found within the Assets folder.
         */
        public static AssetBundle OpenAssetBundleFile(string BundleName)
        {
            AssetBundle Bundle;
            string AssetsLocation;
            string AbsolutePath;

            /* Sanity check -- Make sure the Assets folder of the mod exists */
            AssetsLocation = Path.Combine(Plugin.CotlmpPathLocation, "Assets");
            COTLMP.Debug.Assertions.Assert(Directory.Exists(AssetsLocation), true, "Assets folder missing", "The Assets folder is either missing or corrupt, please re-install COTLMP!");

            /* Now build the absolute path to the UI assets bundle */
            AbsolutePath = Path.Combine(AssetsLocation, BundleName);
            COTLMP.Debug.PrintLogger.Print(DebugLevel.MESSAGE_LEVEL, DebugComponent.ASSETS_MANAGEMENT_COMPONENT, $"Absolute Path -> {AbsolutePath}");

            /* And load it from the absolute path */
            Bundle = AssetBundle.LoadFromFile(AbsolutePath);
            if (Bundle == null)
            {
                COTLMP.Debug.PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.ASSETS_MANAGEMENT_COMPONENT,
                                               $"Failed to load the bundle file -- {BundleName}!");
                return null;
            }

            return Bundle;
        }

        /*
         * @brief
         * Loads a scene and shows it to the screen, given its name.
         * 
         * @param[in] SceneName
         * A string to the name of the scene of which to be loaded.
         * 
         * @param[in] ActionToTakeCallback
         * An action callback provided by the caller. The method executes
         * this callback if the scene name is "Main Menu" of which it
         * performs a specific action depending on the pointed callback.
         * This parameter is optional and ignored for every other scene name.
         * 
         */
        public static void ShowScene(string SceneName, Action ActionToTakeCallback)
        {
            /*
             * The caller asked to load the main menu scene, use MMTools for that
             * as the main menu scene is built-in COTL natively.
             */
            if (SceneName == "Main Menu")
            {
                MMTools.MMTransition.Play(MMTransition.TransitionType.LoadAndFadeOut,
                                          MMTransition.Effect.BlackFade,
                                          SceneName,
                                          1,
                                          null,
                                          ActionToTakeCallback);
                return;
            }

            /* Otherwise use the scene manager to load whatever scene */
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
        }
    }
}

/* EOF */
