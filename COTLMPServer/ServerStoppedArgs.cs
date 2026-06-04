/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define ServerStoppedArgs class for the Server class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using System;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer
{
    /// <summary>
    /// The event args that get passed to the server stopped event listeners
    /// </summary>
    public class ServerStoppedArgs : EventArgs
    {
        public ServerStopReason Reason;
        /// <summary>
        /// If an error ocurred, the description of the error
        /// </summary>
        public string What;

        /// <summary>
        /// The constructor
        /// </summary>
        public ServerStoppedArgs(ServerStopReason reason, string what)
        {
            Reason = reason;
            What = what;
        }
    }

    /// <summary>
    /// Enum of server stop reasons
    /// </summary>
    public enum ServerStopReason
    {
        Error,
        NormalShutdown
    }
}

/* EOF */
