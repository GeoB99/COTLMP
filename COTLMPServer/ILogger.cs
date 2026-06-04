/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define ILogger interface for the Server class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer
{
    /// <summary>
    /// Interface that classes must implement to be a logger class for the server
    /// </summary>
    public interface ILogger
    {
        void LogInfo(string message);

        void LogWarning(string message);

        void LogError(string message);

        void LogFatal(string message);
    }
}

/* EOF */
