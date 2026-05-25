/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Player teams support
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Game
{
    public class Teams
    {
        /// <summary>
        /// Teams enumeration. Each player is assigned to a team with their own specific team player skin.
        /// </summary>
        public enum Team
        {
            /// <summary>
            /// The lamb team. This is the default supported by the game.
            /// </summary>
            Lamb = 0,

            /// <summary>
            /// The goat team. This is the default supported by the game.
            /// </summary>
            Goat,

            /// <summary>
            /// The owl team. This is currently not supported yet in the mod.
            /// </summary>
            Owl,

            /// <summary>
            /// The snake team. This is currently not supported yet in the mod.
            /// </summary>
            Snake
        }
    }
}

/* EOF */
