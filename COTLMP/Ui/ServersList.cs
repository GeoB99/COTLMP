/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Multiplayer server browser overlay
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using COTLMP.Data;
using COTLMP.Network;
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using MMTools;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains the server browser overlay shown when the user clicks the
 * Multiplayer button on the main menu.
 *
 * @class ServerList
 * Full-screen overlay that lists discovered LAN servers, allows direct
 * IP:Port entry, and manages the connection flow.  Shows the first-time
 * WelcomeDialog before the browser opens, and closes cleanly when the
 * user presses Back or when a new scene loads.
 */
namespace COTLMP.Ui
{
    internal static class ServerList
    {
        /* ------------------------------------------------------------------ */
        /* Private state                                                        */
        /* ------------------------------------------------------------------ */

        private static GameObject        _root;
        private static TextMeshProUGUI   _statusText;
        private static Transform         _listContent;
        private static TMP_InputField    _ipInput;
        private static List<ServerEntry> _entries       = new List<ServerEntry>();
        private static ServerEntry       _selectedEntry;

        private enum ConnectState { Idle, Pending, Success, Failed }
        private static volatile ConnectState _connectState = ConnectState.Idle;

        /* ------------------------------------------------------------------ */
        /* Public API                                                           */
        /* ------------------------------------------------------------------ */

        /** True while the browser overlay is on screen */
        public static bool IsOpen => _root != null;

        /**
         * @brief
         * Entry point called by the MainMenu patch when the user clicks
         * "Multiplayer".  Shows the WelcomeDialog once, then opens the browser.
         */
        public static void DisplayUi()
        {
            if (_root != null) return;
            WelcomeDialog.ShowIfNeeded(OpenBrowser);
        }

        /**
         * @brief
         * Closes and destroys the browser overlay.  Safe to call multiple times.
         */
        public static void Close()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }

            _statusText    = null;
            _listContent   = null;
            _ipInput       = null;
            _selectedEntry = null;
            _entries.Clear();
            _connectState  = ConnectState.Idle;
        }

        /* ------------------------------------------------------------------ */
        /* Browser creation                                                     */
        /* ------------------------------------------------------------------ */

        private static void OpenBrowser()
        {
            if (_root != null) return;

            // Ensure a single subscription; unsubscribed again in Close()
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            // ---- root overlay canvas (covers the main menu entirely) ----
            _root = new GameObject("COTLMP_ServerBrowser");
            UnityEngine.Object.DontDestroyOnLoad(_root);

            var canvas         = _root.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler               = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            _root.AddComponent<GraphicRaycaster>();

            // ---- full-screen opaque background (hides the main menu) ----
            var bg    = MakeRect("Background", _root.transform);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.04f, 0.08f, 1f);
            FillParent(bg.GetComponent<RectTransform>());

            // ---- main panel ----
            var panel   = MakeRect("Panel", _root.transform);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRt.pivot            = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta        = new Vector2(860f, 600f);
            panelRt.anchoredPosition = Vector2.zero;

            panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.13f, 0.98f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding                = new RectOffset(24, 24, 20, 20);
            vlg.spacing                = 12f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment         = TextAnchor.UpperCenter;

            // Title
            AddLabel(panel.transform,
                MultiplayerModLocalization.UI.ServerList.ServerList_MainDescription,
                18f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, 44f);

            // Separator
            var sep = MakeRect("Sep", panel.transform);
            sep.AddComponent<Image>().color = new Color(0.35f, 0.35f, 0.50f, 1f);
            sep.AddComponent<LayoutElement>().preferredHeight = 2f;

            // Status label
            var statusGo = MakeRect("Status", panel.transform);
            statusGo.AddComponent<LayoutElement>().preferredHeight = 28f;
            _statusText           = statusGo.AddComponent<TextMeshProUGUI>();
            _statusText.text      = MultiplayerModLocalization.UI.ServerList.ServerList_SelectServer;
            _statusText.fontSize  = 12f;
            _statusText.color     = new Color(0.70f, 0.70f, 0.70f, 1f);
            _statusText.alignment = TextAlignmentOptions.Center;

            BuildScrollList(panel.transform);
            BuildDirectConnectRow(panel.transform);
            BuildToolbar(panel.transform);
        }

        /* ------------------------------------------------------------------ */
        /* Sub-panel builders                                                   */
        /* ------------------------------------------------------------------ */

        private static void BuildScrollList(Transform parent)
        {
            var scrollGo   = MakeRect("Scroll", parent);
            scrollGo.AddComponent<LayoutElement>().preferredHeight = 280f;
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewportGo = MakeRect("Viewport", scrollGo.transform);
            FillParent(viewportGo.GetComponent<RectTransform>());
            viewportGo.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 1f);
            viewportGo.AddComponent<Mask>().showMaskGraphic = true;

            var contentGo = MakeRect("Content", viewportGo.transform);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero;

            var cvlg = contentGo.AddComponent<VerticalLayoutGroup>();
            cvlg.spacing                = 4f;
            cvlg.childForceExpandWidth  = true;
            cvlg.childForceExpandHeight = false;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
            scrollRect.content  = contentRt;
            _listContent        = contentGo.transform;
        }

        private static void BuildDirectConnectRow(Transform parent)
        {
            var row = MakeRect("DirectConnect", parent);
            row.AddComponent<LayoutElement>().preferredHeight = 36f;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 8f;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;

            // IP input
            var inputGo  = MakeRect("IPInput", row.transform);
            inputGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var inputImg = inputGo.AddComponent<Image>();
            inputImg.color = new Color(0.15f, 0.15f, 0.22f, 1f);
            _ipInput = inputGo.AddComponent<TMP_InputField>();

            var textGo     = MakeRect("Text", inputGo.transform);
            FillParent(textGo.GetComponent<RectTransform>());
            var textTm     = textGo.AddComponent<TextMeshProUGUI>();
            textTm.fontSize = 12f;
            textTm.color    = Color.white;
            textTm.margin   = new Vector4(6f, 0f, 6f, 0f);

            var phGo      = MakeRect("Placeholder", inputGo.transform);
            FillParent(phGo.GetComponent<RectTransform>());
            var phTm      = phGo.AddComponent<TextMeshProUGUI>();
            phTm.text     = MultiplayerModLocalization.UI.ServerList.ServerList_IpPlaceholder;
            phTm.fontSize = 12f;
            phTm.color    = new Color(0.5f, 0.5f, 0.5f, 1f);
            phTm.fontStyle = FontStyles.Italic;
            phTm.margin   = new Vector4(6f, 0f, 6f, 0f);

            _ipInput.textComponent = textTm;
            _ipInput.placeholder   = phTm;
            _ipInput.targetGraphic = inputImg;

            var dcBtn = BuildButton("DCBtn", row.transform,
                MultiplayerModLocalization.UI.ServerList.ServerList_DirectConnectButton,
                new Color(0.15f, 0.40f, 0.65f, 1f), 36f, 160f);
            dcBtn.onClick.AddListener(OnDirectConnect);
        }

        private static void BuildToolbar(Transform parent)
        {
            var toolbar = MakeRect("Toolbar", parent);
            toolbar.AddComponent<LayoutElement>().preferredHeight = 44f;
            var hlg = toolbar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 8f;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;

            var backBtn = BuildButton("BackBtn", toolbar.transform,
                MultiplayerModLocalization.UI.ServerList.ServerList_BackButton,
                new Color(0.50f, 0.15f, 0.15f, 1f), 44f, 120f);
            backBtn.onClick.AddListener(Close);

            // Flexible spacer pushes scan/connect to the right
            var spacer = MakeRect("Spacer", toolbar.transform);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var scanBtn = BuildButton("ScanBtn", toolbar.transform,
                MultiplayerModLocalization.UI.ServerList.ServerList_ScanButton,
                new Color(0.15f, 0.45f, 0.25f, 1f), 44f, 140f);
            scanBtn.onClick.AddListener(OnScan);

            var connectBtn = BuildButton("ConnectBtn", toolbar.transform,
                MultiplayerModLocalization.UI.ServerList.ServerList_ConnectButton,
                new Color(0.15f, 0.55f, 0.30f, 1f), 44f, 120f);
            connectBtn.onClick.AddListener(OnConnect);
        }

        /* ------------------------------------------------------------------ */
        /* Button handlers                                                      */
        /* ------------------------------------------------------------------ */

        private static void OnScan()
        {
            _selectedEntry = null;
            ClearList();
            SetStatus(MultiplayerModLocalization.UI.ServerList.ServerList_Scanning);

            LanDiscovery.ScanAsync(entries =>
                Plugin.MonoInstance?.StartCoroutine(ApplyScanResults(entries)));
        }

        private static IEnumerator ApplyScanResults(List<ServerEntry> entries)
        {
            yield return null; // wait one frame so we're on the main thread

            if (_root == null) yield break;

            _entries = entries ?? new List<ServerEntry>();
            ClearList();

            if (_entries.Count == 0)
            {
                SetStatus(MultiplayerModLocalization.UI.ServerList.ServerList_NoneFound);
            }
            else
            {
                SetStatus(string.Format(
                    MultiplayerModLocalization.UI.ServerList.ServerList_Found, _entries.Count));

                foreach (var entry in _entries)
                {
                    var captured = entry;
                    AddListRow(captured, () => _selectedEntry = captured);
                }
            }
        }

        private static void OnConnect()
        {
            if (_selectedEntry == null) return;
            ConnectToServer(_selectedEntry.Address, _selectedEntry.Port);
        }

        private static void OnDirectConnect()
        {
            string raw = _ipInput?.text?.Trim();
            if (string.IsNullOrEmpty(raw)) return;

            int port     = 7777;
            int colonIdx = raw.LastIndexOf(':');
            if (colonIdx > 0 && int.TryParse(raw.Substring(colonIdx + 1), out int p))
            {
                port = p;
                raw  = raw.Substring(0, colonIdx);
            }

            if (!IPAddress.TryParse(raw, out IPAddress addr))
            {
                SetStatus(MultiplayerModLocalization.UI.ServerList.ServerList_ConnectFailed);
                return;
            }

            ConnectToServer(addr, port);
        }

        /* ------------------------------------------------------------------ */
        /* Connection logic                                                     */
        /* ------------------------------------------------------------------ */

        private static void ConnectToServer(IPAddress addr, int port)
        {
            SetStatus(MultiplayerModLocalization.UI.ServerList.ServerList_Connecting);
            _connectState = ConnectState.Pending;

            string playerName = Data.InternalData.GetLocalPlayerName();
            var client        = new Client();

            // Capture result on the network thread; WaitForConnection reads it
            client.Connected    += id => _connectState = ConnectState.Success;
            client.Disconnected += ()  =>
            {
                if (_connectState == ConnectState.Pending)
                    _connectState = ConnectState.Failed;
            };

            // Wire all PlayerSync handlers BEFORE Connect() so that server
            // responses (PlayerJoinAck, PlayerJoin for existing players) are
            // not lost to the race between the network thread and main thread.
            PlayerSync.SetClient(client);

            if (!client.Connect(addr, port, playerName))
            {
                _connectState = ConnectState.Idle;
                SetStatus(MultiplayerModLocalization.UI.ServerList.ServerList_ConnectFailed);
                PlayerSync.SetClient(null);
                return;
            }

            Plugin.MonoInstance?.StartCoroutine(WaitForConnection());
        }

        private static IEnumerator WaitForConnection()
        {
            const float Timeout = 6f;
            float elapsed = 0f;

            while (_connectState == ConnectState.Pending && elapsed < Timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (_root == null) yield break; // browser already closed

            if (_connectState == ConnectState.Success)
            {
                // Wait for the host's save data so we load into the same world
                const float SaveTimeout = 10f;
                float saveElapsed = 0f;
                while (PlayerSync.PendingHostSaveData == null && saveElapsed < SaveTimeout)
                {
                    yield return null;
                    saveElapsed += Time.deltaTime;
                }

                Close();

                // Use a dedicated temp slot for multiplayer so we never
                // overwrite the player's own save files.
                const int MP_TEMP_SLOT = 99;
                SaveAndLoad.SAVE_SLOT = MP_TEMP_SLOT;

                // Write the host's save into our temp slot so
                // SaveAndLoad.Load() picks it up when the scene loads.
                if (PlayerSync.PendingHostSaveData != null)
                {
                    try
                    {
                        byte[] decompressed = DecompressSaveData(PlayerSync.PendingHostSaveData);
                        string savesDir     = Path.Combine(Application.persistentDataPath, "saves");
                        Directory.CreateDirectory(savesDir);
                        string slotName     = SaveAndLoad.MakeSaveSlot(MP_TEMP_SLOT);

                        // Detect format: JSON text starts with '{', MessagePack binary does not.
                        // The game tries .mp (MessagePack) first; we must write with the matching
                        // extension and remove the other to prevent stale data conflicts.
                        bool   isJson   = decompressed.Length > 0 && decompressed[0] == (byte)'{';
                        string destExt  = isJson ? ".json" : ".mp";
                        string otherExt = isJson ? ".mp"   : ".json";
                        string destPath  = Path.Combine(savesDir, Path.ChangeExtension(slotName, destExt));
                        string otherPath = Path.Combine(savesDir, Path.ChangeExtension(slotName, otherExt));

                        if (File.Exists(otherPath)) File.Delete(otherPath);
                        File.WriteAllBytes(destPath, decompressed);
                        PlayerSync.ClearPendingHostSaveData();
                        Plugin.Logger?.LogInfo($"[ServerList] Host save written to {destPath}");
                    }
                    catch (Exception e)
                    {
                        Plugin.Logger?.LogError($"[ServerList] Failed to write host save: {e.Message}");
                    }
                }

                // Transition to the game scene exactly like the game does in
                // LoadMenu.ContinueGame — scene load first, then read save
                // data inside the transition callback.
                AudioManager.Instance.StopCurrentMusic();
                MMTransition.Play(
                    MMTransition.TransitionType.ChangeRoomWaitToResume,
                    MMTransition.Effect.BlackFade,
                    "Base Biome 1", 3f, "",
                    new Action(OnTransitionedToGame), null);
            }
            else
                SetStatus(MultiplayerModLocalization.UI.ServerList.ServerList_ConnectFailed);
        }

        /**
         * @brief
         * Callback invoked by MMTransition once the "Base Biome 1" scene is
         * loaded.  Mirrors LoadMenu.ContinueGameCallback from the base game.
         */
        private static void OnTransitionedToGame()
        {
            AudioManager.Instance.StopCurrentMusic();
            SaveAndLoad.Load(SaveAndLoad.SAVE_SLOT);
        }

        /* ------------------------------------------------------------------ */
        /* Scene change                                                         */
        /* ------------------------------------------------------------------ */

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Close();
        }

        /* ------------------------------------------------------------------ */
        /* List helpers                                                         */
        /* ------------------------------------------------------------------ */

        private static void ClearList()
        {
            if (_listContent == null) return;
            foreach (Transform child in _listContent)
                UnityEngine.Object.Destroy(child.gameObject);
        }

        private static void AddListRow(ServerEntry entry, Action onSelected)
        {
            var go = MakeRect("Row", _listContent);
            go.AddComponent<LayoutElement>().preferredHeight = 34f;

            var img   = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.18f, 1f);

            var btn             = go.AddComponent<Button>();
            ColorBlock cb       = btn.colors;
            cb.highlightedColor = new Color(0.20f, 0.20f, 0.30f, 1f);
            cb.pressedColor     = new Color(0.10f, 0.10f, 0.16f, 1f);
            btn.colors          = cb;
            btn.targetGraphic   = img;

            btn.onClick.AddListener(() =>
            {
                if (_listContent != null)
                    foreach (Transform child in _listContent)
                    {
                        var rowImg = child.GetComponent<Image>();
                        if (rowImg != null) rowImg.color = new Color(0.12f, 0.12f, 0.18f, 1f);
                    }

                img.color = new Color(0.18f, 0.35f, 0.50f, 1f);
                onSelected?.Invoke();
            });

            var textGo   = MakeRect("Label", go.transform);
            FillParent(textGo.GetComponent<RectTransform>());
            var tm       = textGo.AddComponent<TextMeshProUGUI>();
            tm.text      = entry.DisplayLine;
            tm.fontSize  = 11f;
            tm.color     = new Color(0.85f, 0.85f, 0.85f, 1f);
            tm.margin    = new Vector4(8f, 0f, 8f, 0f);
            tm.alignment = TextAlignmentOptions.MidlineLeft;
        }

        /* ------------------------------------------------------------------ */
        /* General UI helpers                                                   */
        /* ------------------------------------------------------------------ */

        private static void SetStatus(string text)
        {
            if (_statusText != null) _statusText.text = text;
        }

        private static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void FillParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void AddLabel(Transform parent, string text, float fontSize,
            Color color, FontStyles style, TextAlignmentOptions align, float preferredHeight)
        {
            var go = MakeRect("Label", parent);
            go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            var tm                = go.AddComponent<TextMeshProUGUI>();
            tm.text               = text;
            tm.fontSize           = fontSize;
            tm.color              = color;
            tm.fontStyle          = style;
            tm.alignment          = align;
            tm.overflowMode       = TextOverflowModes.Overflow;
            tm.enableWordWrapping = true;
        }

        private static Button BuildButton(string name, Transform parent,
            string label, Color bgColor, float height, float width)
        {
            var go = MakeRect(name, parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.preferredWidth  = width;

            var img   = go.AddComponent<Image>();
            img.color = bgColor;

            var btn             = go.AddComponent<Button>();
            ColorBlock cb       = btn.colors;
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.2f);
            cb.pressedColor     = Color.Lerp(bgColor, Color.black, 0.2f);
            btn.colors          = cb;

            var txtGo    = MakeRect("Label", go.transform);
            FillParent(txtGo.GetComponent<RectTransform>());
            var tm       = txtGo.AddComponent<TextMeshProUGUI>();
            tm.text      = label;
            tm.fontSize  = 13f;
            tm.color     = Color.white;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        /* ------------------------------------------------------------------ */
        /* Save data compression helpers                                        */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Decompresses GZip-compressed save data received from the host.
         */
        private static byte[] DecompressSaveData(byte[] compressed)
        {
            using (var input = new MemoryStream(compressed))
            using (var gz = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gz.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}

/* EOF */
