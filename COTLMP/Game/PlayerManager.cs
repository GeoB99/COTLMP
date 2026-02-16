/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define PlayerManager class
 * COPYRIGHT:	Copyright 2026 necoarcc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using HarmonyLib;
using MMTools;
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
     * Class to manage local players created for network players
     * 
     * @field players
     * A fixed-size array of players managed by the class
     */
    internal static class PlayerManager
    {
        private readonly static PlayerFarming[] players;

        /*
         * @brief
         * Static constructor. Initializes the array.
         */
        static PlayerManager()
        {
            players = new PlayerFarming[16];
        }

        /*
         * @brief
         * Move a managed player with a given ID
         * 
         * @param[in] plr
         * The ID of the player you want to move
         * 
         * @param[in] point
         * The point you want to move the player to
         * 
         * @param[in] timeout
         * The timeout of the move
         */
        public static void MovePlayer(uint plr, Vector3 point, float timeout)
        {
            if (plr > players.Length - 1 || players[plr] == null)
                return;
            players[plr].GoToAndStop(point, maxDuration: timeout);
        }

        /*
         * @brief
         * Delete a managed player
         * 
         * @param[in] plr
         * The ID of the managed player you want to delete
         */
        public static void DeletePlayer(uint plr)
        {
            if (plr > players.Length - 1 || players[plr] == null)
                return;
            GameObject.Destroy(players[plr].gameObject);
            players[plr] = null;
        }

        /*
         * @brief
         * Create a managed player under a given ID
         * 
         * @param[in] id
         * The ID you want the managed player to have
         * 
         * @param[in] pos
         * The position of the new managed player to be at
         * 
         * @param[in] skin
         * The fleece ID to put on the new player
         * 
         * @remarks
         * If the ID is already taken, the existing player is given the specified skin and position instead
         */
        public static void CreatePlayer(uint id, Vector3 pos = new(), int skin = 0)
        {
            if (id > players.Length - 1)
                return;
            if (players[id] != null)
            {
                var plrsk = players[id].PlayerSkin = new Spine.Skin("Player Skin");
                plrsk.AddSkin(players[id].Spine.Skeleton.Data.FindSkin($"Lamb_{skin}"));
                players[id].gameObject?.transform.position = pos;
                return;
            }

            GameObject plr = GameObject.Instantiate(CoopManager.Instance.playerPrefab);
            plr.transform.position = pos;

            var farming = plr.GetComponent<PlayerFarming>();
            if (farming == null)
            {
                GameObject.Destroy(plr);
                return;
            }

            players[id] = farming;
            farming.isLamb = true;
            farming.EnableCoopFeatures = false;
            farming.playerID = 1; // afaik the player id here doesn't matter
            farming.Init();
            farming.rewiredPlayer = null;
            farming.transform.parent = PlayerFarming.players[0]?.transform.parent;
            plr.SetActive(true);
            farming.Spine.GetComponent<MeshRenderer>()?.enabled = true;

            var playerskin = farming.PlayerSkin = new Spine.Skin("Player Skin");
            playerskin.AddSkin(farming.Spine.Skeleton.Data.FindSkin($"Lamb_{skin}"));
        }

        /*
         * @brief
         * Set the visual fleece of a managed player
         * 
         * @param[in] plr
         * The ID of the player you want to set the fleece of
         * 
         * @param[in] skin
         * The fleece ID to set the player to
         */
        public static void SetPlayerSkin(uint plr, int skin = 0)
        {
            if (plr > players.Length - 1 || players[plr] == null)
                return;

            PlayerFarming farming = players[plr];
            var plrskin = farming.PlayerSkin = new Spine.Skin("Player Skin");
            plrskin.AddSkin(farming.Spine.Skeleton.Data.FindSkin($"Lamb_{skin}"));
        }

        /*
         * @brief
         * Set the state of a given managed player
         * 
         * @param[in] plr
         * The ID of the player you want to set the state of
         * 
         * @param[in] state
         * The state you want to set the player to
         * 
         * @param[in] isCustomAnimation
         * Whether if the state you want to set is custom animation
         * 
         * @param[in] customAnimation
         * If you want to set the state to custom animation, the name of the animation
         * 
         * @param[in] customAnimationLoop
         * If you want to set the state to custom animation, whether the animation should loop
         * 
         * @remarks
         * If isCustomAnimation is true, the state parameter is ignored
         */
        public static void SetPlayerState(uint plr, StateMachine state, bool isCustomAnimation = false, string customAnimation = null, bool customAnimationLoop = false)
        {
            if (plr > players.Length - 1 || players[plr] == null)
                return;

            PlayerFarming farming = players[plr];
            farming.AbortGoTo();
            if(isCustomAnimation)
            {
                farming.CustomAnimation(customAnimation, customAnimationLoop);
            }
            else
            {
                farming._state = state;
            }
        }

        /*
         * @brief
         * Get the state of a managed player
         * 
         * @param[in] plr
         * The ID of the player you want to get the state of
         * 
         * @returns
         * The StateMachine of the player if the ID is valid. Otherwise, null
         */
        public static StateMachine GetPlayerState(uint plr)
        {
            if (plr > players.Length - 1 || players[plr] == null)
                return null;
            return players[plr].state;
        }

        /*
         * @brief
         * This class contains the patches that the PlayerManager class needs to function
         */
        [HarmonyPatch]
        private static class PlayerManagerPatches
        {
            /*
             * @brief
             * Prevent the managed player from being controlled by the actual player
             * 
             * @param[in] __instance
             * The PlayerFarming instance
             */
            [HarmonyPatch(typeof(PlayerFarming), nameof(PlayerFarming.Update))]
            [HarmonyPostfix]
            private static void Update(PlayerFarming __instance)
            {
                if (players.Contains(__instance))
                    __instance.rewiredPlayer = null;
            }

            /*
             * @brief
             * Prevents the game from adding the managed player to camera focus
             * 
             * @param[in] g
             * The gameobject the game is trying to add to the camera focus
             * 
             * @returns
             * true if the game should continue, false if not
             */
            [HarmonyPatch(typeof(CameraFollowTarget), nameof(CameraFollowTarget.AddTarget))]
            [HarmonyPrefix]
            private static bool AddTarget(GameObject g)
            {
                PlayerFarming farming = g?.gameObject?.GetComponentInParent<PlayerFarming>(true);
                return farming == null || (farming == PlayerFarming.Instance && !players.Contains(farming)); // for some reason the players check doesnt work here so here's what i made
            }

            /*
             * @brief
             * Prevent managed players from triggering a transition
             * 
             * @param[in] collision
             * The collider that collided with the transition zone
             * 
             * @returns
             * false if the collider is one of the managed players, true otherwise
             */
            [HarmonyPatch(typeof(EnterBuilding), "OnTriggerEnter2D")]
            [HarmonyPrefix]
            private static bool OnTriggerEnter2D(Collider2D collision)
            {
                var farming = collision.GetComponent<PlayerFarming>();
                return !players.Contains(farming);
            }

            /*
             * @brief
             * Destroy all managed players on transition
             */
            [HarmonyPatch(typeof(MMTransition), nameof(MMTransition.Play))]
            [HarmonyPostfix]
            private static void MMTransitionPlay()
            {
                for(uint i = 0; i < players.Length; ++i)
                {
                    DeletePlayer(i);
                }
            }
        }
    }
}

/* EOF */
