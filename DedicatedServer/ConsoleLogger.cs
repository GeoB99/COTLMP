/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Console logger methods
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using System;

/* CLASSES & CODE *************************************************************/

namespace DedicatedServer
{
    public class ConsoleLogger : COTLMPServer.ILogger
    {
        /// <summary>
        /// Displays a fatal message to the console.
        /// </summary>
        /// <param name = "Message">The message string to be passed to this method.</param>
        public void LogFatal(string Message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Fatal: " +  Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Displays a normal message to the console.
        /// </summary>
        /// <param name = "Message">The message string to be passed to this method.</param>
        public void LogError(string Message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Displays a warning message to the console.
        /// </summary>
        /// <param name = "Message">The message string to be passed to this method.</param>
        public void LogWarning(string Message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: " + Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Displays an informational message to the console.
        /// </summary>
        /// <param name = "Message">The message string to be passed to this method.</param>
        public void LogInfo(string Message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Message);
            Console.ResetColor();
        }
    }
}

/* EOF */
