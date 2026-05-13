/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define ServerStoppedArgs class for the Server class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using System;

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * Contains the classes/structs/enums for the server
 */
namespace COTLMPServer
{
    /**
     * @brief
     * The event args that get passed to the server stopped event listeners
     * 
     * @field Reason
     * The reason for which the server was stopped
     * 
     * @field What
     * If an error ocurred, the description of the error
     */
    public class ServerStoppedArgs : EventArgs
    {
        public ServerStopReason Reason;
        public string What;

        /**
         * @brief
         * The constructor
         * 
         * @param[in] reason
         * The value to initialize Reason with
         * 
         * @param[in] what
         * The value to initialize What with
         */
        public ServerStoppedArgs(ServerStopReason reason, string what)
        {
            Reason = reason;
            What = what;
        }
    }

    /**
     * @brief
     * Enum of server stop reasons
     */
    public enum ServerStopReason
    {
        Error,
        NormalShutdown
    }
}

/* EOF */
