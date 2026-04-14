/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Extend StateMachine class
 * COPYRIGHT:	Copyright 2026 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMPServer;
using COTLMPServer.Messages;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains the classes and code for the mod game related stuff.
 */
namespace COTLMP.Game
{
    /*
     * @brief
     * Extensions for the StateMachine class
     */
    internal static class StateMachineExtensions
    {
        /*
         * @brief
         * Convert a StateMachine instance to PlayerState
         * 
         * @param[in] position
         * The player's position
         * 
         * @returns
         * A PlayerState instance that represents the same thing as the StateMachine instance
         */
        public static PlayerState ToNetwork(this StateMachine machine, Vector3 position)
        {
            return new PlayerState((PlayerState.State)machine.CURRENT_STATE, machine.facingAngle, machine.LookAngle, machine.isDefending, machine.Timer, position);
        }
    }
}

/* EOF */
