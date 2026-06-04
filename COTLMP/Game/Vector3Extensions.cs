/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Extend Vector3 class
 * COPYRIGHT:	Copyright 2026 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using UnityEngine;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Game
{
    /// <summary>
    /// Extensions for the Vector3 class
    /// </summary>
    internal static class Vector3Extensions
    {
        /// <summary>
        /// Convert a Unity Vector3 instance to a network Vector3
        /// </summary>
        /// <returns>A network Vector3 instance that represents the same point as the Unity one</returns>
        public static COTLMPServer.Vector3 ToNetwork(this Vector3 vec)
        {
            return new COTLMPServer.Vector3(vec.x, vec.y, vec.z);
        }

        /// <summary>
        /// Convert a network Vector 3 to a Unity Vector3
        /// </summary>
        /// <param name="vec"></param>
        /// <returns>A Unity Vector3 instance that represents the same point as the network one</returns>
        public static Vector3 ToUnity(this COTLMPServer.Vector3 vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }
    }
}

/* EOF */
