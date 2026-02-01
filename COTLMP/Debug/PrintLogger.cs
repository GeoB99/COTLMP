/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Debug support and logging routines
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using BepInEx.Logging;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains the classes and code for the debugging mechanism
 * for the mod.
 * 
 * @class PrintLogger
 * Implements debug log output methods.
 */
namespace COTLMP.Debug
{
    /*
     * @brief
     * Debugging log levels enumeration. Each level represents
     * a different priority and meaning for the debug information
     * being output to the logger.
     * 
     * @field INFO_LEVEL
     * Indicates the debug output is an informational log.
     * 
     * @field WARNING_LEVEL
     * Indicates the debug output is a warning log.
     * 
     * @field ERROR_LEVEL
     * Indicates the debug output is an error log, typically used
     * by code of which conditions fail or are not met.
     *
     * @field FATAL_LEVEL
     * Indicates the debug output is a fatal error log. Usually this is
     * used to indicate a certain piece of code has failed to do its job
     * and the mod will prematurely fail to operate properly.
     * 
     * @field MESSAGE_LEVEL
     * Indicates the debug output is a message log. The difference between
     * INFO_LEVEL and this one is that a message log is output to the
     * interest of the user in the console while INFO_LEVEL is a low
     * level priority log used to mainly display debug or informational
     * stuff.
     */
    public enum DebugLevel
    {
        INFO_LEVEL = 0,
        WARNING_LEVEL,
        ERROR_LEVEL,
        FATAL_LEVEL,
        MESSAGE_LEVEL
    }

    /*
     * @brief
     * Components of the mod of which they log out debug information.
     * 
     * @field INIT_COMPONENT
     * Mod startup initialization component.
     * 
     * @field UI_COMPONENT
     * UI module component of the mod.
     * 
     * @field NETWORK_STACK_COMPONENT
     * Network (Server/Client) multiplayer component of the mod.
     *
     * @field LOCALIZATION_COMPONENT
     * Localization API component of the mod.
     * 
     * @field CONFIGURATION_COMPONENT
     * Save/Load configuration settings component of the mod.
     * 
     * @field SCENES_MANAGEMENT_COMPONENT
     * UI scenes management component of the mod.
     * 
     * @field DEBUG_COMPONENT
     * Core debug routines component of the mod.
     */
    public enum DebugComponent
    {
        INIT_COMPONENT = 0,
        UI_COMPONENT,
        NETWORK_STACK_COMPONENT,
        LOCALIZATION_COMPONENT,
        CONFIGURATION_COMPONENT,
        ASSETS_MANAGEMENT_COMPONENT,
        DEBUG_COMPONENT
    }

    public class PrintLogger
    {
        /*
         * @brief
         * Private method helper of which it retrieves the name of the
         * component as a string.
         * 
         * @param[in] Component
         * An enumeration to a specific component of the mod.
         * 
         * @return
         * Returns a string of the component name, otherwise NULL is
         * returned if the name of the component is unknown.
         */
        private static string GetComponentName(DebugComponent Component)
        {
            string Name;

            /* Return the appropriate name of the COTLMP component */
            switch (Component)
            {
                case DebugComponent.INIT_COMPONENT:
                {
                    Name = "INIT_COMPONENT";
                    break;
                }

                case DebugComponent.UI_COMPONENT:
                {
                    Name = "UI_COMPONENT";
                    break;
                }

                case DebugComponent.NETWORK_STACK_COMPONENT:
                {
                    Name = "NETWORK_STACK_COMPONENT";
                    break;
                }

                case DebugComponent.LOCALIZATION_COMPONENT:
                {
                    Name = "LOCALIZATION_COMPONENT";
                    break;
                }

                case DebugComponent.CONFIGURATION_COMPONENT:
                {
                    Name = "CONFIGURATION_COMPONENT";
                    break;
                }

                case DebugComponent.ASSETS_MANAGEMENT_COMPONENT:
                {
                    Name = "ASSETS_MANAGEMENT_COMPONENT";
                    break;
                }

                case DebugComponent.DEBUG_COMPONENT:
                {
                    Name = "DEBUG_COMPONENT";
                    break;
                }

                default:
                {
                    Name = null;
                    break;
                }
            }

            return Name;
        }

        /*
         * @brief
         * Prints debug information to the logger.
         * 
         * @param[in] Level
         * An enumeration to a specific debug level, to indicate the meaning
         * of what's being logged out.
         * 
         * @param[in] Component
         * An enumeration to a specific component, to indicate from which place
         * of the mod is the information being logged out.
         * 
         * @param[in] Text
         * A string to a debug text to be logged out to the logger.
         */
        public static void Print(DebugLevel Level, DebugComponent Component, string Text)
        {
            string ComponentName;
            string DebugText;

            /* Bail out if the caller didn't provide anything to output to the logger */
            if (Text == null)
            {
                return;
            }

            /* Obtain a string name of the component */
            ComponentName = GetComponentName(Component);
            if (ComponentName == null)
            {
                /*
                 * We don't know what kind of component did the caller give so
                 * default it to "Unknown".
                 */
                ComponentName = "Unknown";
            }

            /* Format the debug output properly with the component name and debug string */
            DebugText = string.Format("[{0}]: {1}", ComponentName, Text);

            /* Use the appropriate log function baesd on debug level */
            switch (Level)
            {
                case DebugLevel.INFO_LEVEL:
                {
                    Plugin.Logger.LogInfo(DebugText);
                    break;
                }

                case DebugLevel.WARNING_LEVEL:
                {
                    Plugin.Logger.LogWarning(DebugText);
                    break;
                }

                case DebugLevel.ERROR_LEVEL:
                {
                    Plugin.Logger.LogError(DebugText);
                    break;
                }

                case DebugLevel.FATAL_LEVEL:
                {
                    Plugin.Logger.LogFatal(DebugText);
                    break;
                }

                case DebugLevel.MESSAGE_LEVEL:
                {
                    Plugin.Logger.LogMessage(DebugText);
                    break;
                }

                default:
                {
                    /*
                     * I don't know any other debug levels. Default the logger
                     * to the system one.
                     */
                     System.Diagnostics.Debug.Print(DebugText);
                     break;
                }
            }
        }

        /*
         * @brief
         * Prints debug information to the logger. Works identically
         * the same to the Print method except that this method is
         * reserved for debug output that might be too spammy in
         * the debug console. If the VerboseDebug flag is set to FALSE
         * this method won't output anything to the console.
         */
        public static void PrintVerbose(DebugLevel Level, DebugComponent Component, string Text)
        {
            /* Don't display anything if verbose debugging is disabled */
            if (!Plugin.GlobalsInternal.VerboseDebug)
            {
                return;
            }

            /* Output the spammy debug log */
            Print(Level, Component, Text);
        }
    }
}

/* EOF */
