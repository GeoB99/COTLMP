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

namespace COTLMP.Debug
{
    /// <summary>
    /// Debugging log levels enumeration. Each level represents
    /// a different priority and meaning for the debug information
    /// being output to the logger.
    /// </summary>
    public enum DebugLevel
    {
        /// <summary>
        /// Indicates the debug output is an informational log.
        /// </summary>
        INFO_LEVEL = 0,

        /// <summary>
        /// Indicates the debug output is a warning log.
        /// </summary>
        WARNING_LEVEL,

        /// <summary>
        /// Indicates the debug output is an error log, typically used
        /// by code of which conditions fail or are not met.
        /// </summary>
        ERROR_LEVEL,

        /// <summary>
        /// Indicates the debug output is a fatal error log. Usually this is
        /// used to indicate a certain piece of code has failed to do its job
        /// and the mod will prematurely fail to operate properly.
        /// </summary>
        FATAL_LEVEL,

        /// <summary>
        /// Indicates the debug output is a message log. The difference between
        /// INFO_LEVEL and this one is that a message log is output to the
        /// interest of the user in the console while INFO_LEVEL is a low
        /// level priority log used to mainly display debug or informational stuff.
        /// </summary>
        MESSAGE_LEVEL
    }

    /// <summary>
    /// Components of the mod of which they log out debug information.
    /// </summary>
    public enum DebugComponent
    {
        /// <summary>
        /// Mod startup initialization component.
        /// </summary>
        INIT_COMPONENT = 0,

        /// <summary>
        /// UI module component of the mod.
        /// </summary>
        UI_COMPONENT,

        /// <summary>
        /// Network (Server/Client) multiplayer component of the mod.
        /// </summary>
        NETWORK_STACK_COMPONENT,

        /// <summary>
        /// Localization API component of the mod.
        /// </summary>
        LOCALIZATION_COMPONENT,

        /// <summary>
        /// Save/Load configuration settings component of the mod.
        /// </summary>
        CONFIGURATION_COMPONENT,

        /// <summary>
        /// UI scenes management component of the mod.
        /// </summary>
        ASSETS_MANAGEMENT_COMPONENT,

        /// <summary>
        /// Core debug routines component of the mod.
        /// </summary>
        DEBUG_COMPONENT
    }

    public class PrintLogger
    {
        /// <summary>
        /// Private method helper of which it retrieves the name of the component as a string.
        /// </summary>
        /// <param name = "Component">An enumeration to a specific component of the mod.</param>
        /// <returns>Returns a string of the component name, otherwise NULL is returned if the name of the component is unknown.</returns>
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

        /// <summary>
        /// Prints debug information to the logger.
        /// </summary>
        /// <param name = "Level">An enumeration to a specific debug level, to indicate the meaning of what's being logged out.</param>
        /// <param name = "Component">An enumeration to a specific component, to indicate from which place of the mod is the information being logged out.</param>
        /// <param name = "Text">A string to a debug text to be logged out to the logger.</param>
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

        /// <summary>
        /// Prints debug information to the logger. Works identically the same to the Print method except that this method is
        /// reserved for debug output that might be too spammy in the debug console. If the VerboseDebug flag is set to FALSE
        /// this method won't output anything to the console.
        /// </summary>
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
