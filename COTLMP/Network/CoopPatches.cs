/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Harmony patches that prevent the game's coop lifecycle from
 *              removing network-controlled avatars during a multiplayer session
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using COTLMP.Data;
using HarmonyLib;
using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Network
{
    /**
     * @brief
     * During a multiplayer session the game must not remove the coop
     * player slot.  The vanilla code detects "no second controller"
     * and fires removal animations, faith rewards, difficulty resets
     * and HidePlayer in a tight loop every frame when fighting with
     * our Tick() re-add.  These patches short-circuit those paths.
     */
    [HarmonyPatch]
    internal static class CoopPatches
    {
        /* ------------------------------------------------------------------ */
        /* Block the animated remove (leave-animation + effects + faith)        */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Blocks CoopManager.RemoveCoopPlayer during a multiplayer session.
         * This is the method that fires OnPlayerLeft, plays the despawn
         * animation, calls HidePlayer and resets Rewired.
         *
         * @return
         * false to skip the original method during multiplayer, true otherwise.
         */
        [HarmonyPatch(typeof(CoopManager), nameof(CoopManager.RemoveCoopPlayer))]
        [HarmonyPrefix]
        private static bool BlockRemoveCoopPlayer()
        {
            return !InternalData.IsMultiplayerSession;
        }

        /**
         * @brief
         * Blocks CoopManager.RemoveCoopPlayerStatic during multiplayer.
         */
        [HarmonyPatch(typeof(CoopManager), nameof(CoopManager.RemoveCoopPlayerStatic))]
        [HarmonyPrefix]
        private static bool BlockRemoveCoopPlayerStatic()
        {
            return !InternalData.IsMultiplayerSession;
        }

        /* ------------------------------------------------------------------ */
        /* Block the menu-driven removal path (controller disconnect)           */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Blocks CoopManager.RemovePlayerFromMenu which fires when the game
         * detects a controller was disconnected or user signed out.
         */
        [HarmonyPatch(typeof(CoopManager), nameof(CoopManager.RemovePlayerFromMenu))]
        [HarmonyPrefix]
        private static bool BlockRemovePlayerFromMenu()
        {
            return !InternalData.IsMultiplayerSession;
        }

        /**
         * @brief
         * Blocks CoopManager.ClearCoopMode which destroys all coop players.
         */
        [HarmonyPatch(typeof(CoopManager), nameof(CoopManager.ClearCoopMode))]
        [HarmonyPrefix]
        private static bool BlockClearCoopMode()
        {
            return !InternalData.IsMultiplayerSession;
        }

        /* ------------------------------------------------------------------ */
        /* Block the temporary hide that disables the Spine renderer            */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Blocks CoopManager.HideCoopPlayerTemporarily which sets the
         * gameObject inactive and disables the Spine MeshRenderer.
         */
        [HarmonyPatch(typeof(CoopManager), nameof(CoopManager.HideCoopPlayerTemporarily))]
        [HarmonyPrefix]
        private static bool BlockHideCoopPlayerTemporarily()
        {
            return !InternalData.IsMultiplayerSession;
        }

        /* ------------------------------------------------------------------ */
        /* Block PlayerFarming.HidePlayer so the avatar stays in the list       */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Blocks PlayerFarming.HidePlayer which removes the player from
         * the players list and deactivates the gameObject.
         */
        [HarmonyPatch(typeof(PlayerFarming), nameof(PlayerFarming.HidePlayer))]
        [HarmonyPrefix]
        private static bool BlockHidePlayer(PlayerFarming playerFarming)
        {
            if (!InternalData.IsMultiplayerSession) return true;

            /* Allow hiding the local lamb (e.g. during cutscenes) but
               never hide the network-controlled non-lamb coop slot. */
            if (playerFarming != null && !playerFarming.isLamb)
                return false;

            return true;
        }

        /* ------------------------------------------------------------------ */
        /* Block WaitTillPlayersRady coroutine (NullRef on network coop)        */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Replaces CoopManager.WaitTillPlayersRady with an empty coroutine
         * during multiplayer.  The original coroutine expects Rewired input
         * from a physical second controller and will NullRef every frame
         * when the coop slot is driven by the network.
         *
         * We must set __result to a valid IEnumerator; returning false with
         * a null IEnumerator causes StartCoroutine(null) → "routine is null".
         */
        [HarmonyPatch(typeof(CoopManager), "WaitTillPlayersRady")]
        [HarmonyPrefix]
        private static bool BlockWaitTillPlayersRady(ref IEnumerator __result)
        {
            if (!InternalData.IsMultiplayerSession) return true;
            __result = EmptyCoroutine();
            return false;
        }

        private static IEnumerator EmptyCoroutine()
        {
            yield break;
        }

        /* ------------------------------------------------------------------ */
        /* Rewired safety: prevent NullRef on the network-driven coop slot      */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Swallows the NullReferenceException that occurs when
         * RefreshCoopPlayerRewired tries to access the Rewired input
         * player for slot 1 during multiplayer.  The network-driven
         * coop avatar has no physical controller, so
         * RewiredInputManager.GetPlayer(1) returns a player whose
         * .controllers accessor NullRefs.  The local lamb (slot 0) is
         * processed first and set up correctly before the crash, so
         * swallowing the exception is safe.
         */
        [HarmonyPatch(typeof(CoopManager), nameof(CoopManager.RefreshCoopPlayerRewired))]
        [HarmonyFinalizer]
        private static Exception SafeRefreshCoopPlayerRewired(Exception __exception)
        {
            if (InternalData.IsMultiplayerSession && __exception != null)
                return null;
            return __exception;
        }

        /* ------------------------------------------------------------------ */
        /* Camera fix: keep camera on the local lamb after coop spawn           */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * After the coop system spawns a second player, strip any
         * camera-targeting components from the new avatar so the
         * camera stays centred on the local lamb.
         */
        [HarmonyPatch(typeof(CoopManager), nameof(CoopManager.SpawnCoopPlayer))]
        [HarmonyPostfix]
        private static void AfterCoopSpawn()
        {
            if (!InternalData.IsMultiplayerSession) return;

            try
            {
                if (PlayerFarming.players == null) return;
                foreach (var pf in PlayerFarming.players)
                {
                    if (pf == null || pf.isLamb) continue;

                    // Remove any MonoBehaviour whose type name contains
                    // "CameraFollow" so the camera ignores this avatar.
                    foreach (var mb in pf.GetComponents<UnityEngine.MonoBehaviour>())
                    {
                        if (mb != null && mb.GetType().Name.Contains("CameraFollow"))
                            UnityEngine.Object.Destroy(mb);
                    }

                    // Disable trigger colliders (redundant safety-net;
                    // RemotePlayerInfo.LinkAvatar also does this).
                    foreach (var col in pf.GetComponentsInChildren<UnityEngine.Collider2D>())
                    {
                        if (col.isTrigger)
                            col.enabled = false;
                    }
                }
            }
            catch { /* safe to ignore if scene is not ready */ }
        }
    }

    /**
     * @brief
     * Prevents the client from triggering door/room/scene transitions.
     * Only the host may walk through doors; clients follow via the
     * SceneChange network message.
     *
     * Kept in a separate [HarmonyPatch] class so that if any target
     * method is renamed or removed in a game update, the failure is
     * isolated and does not prevent the core CoopPatches from loading.
     */
    [HarmonyPatch]
    internal static class DoorPatches
    {
        /**
         * @brief
         * Helper shared by every door prefix.
         * Returns true (allow original) when NOT in a client session,
         * false (skip original) when the local player is a client.
         */
        private static bool AllowDoor()
        {
            return !(InternalData.IsMultiplayerSession && !InternalData.IsHost);
        }

        /* ---- Door (dungeon room-to-room transitions) ---- */

        [HarmonyPatch(typeof(Door), "OnTriggerEnter2D")]
        [HarmonyPrefix]
        private static bool BlockDoorTrigger()
        {
            return AllowDoor();
        }

        /* ---- Inteaction_DoorRoomDoor (door-room → dungeon run) ---- */

        [HarmonyPatch(typeof(Inteaction_DoorRoomDoor), "OnTriggerEnter2D")]
        [HarmonyPrefix]
        private static bool BlockDoorRoomDoorTrigger()
        {
            return AllowDoor();
        }

        /* ---- Interaction_BaseDungeonDoor (base → dungeon entry) ---- */

        [HarmonyPatch(typeof(Interaction_BaseDungeonDoor), "OnTriggerEnter2D")]
        [HarmonyPrefix]
        private static bool BlockBaseDungeonDoorTrigger()
        {
            return AllowDoor();
        }

        /* ---- Interaction_BaseDoor (base → world map selection) ---- */

        [HarmonyPatch(typeof(Interaction_BaseDoor), "OnTriggerEnter2D")]
        [HarmonyPrefix]
        private static bool BlockBaseDoorTrigger()
        {
            return AllowDoor();
        }

        /* ---- Interaction_BiomeDoor (biome transitions) ---- */

        [HarmonyPatch(typeof(Interaction_BiomeDoor), "OnTriggerEnter2D")]
        [HarmonyPrefix]
        private static bool BlockBiomeDoorTrigger()
        {
            return AllowDoor();
        }

        /* ---- DungeonDoor (dungeon activation trigger) ---- */

        [HarmonyPatch(typeof(DungeonDoor), "OnTriggerEnter2D")]
        [HarmonyPrefix]
        private static bool BlockDungeonDoorTrigger()
        {
            return AllowDoor();
        }
    }

    /**
     * @brief
     * Save-game patches for multiplayer sessions.
     *
     * Client side: blocks all saves so the client never overwrites
     * the host's authoritative data (the client uses a disposable
     * temp slot).
     *
     * Host side: after every save, re-captures the save file,
     * compresses it and broadcasts it to all connected clients so
     * their temp slot stays up-to-date with the latest world state.
     */
    [HarmonyPatch]
    internal static class SavePatches
    {
        /* ------------------------------------------------------------------ */
        /* Client: block saves                                                  */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Blocks SaveAndLoad.Save() on clients.  The client's local
         * DataManager is only a mirror of the host's save; writing it
         * to disk is unnecessary and could desync the temp slot.
         */
        [HarmonyPatch(typeof(SaveAndLoad), "Save", new Type[0])]
        [HarmonyPrefix]
        private static bool BlockClientSave()
        {
            if (InternalData.IsMultiplayerSession && !InternalData.IsHost)
                return false;
            return true;
        }

        /**
         * @brief
         * Blocks the overload SaveAndLoad.Save(string) on clients.
         */
        [HarmonyPatch(typeof(SaveAndLoad), "Save", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        private static bool BlockClientSaveWithFilename()
        {
            if (InternalData.IsMultiplayerSession && !InternalData.IsHost)
                return false;
            return true;
        }

        /* ------------------------------------------------------------------ */
        /* Host: broadcast updated save to clients after every save            */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * After the host finishes saving, read the save file back,
         * compress it and send it to the server for relay to all
         * clients.  This keeps the client's temp slot in sync so
         * scene transitions load the correct world state.
         */
        [HarmonyPatch(typeof(SaveAndLoad), "Save", new Type[0])]
        [HarmonyPostfix]
        private static void HostResyncAfterSave()
        {
            if (!InternalData.IsHost) return;
            if (PlayerSync.ActiveClient == null || !PlayerSync.ActiveClient.IsConnected) return;

            try
            {
                string savesDir = Path.Combine(Application.persistentDataPath, "saves");
                string slotName = SaveAndLoad.MakeSaveSlot(SaveAndLoad.SAVE_SLOT);
                string mpPath   = Path.Combine(savesDir, Path.ChangeExtension(slotName, ".mp"));
                string jsonPath = Path.Combine(savesDir, slotName);
                string savePath = File.Exists(mpPath) ? mpPath : jsonPath;

                if (!File.Exists(savePath)) return;

                byte[] raw        = File.ReadAllBytes(savePath);
                byte[] compressed = Compress(raw);
                PlayerSync.ActiveClient.SendSaveResync(compressed);

                Plugin.Logger?.LogInfo($"[SavePatches] Broadcast save resync ({raw.Length} -> {compressed.Length} bytes)");
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[SavePatches] Save resync failed: {e.Message}");
            }
        }

        private static byte[] Compress(byte[] raw)
        {
            using (var output = new MemoryStream())
            {
                using (var gz = new GZipStream(output, CompressionMode.Compress, true))
                    gz.Write(raw, 0, raw.Length);
                return output.ToArray();
            }
        }
    }
}

/* EOF */
