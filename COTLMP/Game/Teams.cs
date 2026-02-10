/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Player teams support
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains the implementation of game teams.
 */
namespace COTLMP.Game
{
    public class Teams
    {
        /*
         * @brief
         * Teams enumeration. Each player is assigned to a team
         * with their own specific team player skin.
         * 
         * @field Lamb
         * The lamb team. This is the default supported by the game. 
         * 
         * @field Goat
         * The goat team. This is the default supported by the game.
         * 
         * @field Owl
         * The owl team. This is currently not supported yet in the mod.
         * 
         * @field Snake
         * The snake team. This is currently not supported yet in the mod.
         */
        public enum Team
        {
            Lamb = 0,
            Goat,
            Owl,
            Snake
        }
    }
}

/* EOF */
