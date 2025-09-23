/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Servers List UI management code
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;
using COTLMP.Data;
using COTLMP.Debug;
using BepInEx;
using HarmonyLib;
using I2.Loc;
using Lamb.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Ui
{
    public class ServerList : MonoBehaviour
    {
        public void Awake()
        {
            Scene ServerListScene;

            /* Create the Servers List scene */
            ServerListScene = SceneManager.CreateScene("ServerlistUI");
            COTLMP.Debug.Assertions.Assert(ServerListScene != null, false, "ServerlistUI is expected to be created but it's NULL!", null);
        }
    }
}

/* EOF */
