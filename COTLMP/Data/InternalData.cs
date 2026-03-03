/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Internal data and flag switches of the mod
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using System;
using System.Reflection;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains internal global mod data, reserved for developers only.
 * 
 * @class InternalData
 * Main class of which mod data is stored.
 */
namespace COTLMP.Data
{
    internal class InternalData
    {
        /******************************************************************************
         *                 THE FOLLOWING FIELDS ARE RESERVED INTERNALLY               *
         *                 FOR THE MOD. CHANGE THESE VALUES WITH CAUTION!!!           *
         ******************************************************************************/

        /* Enable or disable verbose debug output in the console */
        internal bool VerboseDebug = false;

        /* The internal variable of maximum count of players per server. Used for validation purposes. */
        internal const int MaxPlayersPerServerInternal = 12;

        /**
         * True while the local player is either hosting a server or connected as a
         * client.  Used by DLC patches to restrict Woolhaven access during multiplayer.
         */
        internal static bool IsMultiplayerSession = false;

        /**
         * True when the local player is the host of the current session.
         * Only the host captures and sends world-state heartbeats.
         */
        internal static bool IsHost = false;

        internal InternalData()
        {
            /* Do nothing */
        }

        /**
         * @brief
         * Returns the local player's display name.  Tries the Steam
         * persona name first (via reflection to avoid a compile-time
         * dependency on Steamworks.NET) and falls back to the
         * configured PlayerName setting.
         */
        internal static string GetLocalPlayerName()
        {
            try
            {
                var smType = Type.GetType("SteamManager, Assembly-CSharp");
                if (smType != null)
                {
                    var initProp = smType.GetProperty("Initialized",
                        BindingFlags.Public | BindingFlags.Static);
                    if (initProp != null && (bool)initProp.GetValue(null))
                    {
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            var sfType = asm.GetType("Steamworks.SteamFriends");
                            if (sfType == null) continue;
                            var method = sfType.GetMethod("GetPersonaName",
                                BindingFlags.Public | BindingFlags.Static);
                            if (method != null)
                            {
                                string name = method.Invoke(null, null) as string;
                                if (!string.IsNullOrEmpty(name))
                                    return name;
                            }
                            break;
                        }
                    }
                }
            }
            catch { }

            return Plugin.Globals?.PlayerName ?? "Player";
        }
    }
}

/* EOF */
