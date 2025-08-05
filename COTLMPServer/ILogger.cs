/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define ILogger interface for the Server class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * Contains the classes/structs/enums for the server
 */
namespace COTLMPServer
{
    /**
     * @brief
     * Interface that classes must implement to be a logger class for the server
     */
    public interface ILogger
    {
        /**
         * @brief
         * Logs a message with the info log level
         */
        void LogInfo(string message);

        /**
         * @brief
         * Logs a message with the warning log level
         */
        void LogWarning(string message);

        /**
         * @brief
         * Logs a message with the error log level
         */
        void LogError(string message);

        /**
         * @brief
         * Logs a message with the fatal log level
         */
        void LogFatal(string message);
    }
}

/* EOF */
