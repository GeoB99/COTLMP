/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Main file containing global resource localization strings
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using I2.Loc;
using System;

/* CLASSES & CODE *************************************************************/

namespace I2.Loc
{
    public static class MultiplayerModLocalization
    {
        public static class UI
        {
            public static string Multiplayer_Title
            {
                get
                {
                    return LocalizationManager.GetTranslation("Multiplayer/UI/TitleDialog");
                }
            }

            public static string Multiplayer_Text
            {
                get
                {
                    return LocalizationManager.GetTranslation("Multiplayer/UI/WIP");
                }
            }
        }
    }
}

/* EOF */
