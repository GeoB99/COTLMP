/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     DLCs patches
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
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
         * 
         * @param[in] __result
         * The current result value being returned. Typically the original
         * method returns a boolean.
         * 
         * @return
         * Returns TRUE if the original method of the game is to be executed.
         * FALSE if our method is to be executed instead.
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
         * 
         * @param[in] __result
         * The current result value being returned. Typically the original
         * method returns a boolean.
         * 
         * @return
         * Returns TRUE if the original method of the game is to be executed.
         * FALSE if our method is to be executed instead.
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
         * 
         * @param[in] __result
         * The current result value being returned. Typically the original
         * method returns a boolean.
         * 
         * @return
         * Returns TRUE if the original method of the game is to be executed.
         * FALSE if our method is to be executed instead.
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
         * 
         * @param[in] __result
         * The current result value being returned. Typically the original
         * method returns a boolean.
         * 
         * @return
         * Returns TRUE if the original method of the game is to be executed.
         * FALSE if our method is to be executed instead.
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
         * 
         * @param[in] __result
         * The current result value being returned. Typically the original
         * method returns a boolean.
         * 
         * @return
         * Returns TRUE if the original method of the game is to be executed.
         * FALSE if our method is to be executed instead.
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
         * Patches the authentication of Woolhaven DLC method, of which
         * we are going to force disable it.
         * 
         * @param[in] __result
         * The current result value being returned. Typically the original
         * method returns a boolean.
         * 
         * @return
         * Returns TRUE if the original method of the game is to be executed.
         * FALSE if our method is to be executed instead.
         */
        [HarmonyPatch(typeof(GameManager), "AuthenticateMajorDLC")]
        [HarmonyPrefix]
        private static bool DisableWoolhavenDLC(ref bool __result)
        {
            __result = false;
            return false;
        }
    }
}

/* EOF */
