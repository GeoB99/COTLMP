/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Game modes support
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using System;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer.Data
{
    public static class GameModes
    {
        /// <summary>
        /// Enumeration class for the supported game mode values.
        /// </summary>
        public enum GameMode
        {
            /// <summary>
            /// The standard COTL gameplay.
            /// </summary>
            Standard = 0,

            /// <summary>
            /// Every team (Lambs, Goats, etc...) get a blunderbuss and kill each other.
            /// This mode is currently not supported yet.
            /// </summary>
            Deathmatch,

            /// <summary>
            /// Teams compete with each other by who defeats a boss faster.
            /// This mode is currently not supported yet.
            /// </summary>
            BossFight,

            /// <summary>
            /// Goat and Lamb are set in an apocalyptic world full of zombie followers.
            /// The teams are equipped with blunderbuss and must defend their cult from zombies.
            /// This mode is currently not supported yet.
            /// </summary>
            Zombies
        }

        /// <summary>
        /// Translates the game mode enum value to a readable string.
        /// </summary>
        /// <param name = "Modes">The game mode enum value to be passed.</param>
        /// <returns>Returns the name string of the game mode.</returns>
        public static string TranslateGameModeToString(GameMode Modes)
        {
            string Mode;

            switch (Modes)
            {
                case GameMode.Standard:
                {
                    Mode = "Standard";
                    break;
                }

                case GameMode.Deathmatch:
                {
                    Mode = "Deathmatch";
                    break;
                }

                case GameMode.BossFight:
                {
                    Mode = "Boss Fight";
                    break;
                }

                case GameMode.Zombies:
                {
                    Mode = "Zombies";
                    break;
                }

                default:
                {
                    Mode = null;
                    break;
                }
            }

            return Mode;
        }
    }
}

/* EOF */
