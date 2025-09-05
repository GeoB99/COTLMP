/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Debug assert support
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Debug;
using System.Diagnostics;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Debug
{
    public class Assertions
    {
        public static void Assert(bool ConditionToCheck, bool ShowGui, string Message, string Description)
        {
            /* Don't display any GUI if the caller hasn't asked for it */
            if (!ShowGui)
            {
                System.Diagnostics.Debug.Assert(ConditionToCheck);
            }
            else
            {
                /*
                 * When the assert kicks in a message and description are expected
                 * to describe information about such failed assert.
                 */
                if (Message == null || Description == null)
                {
                    COTLMP.Debug.PrintLogger.Print(DebugLevel.FATAL_LEVEL, DebugComponent.DEBUG_COMPONENT, "Message and description are expected when displaying GUI!");
                    return;
                }

                /* Display it now */
                System.Diagnostics.Debug.Assert(ConditionToCheck, Message, Description);
            }
        }
    }
}

/* EOF */
