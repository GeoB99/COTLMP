/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Banner "Multiplayer Edition" header
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Data;
using HarmonyLib;
using BepInEx;
using I2.Loc;
using Lamb.UI;
using src.Extensions;
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
        /// A coroutine that forces the loading of game assets into memory.
        /// This is needed because joining a game server from the serverlist
        /// makes the game not loading the assets due to a peculiar behavior of
        /// SceneLoad() of the game.
        ///
        /// It first tries to check the current active scene to unload and based
        /// on that it loads whatever game asset depending on that current scene.
        /// The game assets are loaded ONLY if the previous active scene was Main
        /// Menu and the serverlist scene isn't named like that. Consequently,
        /// the game was started with unintialized assets.
        /// </summary>
        private static IEnumerator BannerEnumerator()
        {
            yield return MonoSingleton<UIManager>.Instance.LoadPersistentGameAssets().YieldUntilCompleted();
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
