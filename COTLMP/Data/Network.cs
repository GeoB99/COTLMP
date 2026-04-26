/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Global header for network related classes and data fields
 * COPYRIGHT:	Copyright 2026 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using UnityEngine;
using UnityEngine.UI;
using System.Net;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains network related data such as fields, classes and
 * whatnot for multiplayer support. "Network" is the main parent
 * class that holds all other subsequent classes together.
 *
 * @class ServerEntry
 * Item entry of a server that describes the details of the server.
 * Used by the servers list GUI of which the listview is populated
 * by server entries.
 */
namespace COTLMP.Data
{
    internal class Network
    {
        internal sealed class ServerEntry
        {
            /* Name of the server to be displayed in the servers list GUI */
            internal string ServerName;

            /* Count of active players and maximum players the server can hold */
            internal int OnlinePlayers;
            internal int MaxPlayers;

            /* The game play mode of the server */
            internal string GameMode;

            /*
             * Indicates whether the server is passwordprotected (which in this case
             * the player is prompted to type the password in order to join).
             */
            internal bool Protected;

            /* Indicates whether the server is set to Favorites */
            internal bool IsFavorite;

            /* Port and IP address of the server */
            internal IPAddress Address;
            internal ushort Port;

            /* The instance of an object that represents the server entry item in the browser */
            internal GameObject InstanceObject;

            public ServerEntry(string Name,
                               int JoinedPlayers,
                               int MaxAllowedPlayers,
                               string Game,
                               bool NeedPw,
                               bool Favorite,
                               IPAddress Addr,
                               ushort PortNum,
                               GameObject Instance)
            {
                ServerName = Name;
                OnlinePlayers = JoinedPlayers;
                MaxPlayers = MaxAllowedPlayers;
                GameMode = Game;
                Protected = NeedPw;
                IsFavorite = Favorite;
                Address = Addr;
                Port = PortNum;
                InstanceObject = Instance;
            }
        }
    }
}

/* EOF */
