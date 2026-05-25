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
        /// <summary>
        /// Checks a condition of which the predicate of the said condition returns TRUE, otherwise the system will stop execution of the mod.
        /// </summary>
        /// <param name = "ConditionToCheck">The condition to check.</param>
        /// <param name = "ShowGui">If set to TRUE, the assertion mechanism will display a GUI box with detailed information of the assertion failure.
        /// If set to FALSE, it won't display any GUI box.</param>
        /// <param name = "Description">A string to a detailed description which describes further the failure of the assertion.</param>
        /// <remarks>If the caller wants to display an informative GUI box (ShowGui == TRUE), both Message and Description parameters must be provided.</remarks>
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
