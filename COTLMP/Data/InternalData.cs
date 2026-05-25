/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Internal data and flag switches of the mod
 * COPYRIGHT:	Copyright 2025-2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Data
{
    /// <summary>
    /// Contains internal global mod data, reserved for developers only.
    /// Some of the fields might be changed at runtime.
    /// </summary>
    internal class InternalData
    {
        /******************************************************************************
         *                 THE FOLLOWING FIELDS ARE RESERVED INTERNALLY               *
         *                 FOR THE MOD. CHANGE THESE VALUES WITH CAUTION!!!           *
         ******************************************************************************/

        /// <summary>
        /// Enable or disable verbose debug output in the console.
        /// </summary>
        internal bool VerboseDebug = false;

        /// <summary>
        /// The internal variable of maximum count of players per server. Used for validation purposes.
        /// </summary>
        internal const int MaxPlayersPerServerInternal = 12;

        /// <summary>
        /// Maximum number of characters a player can type in the saychat box.
        /// </summary>
        internal const int MaxSayCharsLimit = 90;

        /******************************************************************************
         *                 THE FOLLOWING FIELDS ARE UPDATED AT RUNTIME.               *
         *                    DO NOT CHANGE THE FOLLOWING FIELDS!!!                   *
         ******************************************************************************/

        /// <summary>
        /// TRUE if the player is currently into a game session, FALSE otherwise.
        /// </summary>
        internal bool InGameSession = false;

        /// <summary>
        /// The player hosts the server through LAN (tipically via Play > Open to LAN option).
        /// </summary>
        internal bool IsServerCreator = false;

        internal InternalData()
        {
            /* Do nothing */
        }
    }
}

/* EOF */
