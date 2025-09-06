/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Banner "Multiplayer Edition" header
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
using COTLMP.Data;
using BepInEx;
using HarmonyLib;
using I2.Loc;
using Lamb.UI;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains classed and code for the UI interface of the mod,
 * namely the banner.
 * 
 * @class Banner
 * Main class of which it implements the banner header of the mod.
 */
namespace COTLMP.Ui
{
    [HarmonyPatch]
    internal static class Banner
    {
        /*
         * @brief
         * Patches the private DLC check edition method, of which it replaces
         * the banner header with ours.
         * 
         * @param[in] __instance
         * The current instance value of the method being patched.
         * 
         * @return
         * Returns TRUE if tthe original method of the game is to be executed.
         * FALSE if our method is to be executed instead.
         */
        [HarmonyPatch(typeof(ShowIfSpecialEdition), "WaitForDLCCheck")]
        [HarmonyPrefix]
        private static bool BannerEditionPatch(ShowIfSpecialEdition __instance)
        {
            /* The mod is currently not executing so run the original method instead */
            if (!Plugin.Globals.EnableMod)
            {
                return true;
            }

            /* Replace the banner header */
            __instance._localize.Term = "UI/Banner";
            __instance._text.text = MultiplayerModLocalization.UI.Multiplayer_Banner;
            return false;
        }
    }
}

/* EOF */
