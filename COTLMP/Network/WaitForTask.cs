/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define WaitForTask classs
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using UnityEngine;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Network
{
    /// <summary>
    /// The class to use to wait for a Task in a Unity coroutine
    /// </summary>
    internal class WaitForTask : CustomYieldInstruction
    {
        /// <summary>
        /// The task to wait for
        /// </summary>
        public System.Threading.Tasks.Task what;

        public WaitForTask(System.Threading.Tasks.Task task) => what = task;

        public override bool keepWaiting => what != null && !what.IsCompleted;
    }
}

/* EOF */
