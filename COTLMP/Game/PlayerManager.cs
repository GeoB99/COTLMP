/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define PlayerManager class
 * COPYRIGHT:	Copyright 2026 necoarcc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using COTLMP.Data;
using COTLMP.Debug;
using HarmonyLib;
using MMTools;
using System;
using System.Collections;
using UnityEngine;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Game
{
    /// <summary>
    /// Class to manage local players created for network players
    /// </summary>
    internal static class PlayerManager
    {
        /// <summary>
        /// The struct representing a command to move a player
        /// </summary>
        private readonly struct MoveInfo(Vector3 point, float timeout)
        {
            public readonly Vector3 Point = point;
            public readonly float Timeout = timeout;
        }

        private readonly static PlayerFarming[] players;
        private readonly static MoveInfo?[] moveCommands;

        /// <summary>
        /// Static constructor. Initializes the arrays and starts the move player coroutine.
        /// </summary>
        static PlayerManager()
        {
            players = new PlayerFarming[InternalData.MaxPlayersPerServerInternal];
            moveCommands = new MoveInfo?[InternalData.MaxPlayersPerServerInternal];
            Plugin.MonoInstance.StartCoroutine(MovePlayers());
        }

        /// <summary>
        /// Moves managed players, never supposed to quit
        /// </summary>
        private static IEnumerator MovePlayers()
        {
            while (true)
            {
                for (uint i = 0; i < moveCommands.Length; ++i)
                {
                    if (moveCommands[i].HasValue)
                    {
                        players[i]?.GoToAndStop(moveCommands[i].Value.Point, maxDuration: moveCommands[i].Value.Timeout, forcePositionOnTimeout: true, groupAbortCurrentGoto: false);
                        moveCommands[i] = null;
                    }
                }
                yield return null;
            }
        }

        /// <summary>
        /// Move a managed player with a given ID
        /// </summary>
        /// <param name="plr">The ID of the player you want to move</param>
        /// <param name="point">The point you want to move the player to</param>
        /// <param name="timeout">The timeout of the move</param>
        public static void MovePlayer(uint plr, Vector3 point, float timeout)
        {
            if (plr > players.Length - 1 || players[plr] == null)
                return;
            moveCommands[plr] = new MoveInfo(point, timeout);
        }

        /// <summary>
        /// Move a managed player with a given ID instantly to the point provided
        /// </summary>
        /// <param name="plr">The ID of the player you want to move</param>
        /// <param name="point">The point you want to move the player to</param>
        public static void MovePlayerNow(uint plr, Vector3 point)
        {
            if (plr > players.Length - 1 || players[plr] == null)
                return;

            players[plr].gameObject.transform.position = point;
        }

        /// <summary>
        /// Delete a managed player
        /// </summary>
        /// <param name="plr">The ID of the managed player you want to delete</param>
        public static void DeletePlayer(uint plr)
        {
            if (plr > players.Length - 1 || players[plr] == null)
                return;

            players[plr].AbortGoTo();
            CoopManager.RemoveCoopPlayerStatic(players[plr], withDelay: false);
            // if this turns out to work bad, can try CoopManager.RemoveCoopPlayerStatic
            // GameObject.Destroy(players[plr].gameObject);
            players[plr] = null;
        }

        /// <summary>
        /// Create a managed player under a given ID
        /// </summary>
        /// <param name="id">The ID you want the managed player to have</param>
        /// <param name="pos">The position of the new managed player to be at</param>
        /// <param name="skin">The fleece ID to put on the new player</param>
        /// <remarks>
        /// If the ID is already taken, the existing player is deleted and recreated.
        /// </remarks>
        public static void CreatePlayer(uint id, Vector3 pos = new(), int skin = 0)
        {
            if (id > players.Length - 1)
                return;

            if (players[id] != null)
            {
                DeletePlayer(id);
            }

            GameObject plr = GameObject.Instantiate(CoopManager.Instance?.playerPrefab);
            if (plr == null)
                return;

            plr.transform.position = pos;

            var farming = plr.GetComponent<PlayerFarming>();
            if (farming == null)
            {
                GameObject.Destroy(plr);
                return;
            }

            try
            {
                farming.isLamb = true;
                farming.EnableCoopFeatures = false;
                farming.playerID = 1; // afaik the player id here doesn't matter
                farming.Init();
                farming.rewiredPlayer = null;
                farming.transform.parent = PlayerFarming.players[0]?.transform.parent;
                plr.SetActive(true);
                farming.Spine.GetComponent<MeshRenderer>()?.enabled = true;
            }
            catch (Exception e)
            {
                GameObject.Destroy(plr);
                PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT, $"Failed to create additional player: {e.Message}");
                return;
            }

            players[id] = farming;
            var playerskin = farming.PlayerSkin = new Spine.Skin("Player Skin");
            playerskin.AddSkin(farming.Spine.Skeleton.Data.FindSkin($"Lamb_{skin}"));
            farming.Spine.Skeleton.SetSkin(playerskin);
            farming.Spine.Skeleton.SetToSetupPose();
        }

        /// <summary>
        /// Set the visual fleece of a managed player
        /// </summary>
        /// <param name="plr">The ID of the player you want to set the fleece of</param>
        /// <param name="skin">The fleece ID to set the player to</param>
        public static void SetPlayerSkin(uint plr, int skin = 0)
        {
            if (plr > players.Length - 1 || players[plr] == null)
                return;

            PlayerFarming farming = players[plr];
            var plrskin = farming.PlayerSkin = new Spine.Skin("Player Skin");
            plrskin.AddSkin(farming.Spine.Skeleton.Data.FindSkin($"Lamb_{skin}"));
            farming.Spine.skeleton.SetSkin(plrskin);
            farming.Spine.skeleton.SetToSetupPose();
        }

        /// <summary>
        /// Check if a managed player exists
        /// </summary>
        /// <param name="plr">The managed player ID</param>
        /// <returns>Whether a managed player exists or not</returns>
        public static bool DoesPlayerExist(uint plr)
        {
            if (plr > players.Length - 1)
                return false;
            return players[plr] != null;
        }

        /// <summary>
        /// Set the state of a given managed player
        /// </summary>
        /// <param name="plr">The ID of the player you want to set the state of</param>
        /// <param name="state">The state you want to set the player to</param>
        /// <param name="isCustomAnimation">Whether if the state you want to set is custom animation</param>
        /// <param name="customAnimation">If you want to set the state to custom animation, the name of the animation</param>
        /// <param name="customAnimationLoop">If you want to set the state to custom animation, whether the animation should loop</param>
        /// <remarks>
        /// If isCustomAnimation is true, the state parameter is ignored
        /// </remarks>
        public static void SetPlayerState(uint plr, StateMachine state, bool isCustomAnimation = false, string customAnimation = null, bool customAnimationLoop = false)
        {
            if (plr > players.Length - 1 || players[plr] == null)
                return;

            PlayerFarming farming = players[plr];
            farming.AbortGoTo();
            if (isCustomAnimation)
            {
                farming.CustomAnimation(customAnimation, customAnimationLoop);
            }
            else
            {
                farming._state = state;
            }
        }

        /// <summary>
        /// Get the state of a managed player
        /// </summary>
        /// <param name="plr">The ID of the player you want to get the state of</param>
        /// <returns>The StateMachine of the player if the ID is valid. Otherwise, null</returns>
        public static StateMachine GetPlayerState(uint plr)
        {
            if (plr > players.Length - 1 || players[plr] == null)
                return null;
            return players[plr].state;
        }

        /// <summary>
        /// This class contains the patches that the PlayerManager class needs to function
        /// </summary>
        [HarmonyPatch]
        private static class PlayerManagerPatches
        {
            /// <summary>
            /// Prevent the managed player from being controlled by the actual player
            /// </summary>
            /// <param name="__instance">The PlayerFarming instance</param>
            [HarmonyPatch(typeof(PlayerFarming), nameof(PlayerFarming.Update))]
            [HarmonyPostfix]
            private static void Update(PlayerFarming __instance)
            {
                if (players.Contains(__instance))
                    __instance.rewiredPlayer = null;
            }

            /// <summary>
            /// Prevents the game from adding the managed player to camera focus
            /// </summary>
            /// <param name="g">The gameobject the game is trying to add to the camera focus</param>
            /// <returns>true if the game should continue, false if not</returns>
            [HarmonyPatch(typeof(CameraFollowTarget), nameof(CameraFollowTarget.AddTarget))]
            [HarmonyPrefix]
            private static bool AddTarget(GameObject g)
            {
                PlayerFarming farming = g?.gameObject?.GetComponentInParent<PlayerFarming>(true);
                return farming == null || (farming == PlayerFarming.Instance && !players.Contains(farming)); // for some reason the players check doesnt work here so here's what i made
            }

            /// <summary>
            /// Prevent managed players from triggering a transition
            /// </summary>
            /// <param name="collision">The collider that collided with the transition zone</param>
            /// <returns>false if the collider is one of the managed players, true otherwise</returns>
            [HarmonyPatch(typeof(EnterBuilding), "OnTriggerEnter2D")]
            [HarmonyPrefix]
            private static bool OnTriggerEnter2D(Collider2D collision)
            {
                var farming = collision.GetComponent<PlayerFarming>();
                return !players.Contains(farming);
            }

            /// <summary>
            /// Destroy all managed players on transition
            /// </summary>
            [HarmonyPatch(typeof(MMTransition), nameof(MMTransition.Play))]
            [HarmonyPostfix]
            private static void MMTransitionPlay()
            {
                for (uint i = 0; i < players.Length; ++i)
                {
                    DeletePlayer(i);
                }
            }
        }
    }
}

/* EOF */
