/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Sync local player to server; apply server relays to remote players
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using COTLMP.Data;
using COTLMP.Debug;
using I2.Loc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.SceneManagement;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Network
{
    /**
     * @brief
     * MonoBehaviour component added to Plugin.MonoInstance at startup.
     *
     * Responsibilities:
     *   - Own and manage the active Client connection.
     *   - On each frame (throttled): read local PlayerFarming state and send
     *     position / state / health updates to the server.
     *   - Receive network callbacks (fired on background threads), enqueue them
     *     as Actions, and execute them safely on the main thread each frame.
     *   - Maintain a RemotePlayerInfo for every connected peer and tick them
     *     each frame for interpolation.
     */
    internal sealed class PlayerSync : MonoBehaviour
    {
        /* ------------------------------------------------------------------ */
        /* Public accessors                                                     */
        /* ------------------------------------------------------------------ */

        /** The active client connection, or null if not connected */
        public static Client ActiveClient { get; private set; }

        public static bool IsConnected => ActiveClient != null && ActiveClient.IsConnected;

        /** Compressed host save data received from the server, waiting to be applied */
        public static byte[] PendingHostSaveData { get; private set; }

        /** Clears the pending host save data after it has been consumed */
        public static void ClearPendingHostSaveData() => PendingHostSaveData = null;

        /* ------------------------------------------------------------------ */
        /* Private fields                                                       */
        /* ------------------------------------------------------------------ */

        private static readonly Dictionary<int, RemotePlayerInfo> _remotePlayers
            = new Dictionary<int, RemotePlayerInfo>();

        private static readonly ConcurrentQueue<Action> _mainThreadQueue
            = new ConcurrentQueue<Action>();

        // Position / state / health send throttle
        private float _syncTimer;
        private const float SyncInterval = 0.05f; // 20 Hz

        // Heartbeat timer
        private float _heartbeatTimer;
        private const float HeartbeatInterval = 5f;

        // World-state heartbeat timer (host only)
        private float _worldStateTimer;
        private const float WorldStateInterval = 2f;

        // Last-sent values to avoid redundant state/health packets
        private StateMachine.State _lastState  = StateMachine.State.Idle;
        private float              _lastHp     = -1f;
        private string             _lastAnimName = "";

        // Scene transition grace period – suppress position sync briefly
        // after a scene load to prevent loading-zone rubber-banding.
        private static float _sceneTransitionGrace;
        private const float SceneTransitionGraceTime = 2f;

        // Chat state
        private static readonly System.Collections.Generic.List<ChatEntry> _chatMessages
            = new System.Collections.Generic.List<ChatEntry>();
        private bool   _chatOpen;
        private string _chatInput = "";
        private const int   MaxChatMessages     = 8;
        private const float ChatMessageLifetime  = 10f;

        private struct ChatEntry
        {
            public string Text;
            public float  Timestamp;
        }

        /* ------------------------------------------------------------------ */
        /* Public API (call from main thread)                                   */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Replaces the active client connection.  Wires up all network events
         * and tears down the previous client if one exists.
         */
        public static void SetClient(Client client)
        {
            // Tear down existing connection
            if (ActiveClient != null)
            {
                ActiveClient.Dispose();
                ActiveClient = null;
            }

            ClearRemotePlayers();

            if (client == null) return;
            ActiveClient = client;

            // Wire events — lambdas marshal to main thread via the queue
            client.Connected            += id     => Enqueue(() => OnConnected(id));
            client.Disconnected         += ()     => Enqueue(OnDisconnected);
            client.KickedFromServer     += reason => Enqueue(() => OnKicked(reason));
            client.PlayerJoined         += (id, name) => Enqueue(() => OnRemoteJoined(id, name));
            client.PlayerLeft           += id     => Enqueue(() => OnRemoteLeft(id));
            client.PlayerPositionUpdated += (id, x, y, a) => Enqueue(() => OnRemotePosition(id, x, y, a));
            client.PlayerStateUpdated   += (id, s) => Enqueue(() => OnRemoteState(id, s));
            client.PlayerHealthUpdated  += (id, hp, tot) => Enqueue(() => OnRemoteHealth(id, hp, tot));
            client.PlayerAnimationUpdated += (id, anim) => Enqueue(() => OnRemoteAnim(id, anim));
            client.ChatMessageReceived  += (id, msg) => Enqueue(() => OnChatMessage(id, msg));
            client.HostSaveDataReceived += data => Enqueue(() => OnHostSaveDataReceived(data));
            client.WorldStateHeartbeatReceived += data => Enqueue(() => OnWorldStateHeartbeat(data));
            client.SceneChangeReceived += scene => Enqueue(() => OnSceneChange(scene));
        }

        /* ------------------------------------------------------------------ */
        /* Unity lifecycle                                                      */
        /* ------------------------------------------------------------------ */

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnGameSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
        }

        private void Update()
        {
            // Drain main-thread callback queue first
            while (_mainThreadQueue.TryDequeue(out Action action))
            {
                try   { action?.Invoke(); }
                catch (Exception e)
                {
                    PrintLogger.Print(DebugLevel.ERROR_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT,
                        $"PlayerSync main-thread callback threw: {e.Message}");
                }
            }

            if (!IsConnected) return;

            // Tick down scene-transition grace period
            if (_sceneTransitionGrace > 0f)
                _sceneTransitionGrace -= Time.deltaTime;

            // Position / state / health throttle
            _syncTimer += Time.deltaTime;
            if (_syncTimer >= SyncInterval)
            {
                _syncTimer = 0f;
                SyncLocalPlayer();
            }

            // Heartbeat
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= HeartbeatInterval)
            {
                _heartbeatTimer = 0f;
                ActiveClient?.SendHeartbeat();
            }

            // World-state heartbeat (host only)
            if (Data.InternalData.IsHost)
            {
                _worldStateTimer += Time.deltaTime;
                if (_worldStateTimer >= WorldStateInterval)
                {
                    _worldStateTimer = 0f;
                    SendWorldStateHeartbeat();
                }
            }

            // Tick all remote player avatars for interpolation
            foreach (var rp in _remotePlayers.Values)
                rp.Tick();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
            ActiveClient?.Dispose();
            ActiveClient = null;
            ClearRemotePlayers();
        }

        /**
         * @brief
         * When a game scene loads (not Main Menu), reset all remote player
         * avatars so the retry timer in Tick() re-creates them once the
         * new scene's CoopManager and SessionHandler are ready.
         */
        private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.Equals("Main Menu")) return;
            if (!IsConnected) return;

            _sceneTransitionGrace = SceneTransitionGraceTime;

            // Host: tell all clients to follow to this scene
            if (Data.InternalData.IsHost)
                ActiveClient?.SendSceneChange(scene.name);

            StartCoroutine(RespawnRemotePlayersDelayed());
        }

        private System.Collections.IEnumerator RespawnRemotePlayersDelayed()
        {
            // Wait a few frames for the new scene to fully initialise
            yield return null;
            yield return null;
            yield return null;

            foreach (var rp in _remotePlayers.Values)
                rp.ResetAvatar();
            // Tick()'s retry timer will call TrySpawn() on each RemotePlayerInfo
        }

        /* ------------------------------------------------------------------ */
        /* Local player sync                                                    */
        /* ------------------------------------------------------------------ */

        private void SyncLocalPlayer()
        {
            // Don't send position during scene transition grace period
            // to avoid re-triggering loading zones on remote clients.
            if (_sceneTransitionGrace > 0f) return;

            try
            {
                var pf = PlayerFarming.Instance;
                if (pf == null) return;

                // Position
                Vector3 pos   = pf.transform.position;
                float   angle = pf.state?.facingAngle ?? 0f;
                ActiveClient.SendPosition(pos.x, pos.y, angle);

                // State (only on change)
                StateMachine.State st = pf.state?.CURRENT_STATE ?? StateMachine.State.Idle;
                if (st != _lastState)
                {
                    _lastState = st;
                    ActiveClient.SendState((int)st);
                }

                // Health (only on change)
                float hp    = pf.health?.HP                  ?? 0f;
                float totHp = pf.health?.PLAYER_TOTAL_HEALTH ?? 0f;
                if (Mathf.Abs(hp - _lastHp) > 0.01f)
                {
                    _lastHp = hp;
                    ActiveClient.SendHealth(hp, totHp);
                }

                // Animation (only on change)
                try
                {
                    var spineAnim = pf.Spine as Spine.Unity.SkeletonAnimation;
                    if (spineAnim != null && spineAnim.valid && spineAnim.AnimationState != null)
                    {
                        var track     = spineAnim.AnimationState.GetCurrent(0);
                        string animName = track?.Animation?.Name;
                        if (!string.IsNullOrEmpty(animName) && animName != _lastAnimName)
                        {
                            _lastAnimName = animName;
                            ActiveClient.SendAnimation(animName);
                        }
                    }
                }
                catch { }
            }
            catch (Exception e)
            {
                PrintLogger.Print(DebugLevel.WARNING_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT,
                    $"SyncLocalPlayer error: {e.Message}");
            }
        }

        /* ------------------------------------------------------------------ */
        /* Network event handlers (run on main thread via queue)               */
        /* ------------------------------------------------------------------ */

        private static void OnConnected(int id)
        {
            InternalData.IsMultiplayerSession = true;
            Plugin.Logger?.LogInfo($"[PlayerSync] Connected to server. Assigned ID: {id}");
        }

        private static void OnDisconnected()
        {
            Plugin.Logger?.LogInfo("[PlayerSync] Disconnected from server.");
            InternalData.IsMultiplayerSession = false;
            PendingHostSaveData = null;
            ClearRemotePlayers();
            ActiveClient = null;
        }

        private static void OnKicked(string reason)
        {
            Plugin.Logger?.LogWarning($"[PlayerSync] Kicked: {reason}");
            ClearRemotePlayers();
            ActiveClient = null;

            // Show feedback via the confirmation window pattern used elsewhere
            try
            {
                MonoSingleton<Lamb.UI.UIManager>.Instance
                    .ConfirmationWindowTemplate
                    .GetType(); // null-guard trick; actual push below
                Plugin.MonoInstance.StartCoroutine(ShowKickDialog(reason));
            }
            catch { /* not in a game scene, ignore */ }
        }

        private static System.Collections.IEnumerator ShowKickDialog(string reason)
        {
            yield return null; // wait a frame
            try
            {
                var win = MonoSingleton<Lamb.UI.UIManager>.Instance
                    .GetComponentInChildren<src.UI.UIMenuConfirmationWindow>(true);
                if (win != null)
                    win.Configure(MultiplayerModLocalization.UI.Disconnected,
                        string.IsNullOrEmpty(reason) ? MultiplayerModLocalization.UI.DisconnectedError : reason, true);
            }
            catch { }
        }

        private static void OnRemoteJoined(int id, string name)
        {
            if (_remotePlayers.ContainsKey(id)) return;
            var rp = new RemotePlayerInfo(id, name);
            _remotePlayers[id] = rp;
            rp.TrySpawn();
            Plugin.Logger?.LogInfo($"[PlayerSync] Remote player joined: '{name}' (ID {id})");
        }

        private static void OnRemoteLeft(int id)
        {
            if (!_remotePlayers.TryGetValue(id, out RemotePlayerInfo rp)) return;
            rp.Despawn();
            _remotePlayers.Remove(id);
            Plugin.Logger?.LogInfo($"[PlayerSync] Remote player left (ID {id})");
        }

        private static void OnRemotePosition(int id, float x, float y, float angle)
        {
            if (_sceneTransitionGrace > 0f) return;
            if (_remotePlayers.TryGetValue(id, out RemotePlayerInfo rp))
                rp.SetTargetPosition(new Vector3(x, y, 0f), angle);
        }

        private static void OnRemoteState(int id, int state)
        {
            if (_remotePlayers.TryGetValue(id, out RemotePlayerInfo rp))
                rp.SetState((StateMachine.State)state);
        }

        private static void OnRemoteHealth(int id, float hp, float totalHp)
        {
            if (_remotePlayers.TryGetValue(id, out RemotePlayerInfo rp))
                rp.SetHealth(hp, totalHp);
        }

        private static void OnRemoteAnim(int id, string animName)
        {
            if (_remotePlayers.TryGetValue(id, out RemotePlayerInfo rp))
                rp.SetAnimationName(animName);
        }

        private static void OnChatMessage(int id, string text)
        {
            string senderName = "Unknown";
            if (_remotePlayers.TryGetValue(id, out RemotePlayerInfo rp))
                senderName = rp.Name;

            Plugin.Logger?.LogInfo($"[Chat] {senderName}: {text}");
            AddChatMessage($"{senderName}: {text}");
        }

        private static void AddChatMessage(string text)
        {
            _chatMessages.Add(new ChatEntry { Text = text, Timestamp = Time.time });
            if (_chatMessages.Count > MaxChatMessages)
                _chatMessages.RemoveAt(0);
        }

        private static void OnHostSaveDataReceived(byte[] data)
        {
            PendingHostSaveData = data;
            Plugin.Logger?.LogInfo($"[PlayerSync] Received host save data ({data?.Length ?? 0} bytes compressed)");

            /* During an active session (not the initial join), write the
               updated save straight to the temp slot so the client's
               on-disk save stays in sync with the host.  This ensures
               that scene transitions reload the correct world state. */
            if (Data.InternalData.IsMultiplayerSession && !Data.InternalData.IsHost && data != null)
            {
                try
                {
                    const int MP_TEMP_SLOT = 99;
                    byte[] decompressed = DecompressSaveData(data);
                    string savesDir     = Path.Combine(UnityEngine.Application.persistentDataPath, "saves");
                    Directory.CreateDirectory(savesDir);
                    string slotName     = SaveAndLoad.MakeSaveSlot(MP_TEMP_SLOT);

                    bool   isJson   = decompressed.Length > 0 && decompressed[0] == (byte)'{';
                    string destExt  = isJson ? ".json" : ".mp";
                    string otherExt = isJson ? ".mp"   : ".json";
                    string destPath  = Path.Combine(savesDir, Path.ChangeExtension(slotName, destExt));
                    string otherPath = Path.Combine(savesDir, Path.ChangeExtension(slotName, otherExt));

                    if (File.Exists(otherPath)) File.Delete(otherPath);
                    File.WriteAllBytes(destPath, decompressed);
                    Plugin.Logger?.LogInfo($"[PlayerSync] Temp-slot save updated ({decompressed.Length} bytes)");
                }
                catch (Exception e)
                {
                    Plugin.Logger?.LogWarning($"[PlayerSync] Failed to write resync save: {e.Message}");
                }
            }
        }

        /**
         * @brief
         * Decompresses GZip-compressed save data received from the host.
         */
        private static byte[] DecompressSaveData(byte[] compressed)
        {
            using (var input  = new MemoryStream(compressed))
            using (var gz     = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gz.CopyTo(output);
                return output.ToArray();
            }
        }

        /**
         * @brief
         * Called on a client when the host's periodic world-state snapshot
         * arrives.  Decompresses, deserializes and applies the snapshot.
         */
        private static void OnWorldStateHeartbeat(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0) return;

            /* Hosts should never apply their own snapshot */
            if (Data.InternalData.IsHost) return;

            try
            {
                byte[] raw = WorldStateSnapshot.Decompress(compressedData);
                var snapshot = WorldStateSnapshot.Deserialize(raw);
                WorldStateSyncer.ApplySnapshot(snapshot);
            }
            catch (Exception e)
            {
                PrintLogger.Print(DebugLevel.WARNING_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT,
                    $"WorldState apply failed: {e.Message}");
            }
        }

        /**
         * @brief
         * Called on a client when the host transitions to a different scene.
         * Triggers a scene transition so the client follows the host.
         */
        private static void OnSceneChange(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (Data.InternalData.IsHost) return; // host drives scene changes

            string current = SceneManager.GetActiveScene().name;
            if (string.Equals(current, sceneName, StringComparison.Ordinal)) return;

            Plugin.Logger?.LogInfo($"[PlayerSync] Host changed scene to '{sceneName}', following...");

            _sceneTransitionGrace = SceneTransitionGraceTime;

            try
            {
                /* Use ChangeSceneAutoResume instead of ChangeRoomWaitToResume.
                   ChangeRoomWaitToResume waits for the game's internal "resume"
                   signal which never fires on the client for dungeon scenes
                   (the dungeon room management system is not initialised),
                   causing a permanent softlock. */
                MMTools.MMTransition.Play(
                    MMTools.MMTransition.TransitionType.ChangeSceneAutoResume,
                    MMTools.MMTransition.Effect.BlackFade,
                    sceneName, 1f, "", null, null);
            }
            catch (Exception e)
            {
                /* Fallback: direct scene load so the client never softlocks */
                Plugin.Logger?.LogWarning($"[PlayerSync] MMTransition failed, falling back to direct load: {e.Message}");
                try { SceneManager.LoadScene(sceneName); } catch { }
            }
        }

        /* ------------------------------------------------------------------ */
        /* Host-side world-state capture & send                                */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Captures the current game world into a snapshot, serializes and
         * compresses it, then sends it to the server for relay to all clients.
         */
        private static void SendWorldStateHeartbeat()
        {
            if (ActiveClient == null || !ActiveClient.IsConnected) return;

            try
            {
                var snapshot   = WorldStateSyncer.CaptureSnapshot();
                byte[] raw     = snapshot.Serialize();
                byte[] compressed = WorldStateSnapshot.Compress(raw);
                ActiveClient.SendWorldStateHeartbeat(compressed);
            }
            catch (Exception e)
            {
                PrintLogger.Print(DebugLevel.WARNING_LEVEL, DebugComponent.NETWORK_STACK_COMPONENT,
                    $"WorldState capture failed: {e.Message}");
            }
        }

        /* ------------------------------------------------------------------ */
        /* Helpers                                                              */
        /* ------------------------------------------------------------------ */

        private static void Enqueue(Action action) => _mainThreadQueue.Enqueue(action);

        /* ------------------------------------------------------------------ */
        /* Chat UI (OnGUI overlay)                                              */
        /* ------------------------------------------------------------------ */

        private void OnGUI()
        {
            if (!IsConnected) return;
            if (SceneManager.GetActiveScene().name == "Main Menu") return;

            Event e = Event.current;

            // Toggle chat open with T key (only when chat is closed)
            if (!_chatOpen && e.type == EventType.KeyDown && e.keyCode == KeyCode.T)
            {
                _chatOpen  = true;
                _chatInput = "";
                e.Use();
                return;
            }

            // ---- display recent messages ----

            float y   = Screen.height - 80;
            float now = Time.time;

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14
            };
            labelStyle.normal.textColor = Color.white;

            var shadowStyle = new GUIStyle(labelStyle);
            shadowStyle.normal.textColor = Color.black;

            for (int i = _chatMessages.Count - 1; i >= 0; i--)
            {
                var msg = _chatMessages[i];
                if (!_chatOpen && now - msg.Timestamp > ChatMessageLifetime)
                    continue;

                GUI.Label(new Rect(12, y + 1, 500, 24), msg.Text, shadowStyle);
                GUI.Label(new Rect(10, y, 500, 24), msg.Text, labelStyle);
                y -= 22;
            }

            if (!_chatOpen) return;

            // ---- input field ----

            /* Capture the key state BEFORE GUI.TextField processes
               the event.  TextField consumes KeyDown for Return and
               Escape internally, so the checks below would never see
               them if we tested after the TextField call. */
            bool enterPressed  = e.type == EventType.KeyDown &&
                                 (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
            bool escapePressed = e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape;

            GUI.SetNextControlName("ChatInput");
            _chatInput = GUI.TextField(new Rect(10, Screen.height - 45, 400, 30), _chatInput, 200);
            GUI.FocusControl("ChatInput");

            if (enterPressed)
            {
                if (!string.IsNullOrEmpty(_chatInput))
                {
                    ActiveClient?.SendChat(_chatInput);
                    AddChatMessage($"You: {_chatInput}");
                }
                _chatOpen  = false;
                _chatInput = "";
                e.Use();
            }
            else if (escapePressed)
            {
                _chatOpen  = false;
                _chatInput = "";
                e.Use();
            }
        }

        /* ------------------------------------------------------------------ */
        /* Helpers                                                              */
        /* ------------------------------------------------------------------ */

        private static void ClearRemotePlayers()
        {
            foreach (var rp in _remotePlayers.Values)
            {
                try { rp.Despawn(); } catch { }
            }
            _remotePlayers.Clear();
        }
    }
}

/* EOF */
