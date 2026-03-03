/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Capture host world state and apply received state on clients
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using COTLMP.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Network
{
    /**
     * @brief
     * Static helpers that bridge between the in-game world objects
     * (followers, structures, enemies, items, resources) and the
     * serializable WorldStateSnapshot used for network sync.
     *
     * All methods are designed to run on the Unity main thread.
     */
    internal static class WorldStateSyncer
    {
        /* ------------------------------------------------------------------ */
        /* Host-side: capture                                                   */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Reads the current game world into a WorldStateSnapshot.
         * Called periodically on the host.
         */
        public static WorldStateSnapshot CaptureSnapshot()
        {
            var snapshot = new WorldStateSnapshot
            {
                SceneName = SceneManager.GetActiveScene().name
            };

            try { snapshot.TimeOfDay = TimeManager.CurrentGameTime; }  catch { }
            try { snapshot.CurrentDay = TimeManager.CurrentDay; }      catch { }

            CaptureFollowers(snapshot);
            CaptureStructures(snapshot);
            CaptureEnemies(snapshot);
            CaptureDroppedItems(snapshot);
            CaptureResources(snapshot);

            return snapshot;
        }

        /* ---- followers ---- */

        private static void CaptureFollowers(WorldStateSnapshot snapshot)
        {
            try
            {
                var list = new List<WorldStateSnapshot.FollowerEntry>();

                if (FollowerBrain.AllBrains != null)
                {
                    foreach (var brain in FollowerBrain.AllBrains)
                    {
                        try
                        {
                            if (brain == null) continue;

                            var entry = new WorldStateSnapshot.FollowerEntry
                            {
                                ID   = -1,
                                Name = ""
                            };

                            /* Stable identity from FollowerBrainInfo */
                            try
                            {
                                entry.ID   = brain.Info.ID;
                                entry.Name = brain.Info.Name ?? "";
                                entry.Role = (int)brain.Info.FollowerRole;
                            }
                            catch { }

                            if (entry.ID < 0) continue;

                            /* Position: try to find the scene Follower by ID */
                            try
                            {
                                Follower sceneFollower = FollowerManager.FindFollowerByID(entry.ID);
                                if (sceneFollower != null)
                                {
                                    entry.X = sceneFollower.transform.position.x;
                                    entry.Y = sceneFollower.transform.position.y;
                                }
                            }
                            catch { }

                            /* Current task */
                            try { entry.TaskType = (int)brain.CurrentTaskType; }
                            catch { }

                            list.Add(entry);
                        }
                        catch { continue; }
                    }
                }

                snapshot.Followers = list.ToArray();
            }
            catch
            {
                snapshot.Followers = Array.Empty<WorldStateSnapshot.FollowerEntry>();
            }
        }

        /* ---- structures ---- */

        private static void CaptureStructures(WorldStateSnapshot snapshot)
        {
            try
            {
                var list = new List<WorldStateSnapshot.StructureEntry>();

                /* Capture structures at the current player location */
                FollowerLocation location = FollowerLocation.Base;
                try { location = PlayerFarming.Location; } catch { }

                List<StructureBrain> brains = null;
                try { brains = StructureManager.StructuresAtLocation(location); } catch { }

                if (brains != null)
                {
                    foreach (var sb in brains)
                    {
                        try
                        {
                            if (sb == null || sb.Data == null) continue;
                            list.Add(new WorldStateSnapshot.StructureEntry
                            {
                                ID          = sb.Data.ID,
                                Type        = (int)sb.Data.Type,
                                X           = sb.Data.Position.x,
                                Y           = sb.Data.Position.y,
                                IsCollapsed = sb.Data.IsCollapsed,
                                IsAflame    = sb.Data.IsAflame
                            });
                        }
                        catch { continue; }
                    }
                }

                snapshot.Structures = list.ToArray();
            }
            catch
            {
                snapshot.Structures = Array.Empty<WorldStateSnapshot.StructureEntry>();
            }
        }

        /* ---- enemies ---- */

        private static void CaptureEnemies(WorldStateSnapshot snapshot)
        {
            try
            {
                var list = new List<WorldStateSnapshot.EnemyEntry>();

                var enemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
                if (enemies != null)
                {
                    foreach (var ai in enemies)
                    {
                        try
                        {
                            if (ai == null) continue;
                            var go = ai.gameObject;

                            var entry = new WorldStateSnapshot.EnemyEntry
                            {
                                InstanceID = go.GetInstanceID(),
                                TypeName   = go.name ?? "",
                                X          = go.transform.position.x,
                                Y          = go.transform.position.y,
                                IsDead     = !go.activeInHierarchy
                            };

                            /* Health component lives on the same object or a parent */
                            try
                            {
                                var hp = go.GetComponent<Health>();
                                if (hp == null) hp = go.GetComponentInParent<Health>();
                                if (hp != null)
                                {
                                    entry.HP      = hp.HP;
                                    entry.TotalHP = hp.totalHP;
                                    entry.IsDead  = entry.IsDead || (hp.HP <= 0f);
                                }
                            }
                            catch { }

                            list.Add(entry);
                        }
                        catch { continue; }
                    }
                }

                snapshot.Enemies = list.ToArray();
            }
            catch
            {
                snapshot.Enemies = Array.Empty<WorldStateSnapshot.EnemyEntry>();
            }
        }

        /* ---- dropped items ---- */

        private static void CaptureDroppedItems(WorldStateSnapshot snapshot)
        {
            try
            {
                var list = new List<WorldStateSnapshot.DroppedItemEntry>();

                /* PickUp.PickUps is a static List<PickUp> maintained by the game */
                if (PickUp.PickUps != null)
                {
                    foreach (var pu in PickUp.PickUps)
                    {
                        try
                        {
                            if (pu == null) continue;
                            list.Add(new WorldStateSnapshot.DroppedItemEntry
                            {
                                InstanceID = pu.gameObject.GetInstanceID(),
                                Type       = (int)pu.type,
                                Quantity   = pu.Quantity,
                                X          = pu.transform.position.x,
                                Y          = pu.transform.position.y
                            });
                        }
                        catch { continue; }
                    }
                }

                snapshot.DroppedItems = list.ToArray();
            }
            catch
            {
                snapshot.DroppedItems = Array.Empty<WorldStateSnapshot.DroppedItemEntry>();
            }
        }

        /* ---- resources (trees, rubble, interactable objects) ---- */

        private static void CaptureResources(WorldStateSnapshot snapshot)
        {
            try
            {
                var list = new List<WorldStateSnapshot.ResourceEntry>();

                /* Scan all Health components in the scene.
                   Resources like trees/rocks typically have Health attached
                   and no EnemyAI or PlayerFarming component. */
                var allHealth = UnityEngine.Object.FindObjectsOfType<Health>();
                if (allHealth != null)
                {
                    foreach (var h in allHealth)
                    {
                        try
                        {
                            if (h == null) continue;
                            var go = h.gameObject;

                            /* Skip players and enemies (tracked elsewhere) */
                            if (go.GetComponent<PlayerFarming>() != null) continue;
                            if (go.GetComponent<EnemyAI>() != null)       continue;

                            /* Only include if it looks like a resource / interaction */
                            if (go.GetComponent<Interaction>() == null)    continue;

                            list.Add(new WorldStateSnapshot.ResourceEntry
                            {
                                InstanceID  = go.GetInstanceID(),
                                X           = go.transform.position.x,
                                Y           = go.transform.position.y,
                                IsDestroyed = h.HP <= 0f || !go.activeInHierarchy
                            });
                        }
                        catch { continue; }
                    }
                }

                snapshot.Resources = list.ToArray();
            }
            catch
            {
                snapshot.Resources = Array.Empty<WorldStateSnapshot.ResourceEntry>();
            }
        }

        /* ------------------------------------------------------------------ */
        /* Client-side: apply                                                   */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Applies a received snapshot to the local game world.
         * Only acts when the client is in the same scene as the host.
         */
        public static void ApplySnapshot(WorldStateSnapshot snapshot)
        {
            if (snapshot == null) return;

            /* Scene guard: only apply if we are in the same scene */
            string local = SceneManager.GetActiveScene().name;
            if (!string.Equals(local, snapshot.SceneName, StringComparison.Ordinal))
                return;

            try { ApplyFollowers(snapshot); }    catch { }
            try { ApplyStructures(snapshot); }   catch { }
            try { ApplyEnemies(snapshot); }      catch { }
            try { ApplyDroppedItems(snapshot); }  catch { }
            try { ApplyResources(snapshot); }    catch { }
        }

        /* ---- followers ---- */

        private static void ApplyFollowers(WorldStateSnapshot snapshot)
        {
            if (snapshot.Followers == null || snapshot.Followers.Length == 0) return;

            bool isClient = InternalData.IsMultiplayerSession && !InternalData.IsHost;

            foreach (var entry in snapshot.Followers)
            {
                try
                {
                    /* On the client, ensure the follower exists in the local
                       save data so the game can spawn it.  This handles
                       followers recruited by the host after the initial
                       save-data transfer. */
                    if (isClient)
                        EnsureFollowerExists(entry);

                    Follower f = FollowerManager.FindFollowerByID(entry.ID);
                    if (f == null) continue;

                    /* Lerp position towards host position */
                    Vector3 target = new Vector3(entry.X, entry.Y, f.transform.position.z);
                    f.transform.position = Vector3.Lerp(f.transform.position, target, 0.5f);

                    /* On client, disable interaction components so followers
                       only bother the host player. */
                    if (isClient)
                        SuppressFollowerInteraction(f);
                }
                catch { continue; }
            }
        }

        /**
         * @brief
         * Ensures a follower from the host's snapshot exists in the local
         * save data and brain list.  If the follower is missing (e.g. the
         * host recruited it after the client joined), a minimal
         * FollowerInfo is injected into DataManager so the game can
         * spawn the follower at the current location.
         */
        private static void EnsureFollowerExists(WorldStateSnapshot.FollowerEntry entry)
        {
            try
            {
                if (entry.ID < 0) return;
                if (DataManager.Instance?.Followers == null) return;

                /* Check save data */
                bool inSave = false;
                foreach (var info in DataManager.Instance.Followers)
                {
                    if (info != null && info.ID == entry.ID)
                    {
                        inSave = true;
                        break;
                    }
                }

                if (!inSave)
                {
                    var newInfo          = new FollowerInfo();
                    newInfo.ID           = entry.ID;
                    newInfo.Name         = !string.IsNullOrEmpty(entry.Name) ? entry.Name : "Follower";
                    newInfo.FollowerRole = (FollowerRole)entry.Role;
                    DataManager.Instance.Followers.Add(newInfo);
                }

                /* Ensure a brain exists for this follower */
                bool hasBrain = false;
                if (FollowerBrain.AllBrains != null)
                {
                    foreach (var brain in FollowerBrain.AllBrains)
                    {
                        if (brain?.Info != null && brain.Info.ID == entry.ID)
                        {
                            hasBrain = true;
                            break;
                        }
                    }
                }

                if (!hasBrain)
                {
                    FollowerInfo saveInfo = null;
                    foreach (var info in DataManager.Instance.Followers)
                    {
                        if (info != null && info.ID == entry.ID)
                        {
                            saveInfo = info;
                            break;
                        }
                    }
                    if (saveInfo != null)
                        new FollowerBrain(saveInfo);
                }
            }
            catch { /* non-critical: follower will be missing until next sync */ }
        }

        /**
         * @brief
         * Disables Interaction components and trigger colliders on a
         * follower so it cannot initiate or receive interactions on the
         * client.  Called every heartbeat cycle; checks are cheap because
         * most components will already be disabled after the first call.
         */
        private static void SuppressFollowerInteraction(Follower f)
        {
            try
            {
                foreach (var interaction in f.GetComponents<Interaction>())
                {
                    if (interaction != null && interaction.enabled)
                        interaction.enabled = false;
                }

                foreach (var col in f.GetComponentsInChildren<Collider2D>())
                {
                    if (col != null && col.isTrigger && col.enabled)
                        col.enabled = false;
                }
            }
            catch { }
        }

        /* ---- structures ---- */

        private static void ApplyStructures(WorldStateSnapshot snapshot)
        {
            if (snapshot.Structures == null || snapshot.Structures.Length == 0) return;

            FollowerLocation loc = FollowerLocation.Base;
            try { loc = PlayerFarming.Location; } catch { }

            List<StructureBrain> local = null;
            try { local = StructureManager.StructuresAtLocation(loc); } catch { }
            if (local == null) return;

            /* Build a lookup by ID for the host structures */
            var hostMap = new Dictionary<int, WorldStateSnapshot.StructureEntry>();
            foreach (var se in snapshot.Structures)
                hostMap[se.ID] = se;

            foreach (var sb in local)
            {
                try
                {
                    if (sb == null || sb.Data == null) continue;
                    if (!hostMap.TryGetValue(sb.Data.ID, out var hostEntry)) continue;

                    /* Sync collapsed / aflame state */
                    if (hostEntry.IsCollapsed && !sb.Data.IsCollapsed)
                    {
                        try { sb.Collapse(true, true, false); } catch { }
                    }
                    if (hostEntry.IsAflame && !sb.Data.IsAflame)
                    {
                        try { sb.SetAflame(true); } catch { }
                    }
                }
                catch { continue; }
            }
        }

        /* ---- enemies ---- */

        /**
         * @brief
         * Matches client-side enemies to host-side data using a proximity
         * heuristic (same type name within a small radius). Updates HP and
         * kills enemies the host has killed.
         */
        private static void ApplyEnemies(WorldStateSnapshot snapshot)
        {
            if (snapshot.Enemies == null || snapshot.Enemies.Length == 0) return;

            var localEnemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
            if (localEnemies == null) return;

            foreach (var localAI in localEnemies)
            {
                try
                {
                    if (localAI == null) continue;
                    var go = localAI.gameObject;
                    string localName = go.name ?? "";

                    /* Find closest matching host entry by name + proximity */
                    float bestDist = float.MaxValue;
                    WorldStateSnapshot.EnemyEntry best = default;
                    bool found = false;

                    foreach (var he in snapshot.Enemies)
                    {
                        /* Match by object name prefix (strip "(Clone)" etc.) */
                        if (!localName.StartsWith(he.TypeName.Split('(')[0].TrimEnd(),
                            StringComparison.OrdinalIgnoreCase))
                            continue;

                        float dx = go.transform.position.x - he.X;
                        float dy = go.transform.position.y - he.Y;
                        float dist = dx * dx + dy * dy;
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            best = he;
                            found = true;
                        }
                    }

                    if (!found) continue;

                    /* Apply health */
                    var hp = go.GetComponent<Health>();
                    if (hp == null) hp = go.GetComponentInParent<Health>();
                    if (hp != null && best.HP < hp.HP)
                    {
                        hp.HP = best.HP;
                    }

                    /* Kill if host says dead */
                    if (best.IsDead && go.activeInHierarchy)
                    {
                        if (hp != null && hp.HP > 0f)
                            hp.HP = 0f;
                    }
                }
                catch { continue; }
            }
        }

        /* ---- dropped items ---- */

        private static void ApplyDroppedItems(WorldStateSnapshot snapshot)
        {
            /* For now we track destruction: if the host has fewer items,
               some were picked up.  We don't spawn new items because that
               requires the game's object pool system. */
            if (snapshot.DroppedItems == null) return;

            /* Build a set of host-side instance positions for quick lookup */
            var hostPositions = new HashSet<long>();
            foreach (var di in snapshot.DroppedItems)
            {
                /* Quantise to a rough grid so floating-point drift is tolerated */
                long key = ((long)(int)(di.X * 10f) << 32) | (uint)(int)(di.Y * 10f);
                hostPositions.Add(key);
            }

            /* If PickUp.PickUps is available, check for orphan items */
            try
            {
                if (PickUp.PickUps == null) return;
                for (int i = PickUp.PickUps.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        var pu = PickUp.PickUps[i];
                        if (pu == null) continue;
                        long key = ((long)(int)(pu.transform.position.x * 10f) << 32)
                                 | (uint)(int)(pu.transform.position.y * 10f);
                        if (!hostPositions.Contains(key))
                        {
                            /* Item no longer exists on host – destroy locally */
                            pu.gameObject.SetActive(false);
                        }
                    }
                    catch { continue; }
                }
            }
            catch { }
        }

        /* ---- resources ---- */

        private static void ApplyResources(WorldStateSnapshot snapshot)
        {
            if (snapshot.Resources == null || snapshot.Resources.Length == 0) return;

            /* Build a set of destroyed host resource positions */
            var destroyedPositions = new HashSet<long>();
            foreach (var res in snapshot.Resources)
            {
                if (!res.IsDestroyed) continue;
                long key = ((long)(int)(res.X * 10f) << 32) | (uint)(int)(res.Y * 10f);
                destroyedPositions.Add(key);
            }

            if (destroyedPositions.Count == 0) return;

            /* Scan local Health + Interaction objects and destroy matches */
            var allHealth = UnityEngine.Object.FindObjectsOfType<Health>();
            if (allHealth == null) return;

            foreach (var h in allHealth)
            {
                try
                {
                    if (h == null) continue;
                    var go = h.gameObject;
                    if (go.GetComponent<PlayerFarming>() != null) continue;
                    if (go.GetComponent<EnemyAI>() != null)       continue;
                    if (go.GetComponent<Interaction>() == null)    continue;

                    long key = ((long)(int)(go.transform.position.x * 10f) << 32)
                             | (uint)(int)(go.transform.position.y * 10f);
                    if (destroyedPositions.Contains(key) && h.HP > 0f)
                    {
                        h.HP = 0f;
                    }
                }
                catch { continue; }
            }
        }
    }
}

/* EOF */
