/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     DLCs patches
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Data;
using COTLMP.Debug;
using HarmonyLib;
using BepInEx;
using System;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains the classes and code for the managemnt of DLCs of the game.
 * 
 * @class DlcPatches
 * The callbacks class which contains all the Harmony patches for DLC stuff.
 */
namespace COTLMP.Game
{
    [HarmonyPatch]
    internal static class DlcPatches
    {
        /*
         * @brief
         * Patches the authentication of the Heretic DLC method, of which
         * we are going to force disable it.
         */
        [HarmonyPatch(typeof(GameManager), "AuthenticateHereticDLC")]
        [HarmonyPrefix]
        private static bool DisableHereticDLC(ref bool __result)
        {
            __result = false;
            return false;
        }

        /*
         * @brief
         * Patches the authentication of the Cultist DLC method, of which
         * we are going to force disable it.
         */
        [HarmonyPatch(typeof(GameManager), "AuthenticateCultistDLC")]
        [HarmonyPrefix]
        private static bool DisableCultistDLC(ref bool __result)
        {
            __result = false;
            return false;
        }

        /*
         * @brief
         * Patches the authentication of the Sinful DLC method, of which
         * we are going to force disable it.
         */
        [HarmonyPatch(typeof(GameManager), "AuthenticateSinfulDLC")]
        [HarmonyPrefix]
        private static bool DisableSinfulDLC(ref bool __result)
        {
            __result = false;
            return false;
        }

        /*
         * @brief
         * Patches the authentication of the Pilgrim DLC method, of which
         * we are going to force disable it.
         */
        [HarmonyPatch(typeof(GameManager), "AuthenticatePilgrimDLC")]
        [HarmonyPrefix]
        private static bool DisablePilgrimDLC(ref bool __result)
        {
            __result = false;
            return false;
        }

        /*
         * @brief
         * Patches the authentication of the the early purchase DLC method,
         * of which we are going to force disable it.
         */
        [HarmonyPatch(typeof(GameManager), "AuthenticatePrePurchaseDLC")]
        [HarmonyPrefix]
        private static bool DisablePrePurchaseDLC(ref bool __result)
        {
            __result = false;
            return false;
        }

        /*
         * @brief
         * Patches the Woolhaven (Major DLC) authentication so that:
         *   - In the main menu / outside a game session it ALWAYS returns false,
         *     ensuring the base-game main-menu scene is loaded for all users and
         *     preventing a DLC-specific menu layout from breaking the Multiplayer
         *     button injection.
         *   - During an active multiplayer session it also returns false so that
         *     no player gains access to Woolhaven DLC areas or content.
         *
         * Single-player users who own the DLC will find Woolhaven unavailable
         * while this mod is installed; they should disable the mod to play the
         * DLC story content in solo mode.
         */
        [HarmonyPatch(typeof(GameManager), "AuthenticateMajorDLC")]
        [HarmonyPrefix]
        private static bool DisableWoolhavenDLC(ref bool __result)
        {
            // Block during multiplayer or in the main menu (no session started)
            if (InternalData.IsMultiplayerSession || !SessionHandler.HasSessionStarted)
            {
                __result = false;
                return false;
            }
            // Single-player in-session: let the original Steam check run
            return true;
        }

        /*
         * @brief
         * Blocks the Woolhaven goop door interaction during any active multiplayer
         * session so players without the DLC cannot accidentally enter DLC areas
         * if they were somehow unlocked.
         */
        [HarmonyPatch(typeof(BaseGoopDoor), "CanWoolhavenDoorOpen")]
        [HarmonyPrefix]
        private static bool BlockWoolhavenDoorInMP(ref bool __result)
        {
            if (!InternalData.IsMultiplayerSession) return true;
            __result = false;
            return false;
        }

        /*
         * @brief
         * Prevent the Woolhaven door lock from ever opening during multiplayer.
         */
        [HarmonyPatch(typeof(BaseGoopDoor), "CheckWoolhavenDoor")]
        [HarmonyPrefix]
        private static bool BlockCheckWoolhavenDoor()
        {
            return !InternalData.IsMultiplayerSession;
        }
    }
}

/* EOF */
