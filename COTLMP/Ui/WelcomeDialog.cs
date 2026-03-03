/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     First-time user welcome / instructions overlay
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using I2.Loc;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Ui
{
    /**
     * @brief
     * Displays a full-screen instructions overlay the first time the user opens
     * the Multiplayer server browser.  A "Don't show again" toggle lets the user
     * suppress it on subsequent visits.  The preference is persisted via
     * UnityEngine.PlayerPrefs.
     *
     * Usage:
     *   WelcomeDialog.ShowIfNeeded(onComplete);
     *
     * @param onComplete  Called (on the main thread) once the dialog is dismissed
     *                    or skipped because the user opted out.
     */
    internal static class WelcomeDialog
    {
        private const string PrefKey = "COTLMP_WelcomeSeen";

        /* ------------------------------------------------------------------ */
        /* Public API                                                           */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * If the user has not dismissed the dialog before (or has not opted out),
         * creates and shows the overlay then calls <paramref name="onComplete"/>
         * when they click "Got it!".  Otherwise calls <paramref name="onComplete"/>
         * immediately.
         */
        public static void ShowIfNeeded(Action onComplete)
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) == 1)
            {
                onComplete?.Invoke();
                return;
            }

            Show(onComplete);
        }

        /* ------------------------------------------------------------------ */
        /* Dialog creation                                                      */
        /* ------------------------------------------------------------------ */

        private static void Show(Action onComplete)
        {
            // ---- root overlay canvas (above everything) ----
            var root = new GameObject("COTLMP_WelcomeDialog");
            UnityEngine.Object.DontDestroyOnLoad(root);

            var canvas        = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1001; // above server browser (sortOrder 1000)

            var scaler               = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            // ---- dim background ----
            var dim   = MakeRect("Dim", root.transform);
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.80f);
            FillParent(dim.GetComponent<RectTransform>());

            // Capture pointer events so clicks don't fall through
            dim.AddComponent<Button>(); // swallow clicks

            // ---- card panel ----
            var card    = MakeRect("Card", root.transform);
            var cardRt  = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot     = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(700f, 520f);
            cardRt.anchoredPosition = Vector2.zero;

            var cardImg   = card.AddComponent<Image>();
            cardImg.color = new Color(0.08f, 0.08f, 0.13f, 0.98f);

            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding  = new RectOffset(40, 40, 36, 30);
            vlg.spacing  = 18f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // ---- title ----
            AddLabel(card.transform, MultiplayerModLocalization.UI.Welcome.Title,
                16f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, 60f);

            // ---- separator line ----
            var sep    = MakeRect("Sep", card.transform);
            var sepImg = sep.AddComponent<Image>();
            sepImg.color = new Color(0.35f, 0.35f, 0.50f, 1f);
            sep.AddComponent<LayoutElement>().preferredHeight = 2f;

            // ---- body text ----
            AddLabel(card.transform, MultiplayerModLocalization.UI.Welcome.Body,
                13f, new Color(0.85f, 0.85f, 0.85f, 1f), FontStyles.Normal,
                TextAlignmentOptions.TopLeft, 240f);

            // ---- "don't show again" toggle row ----
            bool dontShowAgain = false;
            var toggleRow  = MakeRect("ToggleRow", card.transform);
            toggleRow.AddComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            toggleRow.AddComponent<LayoutElement>().preferredHeight = 32f;

            var toggle = BuildToggle(toggleRow.transform, MultiplayerModLocalization.UI.Welcome.DontShow,
                val => dontShowAgain = val);

            // ---- confirm button ----
            var confirmBtn = BuildButton("ConfirmBtn", card.transform,
                MultiplayerModLocalization.UI.Welcome.Confirm,
                new Color(0.15f, 0.55f, 0.30f, 1f), 48f);

            confirmBtn.onClick.AddListener(() =>
            {
                if (dontShowAgain)
                    PlayerPrefs.SetInt(PrefKey, 1);

                UnityEngine.Object.Destroy(root);
                onComplete?.Invoke();
            });
        }

        /* ------------------------------------------------------------------ */
        /* UI helpers                                                           */
        /* ------------------------------------------------------------------ */

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
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text        = text;
            tm.fontSize    = fontSize;
            tm.color       = color;
            tm.fontStyle   = style;
            tm.alignment   = align;
            tm.overflowMode = TextOverflowModes.Overflow;
            tm.enableWordWrapping = true;
        }

        private static Toggle BuildToggle(Transform parent, string label, Action<bool> onChanged)
        {
            var go  = MakeRect("Toggle", parent);
            var le  = go.AddComponent<LayoutElement>();
            le.preferredHeight = 28f;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 10f;

            // Checkbox box
            var box    = MakeRect("Box", go.transform);
            var boxRt  = box.GetComponent<RectTransform>();
            boxRt.sizeDelta = new Vector2(22f, 22f);
            box.AddComponent<LayoutElement>().preferredWidth = 22f;
            var boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.25f, 0.25f, 0.35f, 1f);

            // Checkmark
            var checkGo  = MakeRect("Check", box.transform);
            var checkRt  = checkGo.GetComponent<RectTransform>();
            FillParent(checkRt);
            checkRt.offsetMin = new Vector2(3, 3);
            checkRt.offsetMax = new Vector2(-3, -3);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = new Color(0.15f, 0.80f, 0.40f, 1f);
            checkGo.SetActive(false);

            // Label
            var lblGo = MakeRect("Lbl", go.transform);
            lblGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var lbl  = lblGo.AddComponent<TextMeshProUGUI>();
            lbl.text      = label;
            lbl.fontSize  = 13f;
            lbl.color     = new Color(0.80f, 0.80f, 0.80f, 1f);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            // Toggle component
            var toggle          = go.AddComponent<Toggle>();
            toggle.targetGraphic = boxImg;
            toggle.graphic       = checkImg;
            toggle.isOn          = false;
            checkGo.SetActive(false);

            toggle.onValueChanged.AddListener(val =>
            {
                checkGo.SetActive(val);
                onChanged?.Invoke(val);
            });

            return toggle;
        }

        private static Button BuildButton(string name, Transform parent,
            string label, Color bgColor, float height)
        {
            var go  = MakeRect(name, parent);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            ColorBlock cb       = btn.colors;
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.2f);
            cb.pressedColor     = Color.Lerp(bgColor, Color.black, 0.2f);
            btn.colors          = cb;

            var txtGo = MakeRect("Label", go.transform);
            FillParent(txtGo.GetComponent<RectTransform>());
            var tm = txtGo.AddComponent<TextMeshProUGUI>();
            tm.text      = label;
            tm.fontSize  = 15f;
            tm.color     = Color.white;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;

            return btn;
        }
    }
}

/* EOF */
