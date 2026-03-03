/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     In-game representation of a networked remote player
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using Spine.Unity;
using UnityEngine;
using TMPro;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Network
{
    /**
     * @brief
     * Tracks the last-known state for one remote player and drives their
     * in-game avatar (a co-op PlayerFarming slot) via interpolation.
     *
     * All public methods are called on the Unity main thread by PlayerSync.
     */
    internal sealed class RemotePlayerInfo
    {
        /* ------------------------------------------------------------------ */
        /* Identity                                                             */
        /* ------------------------------------------------------------------ */

        public readonly int    ID;
        public readonly string Name;

        /**
         * Display name with a [Host] or [Client] role tag.
         * Host is always the first player to connect (ID 1).
         */
        public string DisplayName
        {
            get
            {
                if (Data.InternalData.IsHost)
                    return $"[Client] {Name}";
                return (ID == 1) ? $"[Host] {Name}" : $"[Client] {Name}";
            }
        }

        /* ------------------------------------------------------------------ */
        /* Network state                                                        */
        /* ------------------------------------------------------------------ */

        private Vector3             _targetPosition;
            private float               _targetAngle;
            private StateMachine.State  _targetState   = StateMachine.State.Idle;
            private float               _hp;
            private float               _totalHp;
            private string              _targetAnimName;
            private string              _lastAppliedAnim;

        /* ------------------------------------------------------------------ */
        /* Game-side avatar                                                     */
        /* ------------------------------------------------------------------ */

        /** Co-op PlayerFarming slot driven by network data (null if not spawned) */
        private PlayerFarming _avatar;

        /** Cached Spine MeshRenderer – the game frequently disables this */
        private MeshRenderer _spineMesh;

        /** Simple overhead name-label; always created even when avatar is absent */
        private GameObject    _nameLabel;

        private const float LerpSpeed = 12f;

        // Spawn retry – periodically re-attempt when the avatar is missing
        private float _spawnRetryTimer;
        private const float SpawnRetryInterval = 1f;

        /* ------------------------------------------------------------------ */
        /* Constructor                                                          */
        /* ------------------------------------------------------------------ */

        public RemotePlayerInfo(int id, string name)
        {
            ID   = id;
            Name = name;
        }

        /* ------------------------------------------------------------------ */
        /* Spawn / despawn                                                      */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Attempts to create a game-side representation for this remote player.
         * Uses the CoopManager to spawn a second PlayerFarming (slot 1) if the
         * game scene is active, or falls back to a lightweight name-label only.
         */
        public void TrySpawn()
        {
            // Prefer a proper co-op character when in an active game session
            if (TrySpawnCoopAvatar()) return;

            // Fallback: floating label only
            _nameLabel = BuildNameLabel(DisplayName, Vector3.zero);
        }

        private bool TrySpawnCoopAvatar()
        {
            try
            {
                if (CoopManager.Instance == null) return false;
                if (!SessionHandler.HasSessionStarted)  return false;
                if (PlayerFarming.Instance == null)      return false;

                // If CoopActive is stale (the actual coop player was destroyed
                // by a scene reload) clear the flag so we can spawn a new one.
                if (CoopManager.CoopActive)
                {
                    bool found = false;
                    if (PlayerFarming.players != null)
                    {
                        foreach (var pf in PlayerFarming.players)
                        {
                            if (pf != null && !pf.isLamb)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found) return false; // genuine coop player exists
                    CoopManager.CoopActive = false;
                }

                // Bypass SpawnCoopPlayer entirely.  SpawnCoopPlayer funnels
                // ALL creation logic through WaitTillPlayersRady, which our
                // Harmony patch replaces with an empty coroutine (the callback
                // that creates the avatar is silently dropped).  It also
                // triggers Rewired controller setup (NullRef without a physical
                // second gamepad), tarot card menus, and difficulty resets.
                //
                // Instead, use CreateCoopPlayer for the raw instantiation and
                // do the minimum initialisation ourselves.
                var go = CoopManager.Instance.CreateCoopPlayer(1);

                // Set identity fields BEFORE activation so that Start() →
                // SetSkin() correctly applies the Goat skin.
                var avatar = go.GetComponent<PlayerFarming>();
                avatar.isLamb   = false;
                avatar.playerID = 1;
                avatar.transform.parent = PlayerFarming.Instance.transform.parent;
                go.transform.position   = PlayerFarming.Instance.transform.position
                                        + Vector3.right * 1.5f;

                // Activation triggers Awake() immediately and schedules
                // Start() before the next Update.  Start() initialises the
                // Spine skeleton, health component, HUD indicator, and calls
                // SetSkin() which reads isLamb/playerID for the Goat skin.
                go.SetActive(true);

                // Enable the Spine mesh renderer (CreateCoopPlayer leaves
                // the GO inactive; the renderer starts disabled).
                try
                {
                    var mr = avatar.Spine.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = true;
                }
                catch { }

                // Show hearts HUD if available.
                try { if (avatar.hudHearts != null) avatar.hudHearts.gameObject.SetActive(true); }
                catch { }

                // Silently register in the players list.  We set the count
                // directly and skip RefreshPlayersCount(true) which re-runs
                // Init → AddToCamera → RefreshCoopPlayerRewired on every
                // player — the Rewired call NullRefs for the network slot.
                CoopManager.CoopActive = true;
                if (!PlayerFarming.players.Contains(avatar))
                {
                    PlayerFarming.players.Add(avatar);
                    PlayerFarming.playersCount = PlayerFarming.players.Count;
                }

                // Ensure the health component is available immediately
                // (Start() also sets this, but it may not have run yet).
                try
                {
                    if (avatar.health == null)
                        avatar.health = avatar.gameObject.GetComponent<HealthPlayer>();
                    if (avatar.health != null)
                        avatar.health.InitHP();
                }
                catch { }

                Plugin.Logger?.LogInfo($"[RemotePlayer] Coop avatar created for '{Name}' (ID {ID})");
                return true;
            }
            catch (System.Exception e)
            {
                Plugin.Logger?.LogWarning($"[RemotePlayer] Coop spawn failed: {e.Message}");
                return false;
            }
        }

        /**
         * @brief
         * Finds the spawned coop PlayerFarming once SpawnCoopPlayer has finished.
         * Searches both the active players list and the static
         * CoopManager.AllPlayerGameObjects array (the game can remove a
         * player from the list while the object still exists).
         */
        private void TryLinkAvatar()
        {
            if (_avatar != null) return;

            /* Primary: look in the active players list */
            if (PlayerFarming.players != null)
            {
                for (int i = 0; i < PlayerFarming.players.Count; i++)
                {
                    var pf = PlayerFarming.players[i];
                    if (pf != null && !pf.isLamb)
                    {
                        LinkAvatar(pf);
                        return;
                    }
                }
            }

            /* Fallback: the game keeps slot objects in a static array even
               after HidePlayer removes them from the players list. */
            try
            {
                for (int i = 1; i < CoopManager.AllPlayerGameObjects.Length; i++)
                {
                    var go = CoopManager.AllPlayerGameObjects[i];
                    if (go == null) continue;
                    var pf = go.GetComponent<PlayerFarming>();
                    if (pf != null && !pf.isLamb)
                    {
                        LinkAvatar(pf);
                        return;
                    }
                }
            }
            catch { }
        }

        private void LinkAvatar(PlayerFarming pf)
        {
            _avatar         = pf;
            _targetPosition = pf.transform.position;

            /* Cache the Spine mesh renderer for fast visibility checks */
            try
            {
                if (pf.Spine != null)
                    _spineMesh = pf.Spine.GetComponent<MeshRenderer>();
            }
            catch { _spineMesh = null; }

            if (_nameLabel == null)
                _nameLabel = BuildNameLabel(DisplayName, pf.transform.position + Vector3.up * 1.5f);

            /* ---- loading-zone fix ----
             * Disable trigger colliders on the remote avatar so it
             * cannot activate loading-zone or other scene triggers.
             * The avatar is entirely network-driven so physics
             * interactions are not needed. */
            try
            {
                foreach (var col in pf.GetComponentsInChildren<Collider2D>())
                {
                    if (col.isTrigger)
                        col.enabled = false;
                }
            }
            catch { }

            /* ---- camera fix ----
             * Remove any camera-follow component the coop system
             * attached to the avatar so the camera stays on the
             * local lamb only. */
            try
            {
                foreach (var mb in pf.GetComponents<MonoBehaviour>())
                {
                    if (mb != null && mb.GetType().Name.Contains("CameraFollow"))
                        Object.Destroy(mb);
                }
            }
            catch { }
        }

        /* ------------------------------------------------------------------ */
        /* State setters (called from main thread by PlayerSync)               */
        /* ------------------------------------------------------------------ */

        public void SetTargetPosition(Vector3 pos, float angle)
        {
            _targetPosition = pos;
            _targetAngle    = angle;
        }

        public void SetState(StateMachine.State state)   { _targetState = state; }

        public void SetHealth(float hp, float totalHp)
        {
            _hp      = hp;
            _totalHp = totalHp;
        }

        /** Store the latest animation name received from the network */
        public void SetAnimationName(string animName) { _targetAnimName = animName; }

        /* ------------------------------------------------------------------ */
        /* Per-frame update                                                     */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Called each frame from PlayerSync.Update() to interpolate the avatar.
         */
        public void Tick()
        {
            // Detect if Unity destroyed the underlying object (scene change etc.)
            // Use ReferenceEquals to bypass Unity's overloaded == operator:
            // the C# reference is still non-null but the native object is gone.
            if (!ReferenceEquals(_avatar, null) && _avatar == null)
            {
                _avatar    = null;
                _spineMesh = null;
            }

            // Try to link a freshly spawned coop avatar
            if (_avatar == null)
                TryLinkAvatar();

            // If still no avatar, periodically retry spawning
            if (_avatar == null)
            {
                _spawnRetryTimer += Time.deltaTime;
                if (_spawnRetryTimer >= SpawnRetryInterval)
                {
                    _spawnRetryTimer = 0f;
                    TrySpawn();
                }

                // Float name label at the last known position
                if (_nameLabel != null)
                    _nameLabel.transform.position = _targetPosition + Vector3.up * 1.5f;
                return;
            }

            _spawnRetryTimer = 0f;

            // ---- keep the avatar alive against the game's coop lifecycle ----

            // 1) Re-activate the gameObject (HideCoopPlayerTemporarily etc.)
            if (!_avatar.gameObject.activeSelf)
                _avatar.gameObject.SetActive(true);

            // 2) If the avatar was removed from the players list (should not
            //    happen now that the Harmony patches block removal, but keep
            //    as a safety net) re-add it SILENTLY.  Calling
            //    RefreshPlayersCount(true) fires OnPlayerJoined which triggers
            //    faith rewards and difficulty resets every frame → lag.
            try
            {
                if (PlayerFarming.players != null && !PlayerFarming.players.Contains(_avatar))
                {
                    PlayerFarming.players.Add(_avatar);
                    PlayerFarming.playersCount = PlayerFarming.players.Count;
                }
            }
            catch { }

            // 3) Keep CoopActive true so the game doesn't clean up our slot
            if (!CoopManager.CoopActive)
                CoopManager.CoopActive = true;

            // 4) Keep the Spine mesh renderer visible.  The game disables
            //    this during spawn effects and HideCoopPlayerTemporarily.
            try
            {
                if (_spineMesh == null && _avatar.Spine != null)
                    _spineMesh = _avatar.Spine.GetComponent<MeshRenderer>();
                if (_spineMesh != null && !_spineMesh.enabled)
                    _spineMesh.enabled = true;
            }
            catch { _spineMesh = null; }

            // ---- interpolate position ----

            _avatar.transform.position = Vector3.Lerp(
                _avatar.transform.position, _targetPosition, LerpSpeed * Time.deltaTime);

            // ---- state machine ----

            if (_avatar.state != null)
            {
                _avatar.state.facingAngle = _targetAngle;

                // Prevent the game from auto-despawning the remote avatar
                if (_avatar.state.CURRENT_STATE == StateMachine.State.InActive)
                    _avatar.state.CURRENT_STATE = StateMachine.State.Idle;
                else if (_avatar.state.CURRENT_STATE != _targetState)
                    _avatar.state.CURRENT_STATE = _targetState;
            }

            // ---- animation ----
            // Drive the Spine skeleton directly so the remote avatar
            // plays the correct animation instead of gliding.
            try
            {
                if (!string.IsNullOrEmpty(_targetAnimName) && _avatar.Spine != null)
                {
                    var spineAnim = _avatar.Spine as Spine.Unity.SkeletonAnimation;
                    if (spineAnim != null && spineAnim.valid)
                    {
                        var animState = spineAnim.AnimationState;
                        if (animState != null)
                        {
                            var current = animState.GetCurrent(0);
                            // Only set animation when the skeleton has been processed
                            // at least once (current != null) to avoid the
                            // "AnimationState waiting for processing" warning.
                            if (current != null && current.Animation != null
                                && current.Animation.Name != _targetAnimName)
                            {
                                var anim = spineAnim.Skeleton?.Data?.FindAnimation(_targetAnimName);
                                if (anim != null)
                                {
                                    animState.SetAnimation(0, anim, true);
                                    _lastAppliedAnim = _targetAnimName;
                                }
                            }
                        }
                    }
                }
            }
            catch { _lastAppliedAnim = null; }

            // ---- health ----

            if (_avatar.health != null && _totalHp > 0f)
            {
                if (System.Math.Abs(_avatar.health.HP - _hp) > 0.05f)
                    _avatar.health.HP = _hp;
            }

            // ---- name label ----

            if (_nameLabel != null)
                _nameLabel.transform.position = _avatar.transform.position + Vector3.up * 1.5f;
        }

        /**
         * @brief
         * Clears avatar and label references without destroying them.
         * Used after a scene transition where Unity has already destroyed
         * the game objects; the retry timer in Tick() will re-spawn.
         */
        public void ResetAvatar()
        {
            _avatar          = null;
            _spineMesh       = null;
            _targetAnimName  = null;
            _lastAppliedAnim = null;
            if (_nameLabel != null)
                Object.Destroy(_nameLabel);
            _nameLabel       = null;
            _spawnRetryTimer = 0f;
        }

        /* ------------------------------------------------------------------ */
        /* Despawn                                                              */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Removes avatar and label from the scene.
         */
        public void Despawn()
        {
            if (_nameLabel != null)
            {
                Object.Destroy(_nameLabel);
                _nameLabel = null;
            }

            _spineMesh = null;

            if (_avatar != null && !_avatar.isLamb)
            {
                /* Remove from the players list before deactivating so the
                   game does not try to interact with the deactivated object. */
                try
                {
                    if (PlayerFarming.players != null)
                        PlayerFarming.players.Remove(_avatar);
                }
                catch { }

                _avatar.gameObject.SetActive(false);
                CoopManager.CoopActive = false;
                try { PlayerFarming.RefreshPlayersCount(false); } catch { }
                _avatar = null;
            }
        }

        /* ------------------------------------------------------------------ */
        /* Helpers                                                              */
        /* ------------------------------------------------------------------ */

        private static GameObject BuildNameLabel(string text, Vector3 worldPos)
        {
            try
            {
                // Find an existing TMP component BEFORE creating our own Canvas
                // so FindObjectOfType does not return our empty one.
                TMPro.TMP_Text tmPrefab = null;
                var sceneCanvas = Object.FindObjectOfType<Canvas>();
                if (sceneCanvas != null)
                    tmPrefab = sceneCanvas.GetComponentInChildren<TMPro.TMP_Text>(true);

                var go = new GameObject($"RemotePlayer_Label_{text}");
                go.transform.position = worldPos;

                // A world-space Canvas is required for cloned TextMeshProUGUI
                // components to render – without one the label was invisible.
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode  = RenderMode.WorldSpace;
                canvas.sortingOrder = 100;

                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(4f, 1f);
                go.transform.localScale = Vector3.one * 0.01f;

                if (tmPrefab != null)
                {
                    var label = Object.Instantiate(tmPrefab.gameObject, go.transform);
                    label.transform.localPosition = Vector3.zero;

                    // Stretch the cloned label to fill the parent Canvas
                    var labelRt = label.GetComponent<RectTransform>();
                    if (labelRt != null)
                    {
                        labelRt.anchorMin = Vector2.zero;
                        labelRt.anchorMax = Vector2.one;
                        labelRt.offsetMin = Vector2.zero;
                        labelRt.offsetMax = Vector2.zero;
                    }

                    var tm = label.GetComponent<TMPro.TMP_Text>();
                    if (tm != null)
                    {
                        tm.text      = text;
                        tm.fontSize  = 36f;
                        tm.alignment = TMPro.TextAlignmentOptions.Center;
                    }
                }
                return go;
            }
            catch
            {
                return new GameObject($"RemotePlayer_Label_{text}");
            }
        }
    }
}

/* EOF */
