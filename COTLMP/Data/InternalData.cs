/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Internal data and flag switches of the mod
 * COPYRIGHT:	Copyright 2025-2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains internal global mod data, reserved for developers only.
 * Some of the fields might be changed at runtime.
 *
 * @class InternalData
 * Main class of which mod data is stored.
 */
namespace COTLMP.Data
{
    internal class InternalData
    {
        /******************************************************************************
         *                 THE FOLLOWING FIELDS ARE RESERVED INTERNALLY               *
         *                 FOR THE MOD. CHANGE THESE VALUES WITH CAUTION!!!           *
         ******************************************************************************/

        /* Enable or disable verbose debug output in the console */
        internal bool VerboseDebug = false;

        /* The internal variable of maximum count of players per server. Used for validation purposes. */
        internal const int MaxPlayersPerServerInternal = 12;

        /* Maximum number of characters a player can type in the saychat box */
        internal const int MaxSayCharsLimit = 90;

        /******************************************************************************
         *                 THE FOLLOWING FIELDS ARE UPDATED AT RUNTIME.               *
         *                    DO NOT CHANGE THE FOLLOWING FIELDS!!!                   *
         ******************************************************************************/

        /* TRUE if the player is currently into a game session, FALSE otherwise */
        internal bool InGameSession = false;

        /* The player hosts the server through LAN (tipically via Play > Open to LAN option) */
        internal bool IsServerCreator = false;

        internal InternalData()
        {
            /* Do nothing */
        }
    }
}

/* EOF */
