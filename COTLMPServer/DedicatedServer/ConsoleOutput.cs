/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Console output logger methods
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using System;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer.DedicatedServer
{
    internal static class ConsoleOutput
    {
        /// <summary>
        /// Displays a fatal message to the console.
        /// </summary>
        /// <param name = "Message">The message string to be passed to this method.</param>
        public static void Fatal(string Message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Fatal: " +  Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Displays a warning message to the console.
        /// </summary>
        /// <param name = "Message">The message string to be passed to this method.</param>
        public static void Warning(string Message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: " + Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Displays an informational message to the console.
        /// </summary>
        /// <param name = "Message">The message string to be passed to this method.</param>
        public static void Info(string Message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Info: " + Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Displays a normal message to the console.
        /// </summary>
        /// <param name = "Message">The message string to be passed to this method.</param>
        public static void Log(string Message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Message);
            Console.ResetColor();
        }
    }
}

/* EOF */
