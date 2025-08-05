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
     */
    public class ServerStoppedArgs : EventArgs
    {
        public ServerStopReason Reason;

        /**
         * @brief
         * The constructor
         * 
         * @param[in] reason
         * The value to initialize Reason with
         */
        public ServerStoppedArgs(ServerStopReason reason)
        {
            Reason = reason;
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
