/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Banner "Multiplayer Edition" header
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Data;
using BepInEx;
using HarmonyLib;
using I2.Loc;
using Lamb.UI;
using System.Collections;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Ui
{
    /// <summary>
    /// Main class of which it implements the banner header of the mod.
    /// </summary>
    [HarmonyPatch]
    internal static class Banner
    {
        /// <summary>
        /// A dummy IEnumerator method that is used to replace the
        /// returned value which represents a coroutine.
        /// </summary>
        private static IEnumerator BannerEnumerator()
        {
            yield break;
        }

        /// <summary>
        /// Patches the private DLC check edition method, of which it replaces the banner header with ours.
        /// </summary>
        /// <param name = "__instance">The current instance value of the method being patched.</param>
        /// <param name = "__result">The current result value being returned. Typically the original method returns a IEnumerator.</param>
        /// <returns>Returns TRUE if tthe original method of the game is to be executed. FALSE if our method is to be executed instead.</returns>
        [HarmonyPatch(typeof(ShowIfSpecialEdition), "WaitForDLCCheck")]
        [HarmonyPrefix]
        private static bool BannerEditionPatch(ShowIfSpecialEdition __instance, ref IEnumerator __result)
        {
            /* Replace the banner header */
            __instance._localize.Term = "UI/Banner";
            __instance._text.text = MultiplayerModLocalization.UI.Multiplayer_Banner;
            __result = BannerEnumerator();
            return false;
        }
    }
}

/* EOF */
