/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Main plugin startup code file
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using COTLMP.Api;
using COTLMP.Data;
using COTLMP.Ui;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MMTools;
using UnityEngine.SceneManagement;

/* NAMESPACES *****************************************************************/

/* Main COTL MP plugin startup namespace */
namespace COTLMP;

/* CLASSES & CODE *************************************************************/

/* Initialize the base BepInEx attributes of the mod plug-in */ 
[BepInPlugin(COTLMP.Data.Version.CotlMpGuid, COTLMP.Data.Version.CotlMpName, COTLMP.Data.Version.CotlMpVer)]

/* Load the COTL API plugin as a hard dependency */
[BepInDependency("io.github.xhayper.COTL_API", BepInDependency.DependencyFlags.HardDependency)]

/*
 * @brief
 * Creates the COTL MP plug-in instance and initializes the critical
 * COTL MP data.
 */
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static new ConfigFile Config;
    internal static ModDataGlobals Globals;
    internal static InternalData GlobalsInternal;
    private Harmony HarmonyInstance;

    /*
     * @brief
     * Executes initialization code as the mod is being loaded
     * by BepInEx.
     */
    private void Awake()
    {
        Object SettingData;
        int MaxPlayers;
        string ServerName, PlayerName, GameMode;
        bool ToggleMod, VoiceChat;

        /*
         * Start patching the game assembly with our code
         * and cache a harmony instance of this mod.
         */
        HarmonyInstance = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        /* Cache the plugin class methods so that the COTL MP mod can use them across different modules */
        Logger = base.Logger;
        Config = base.Config;

        /* Fetch the flags and switches of the mod, reserved for internal use */
        GlobalsInternal = new InternalData();

        /* Load the localizations of the mod */
        COTLMP.Api.Localization.LoadLocale("English");

        /*
         * Initialize the settings of the mod to defaults in following
         * order, first the "Toggle Mod" setting. If the user configuration
         * file has already been created the function will simply load
         * the configuration entry of each setting.
         */
        SettingData = COTLMP.Api.Configuration.CreateSetting(CONFIGURATION_SECTION.ModSettings,
                                                             "Toggle Mod",
                                                             "Enables or Disables the Multiplayer mod. This setting has to be changed manually here!",
                                                             true);
        if (SettingData == null)
        {
            Logger.LogFatal("Failed to set default or load the \"Mod Toggle\" setting!");
            HarmonyInstance.UnpatchSelf();
            return;
        }

        /* Retrieve the setting data from the setting object */
        ToggleMod = COTLMP.Api.Configuration.GetSettingData<bool>(SettingData);

        /* Initialize the "Game Mode" setting */
        SettingData = COTLMP.Api.Configuration.CreateSetting(CONFIGURATION_SECTION.ServerSettings,
                                                             "Game Mode",
                                                             "The game mode for the multiplayer server. Possible values are: Standard, Boss Fight, Deathmatch, Zombies!",
                                                             "Standard");
        if (SettingData == null)
        {
            Logger.LogFatal("Failed to set default or load the \"Game Mode\" setting!");
            HarmonyInstance.UnpatchSelf();
            return;
        }

        /* Retrieve the setting data from the setting object */
        GameMode = COTLMP.Api.Configuration.GetSettingData<string>(SettingData);

        /* Initialize the "Player Name" setting */
        SettingData = COTLMP.Api.Configuration.CreateSetting(CONFIGURATION_SECTION.ServerSettings,
                                                             "Player Name",
                                                             "The name of the player in-game",
                                                             "The Player");
        if (SettingData == null)
        {
            Logger.LogFatal("Failed to set default or load the \"Player Name\" setting!");
            HarmonyInstance.UnpatchSelf();
            return;
        }

        /* Retrieve the setting data from the setting object */
        PlayerName = COTLMP.Api.Configuration.GetSettingData<string>(SettingData);

        /* Initialize the "Server Name" setting */
        SettingData = COTLMP.Api.Configuration.CreateSetting(CONFIGURATION_SECTION.ServerSettings,
                                                             "Server Name",
                                                             "The name of the server",
                                                             "Cult of the Lamb Server");
        if (SettingData == null)
        {
            Logger.LogFatal("Failed to set default or load the \"Server Name\" setting!");
            HarmonyInstance.UnpatchSelf();
            return;
        }

        /* Retrieve the setting data from the setting object */
        ServerName = COTLMP.Api.Configuration.GetSettingData<string>(SettingData);

        /* Initialize the "Max Players In Server" setting */
        SettingData = COTLMP.Api.Configuration.CreateSetting(CONFIGURATION_SECTION.ServerSettings,
                                                             "Max Players",
                                                             "The maximum count of players that can join a server",
                                                             8);
        if (SettingData == null)
        {
            Logger.LogFatal("Failed to set default or load the \"Max Players\" setting!");
            HarmonyInstance.UnpatchSelf();
            return;
        }

        /* Retrieve the setting data from the setting object */
        MaxPlayers = COTLMP.Api.Configuration.GetSettingData<int>(SettingData);

        /* Initialize the "Enable Voice Chat" setting */
        SettingData = COTLMP.Api.Configuration.CreateSetting(CONFIGURATION_SECTION.ServerSettings,
                                                             "Toggle Voice Chat",
                                                             "Enables or Disables voice chat in a server",
                                                             false);
        if (SettingData == null)
        {
            Logger.LogFatal("Failed to set default or load the \"Toggle Voice Chat\" setting!");
            HarmonyInstance.UnpatchSelf();
            return;
        }

        /* Retrieve the setting data from the setting object */
        VoiceChat = COTLMP.Api.Configuration.GetSettingData<bool>(SettingData);

        /* Now store all the cached settings into the globals data store */
        Globals = new ModDataGlobals(ToggleMod,
                                     GameMode,
                                     PlayerName,
                                     ServerName,
                                     MaxPlayers,
                                     VoiceChat);

        /* Initialize the Settings UI */
        COTLMP.Ui.Settings.InitializeUI();

        UnityEngine.Application.quitting += () => {
            PauseMenuPatches.Quitting = true;
            PauseMenuPatches.Server?.Dispose();
        };

        SceneManager.sceneLoaded += (scene, _) => {
            if (scene.name.Equals("Main Menu"))
            {
                // set the quitting flag temporarily so it doesn't try to transition to the main menu on server stop
                PauseMenuPatches.Quitting = true;
                PauseMenuPatches.Server?.Dispose();
                PauseMenuPatches.Quitting = false;
            }
        };

        /* Log to the debugger that our mod is loaded */
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    /*
     * @brief
     * Performs additional tasks after the successful initialization
     * of the mod.
     */
    private void OnEnable()
    {
        /*
         * The user wants the mod to be disabled so stop the execution
         * of all the patches that have been loaded previously.
         */
        if (!Globals.EnableMod)
        {
            Logger.LogWarning("The user disabled the mod, stopping execution of the mod...");
            HarmonyInstance.UnpatchSelf();
            return;
        }

        Logger.LogMessage($"{HarmonyInstance.GetPatchedMethods().Count()} patches have been applied!");
    }

    /*
     * @brief
     * Unloads all the patches hooked into game on quit event
     * of the game.
     */
    private void OnDisable()
    {
        Logger.LogMessage($"Unloading {MyPluginInfo.PLUGIN_GUID}");
        HarmonyInstance.UnpatchSelf();
        Logger.LogMessage("Mod has been unloaded!");
    }
}

/* EOF */
