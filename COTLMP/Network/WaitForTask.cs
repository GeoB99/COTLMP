/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define WaitForTask classs
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using UnityEngine;

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * The namespace for all network-related classes, enums and structs
 */
namespace COTLMP.Network
{
    /**
     * @brief
     * The class to use to wait for a Task in a Unity coroutine
     * 
     * @field what
     * The task to wait for
     */
    internal class WaitForTask : CustomYieldInstruction
    {
        public System.Threading.Tasks.Task what;

        /**
         * @brief
         * WaitForTask constructor
         * 
         * @param[in] task
         * The task to wait for
         */
        public WaitForTask(System.Threading.Tasks.Task task) => what = task;

        public override bool keepWaiting => what != null && !what.IsCompleted;
    }
}

/* EOF */
