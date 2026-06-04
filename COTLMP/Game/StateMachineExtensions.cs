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

namespace COTLMP.Game
{
    /// <summary>
    /// Extensions for the StateMachine class
    /// </summary>
    internal static class StateMachineExtensions
    {
        /// <summary>
        /// Convert a StateMachine instance to PlayerState
        /// </summary>
        /// <param name="position">The player's position</param>
        /// <returns>A PlayerState instance that represents the same thing as the StateMachine instance</returns>
        public static PlayerState ToNetwork(this StateMachine machine, Vector3 position)
        {
            return new PlayerState((PlayerState.State)machine.CURRENT_STATE, machine.facingAngle, machine.LookAngle, machine.isDefending, machine.Timer, position);
        }

        /// <summary>
        /// Convert a PlayerState instance to StateMachine
        /// </summary>
        /// <param name="state"></param>
        /// <returns>A StateMachine instance that represents the same thing as the PlayerInstance instance</returns>
        public static StateMachine ToUnity(this PlayerState state)
        {
            return new StateMachine()
            {
                Timer = state.Timer,
                CURRENT_STATE = (StateMachine.State)state.Current,
                facingAngle = state.Facing,
                LookAngle = state.Look,
                isDefending = state.Defending,
            };
        }
    }
}

/* EOF */
