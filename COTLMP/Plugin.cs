/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Main plugin startup code file
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP.Version;
using COTLMP.Localization;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

/* NAMESPACES *****************************************************************/

/* Main COTL MP plugin startup namespace */
namespace COTLMP;

/* CLASSES & CODE *************************************************************/

/* Initialize the base BepInEx attributes of the mod plug-in */ 
[BepInPlugin(COTLMP.Version.ModVersion.CotlMpGuid, COTLMP.Version.ModVersion.CotlMpName, COTLMP.Version.ModVersion.CotlMpVer)]

/*
 * @brief
 * Creates the COTL MP plug-in instance and initializes the critical
 * COTL MP data.
 */
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
        
    /*
     * @brief
     * Executes initialization code as the mod is being loaded
     * by BepInEx.
     */
    private void Awake()
    {
        /* Start patching the game assembly with our code */
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        /* Cache the logger field so that the COTL MP mod can use it for debug support */
        Logger = base.Logger;

        /* Load the localizations of the mod */
        COTLMP.Localization.LocaleManager.LoadLocale("English");

        /* Log to the debugger that our mod is loaded */
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
}

/* EOF */
