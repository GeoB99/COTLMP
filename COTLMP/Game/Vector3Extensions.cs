/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Extend Vector3 class
 * COPYRIGHT:	Copyright 2026 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using UnityEngine;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains the classes and code for the mod game related stuff.
 */
namespace COTLMP.Game
{
    /*
     * @brief
     * Extensions for the Vector3 class
     */
    internal static class Vector3Extensions
    {
        /*
         * @brief
         * Convert a Unity Vector3 instance to a network Vector3
         * 
         * @returns
         * A network Vector3 instance that represents the same thing as the Unity one
         */
        public static COTLMPServer.Vector3 ToNetwork(this Vector3 vec)
        {
            return new COTLMPServer.Vector3(vec.x, vec.y, vec.z);
        }
    }
}

/* EOF */
