/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Internal data and flag switches of the mod
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains internal global mod data, reserved for developers only.
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
        internal const int MaxPlayersPerServerInternal = 8;

        internal InternalData()
        {
            /* Do nothing */
        }
    }
}

/* EOF */
