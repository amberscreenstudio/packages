// Pause menu built in uGUI, so clicks work with the Input System only and at timeScale 0.
// Pauses time and audio, frees the cursor and puts it back, Save/Load slot pages, fit-to-screen panel,
// live text size, and a version label with load warnings.
using System;
using System.Collections.Generic;
using AmberScreen.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AmberScreen.GameShell
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-80)]
    public class PauseFrame : MonoBehaviour
    {
        internal enum Page { Main, Save, Load }

        struct ExtraButton
        {
            public string caption;
            public Action action;
            public bool closesMenu;
        }

        public static PauseFrame Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        public static event Action Opened;
        public static event Action Closed;

        static readonly List<ExtraButton> s_Extra = new List<ExtraButton>();

        // Reference pixels at 1920x1080. Text size changes fonts only, not these sizes.
        internal const float PanelWidth = 420f;
        internal const float ButtonHeight = 44f;
        const float LabelHeight = 36f;
        const float TitleHeight = 32f;
        const float LineHeight = 28f;
        const float Spacing = 8f;
        internal const float MaxHeightFraction = 0.9f;
        internal const int TitleSize = 22;
        internal const int BodySize = 22;
        const int SmallSize = 18;

        Page m_Page;
        string m_Status = "";
        int m_ConfirmOverwriteSlot;   // 0 = slot list; >0 = overwrite confirm for that slot
        float m_PrevScale = 1f;
        bool m_PrevAudio;
        bool m_Open;
        bool m_Built;
        float m_AppliedTextScale = -1f;

        Canvas m_Canvas;
        GameObject m_Root;
        Image m_Dimmer;
        Image m_PanelImage;
        Text m_TitleText;
        Text m_SubtitleText;
        Text m_StatusText;
        Text m_VersionText;
        RectTransform m_PanelRt;
        VerticalLayoutGroup m_PanelLayout;
        ContentSizeFitter m_PanelFitter;
        LayoutElement m_ScrollHostLe;
        ScrollRect m_Scroll;
        RectMask2D m_ViewportMask;
        RectTransform m_Content;
        Font m_Font;
        GameShellTheme m_Theme;
        GameShellTheme m_ThemeOverride;
        CursorLockMode m_PrevLock;
        bool m_PrevCursorVisible;
        readonly List<Text> m_SizedTexts = new List<Text>();
        readonly List<int> m_SizedBases = new List<int>();
        bool m_LayoutDirty;
        int m_FitWidth;
        int m_FitHeight;
        float m_FitCanvasH;

        internal Page CurrentPage => m_Page;
        internal RectTransform Content => m_Content;
        internal RectTransform PanelRect => m_PanelRt;
        internal Text TitleText => m_TitleText;
        internal Text VersionText => m_VersionText;
        internal Image PanelImage => m_PanelImage;
        internal Canvas MenuCanvas => m_Canvas;
        internal string Status => m_Status;
        internal bool IsScrolling => m_Scroll != null && m_Scroll.vertical;

        /// <summary>
        /// Adds a button to the main page, above Settings/Quit (e.g. "Main menu", "Controls").
        /// Same caption replaces. <paramref name="closesMenu"/> closes the pause first.
        /// </summary>
        public static void AddMainButton(string caption, Action action, bool closesMenu = false)
        {
            if (string.IsNullOrEmpty(caption)) return;
            RemoveMainButton(caption);
            s_Extra.Add(new ExtraButton { caption = caption, action = action, closesMenu = closesMenu });
            if (Instance != null && Instance.m_Open && Instance.m_Page == Page.Main) Instance.RebuildPage();
        }

        public static void RemoveMainButton(string caption)
        {
            for (int i = s_Extra.Count - 1; i >= 0; i--)
                if (s_Extra[i].caption == caption) s_Extra.RemoveAt(i);
        }

        public static void ClearMainButtons() => s_Extra.Clear();

        /// <summary>Opens the pause (no-op if already open or a scene fade is running).</summary>
        public static void OpenMenu() { if (Instance != null) Instance.Open(); }

        public static void CloseMenu() { if (Instance != null) Instance.Close(); }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            // Fixed Update mode stops Input System while timeScale is 0.
            if (GameShell.Settings.fixInputUpdateMode && InputSystem.settings != null
                && InputSystem.settings.updateMode == InputSettings.UpdateMode.ProcessEventsInFixedUpdate)
            {
                InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
                Debug.Log("[GameShell] Pause: Input System update mode set to Dynamic (was Fixed).");
            }
            EnsureEventSystem();
            EnsureBuilt();
        }

        internal void EnsureBuilt()
        {
            if (Instance == null) Instance = this;
            if (m_Built) return;
            BuildUi();
            SetVisible(false);
        }

        void Update()
        {
            if (m_Open && InputSystem.settings != null
                && InputSystem.settings.updateMode == InputSettings.UpdateMode.ProcessEventsInFixedUpdate)
            {
                InputSystem.Update();
            }

            var settings = GameShell.Settings;
            if (!settings.pauseEnabled) return;
            var keyboard = Keyboard.current;
            bool key = keyboard != null && settings.pauseKey != Key.None && keyboard[settings.pauseKey].wasPressedThisFrame;
            var pad = settings.gamepadPause ? Gamepad.current : null;
            bool start = pad != null && pad.startButton.wasPressedThisFrame;
            bool east = pad != null && m_Open && pad.buttonEast.wasPressedThisFrame;
            if (start && m_Open) { Close(); return; }
            if (key || start || east) HandleBack();
        }

        /// <summary>What the pause key does: open, step back one page, or close.</summary>
        internal void HandleBack()
        {
            if (GameShell.EscapeGuarded) return;
            if (SceneFlow.IsFading) return;
            if (!m_Open) Open();
            else if (m_Page == Page.Save && m_ConfirmOverwriteSlot > 0)
            {
                m_ConfirmOverwriteSlot = 0;
                RebuildPage();
            }
            else if (m_Page != Page.Main) ShowPage(Page.Main);
            else Close();
        }

        void LateUpdate()
        {
            RefreshVersionLabel();
            if (!m_Open || m_PanelRt == null) return;
            ApplyTextScale();
            float canvasH = CanvasHeight();
            if (m_LayoutDirty || Screen.width != m_FitWidth || Screen.height != m_FitHeight
                || Mathf.Abs(canvasH - m_FitCanvasH) > 0.5f)
                FitPanel();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (!m_Open) return;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsOpen = false;
        }

        public void Open()
        {
            if (m_Open || SceneFlow.IsFading) return;
            EnsureBuilt();
            m_Open = true;
            IsOpen = true;
            m_Page = Page.Main;
            m_Status = "";
            m_ConfirmOverwriteSlot = 0;
            m_PrevScale = Time.timeScale;
            m_PrevAudio = AudioListener.pause;
            m_PrevLock = Cursor.lockState;
            m_PrevCursorVisible = Cursor.visible;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (m_Canvas != null) m_Canvas.sortingOrder = GameShell.Settings.pauseSortingOrder;
            SetVisible(true);
            RefreshChrome();
            RebuildPage();
            Opened?.Invoke();
        }

        public void Close()
        {
            if (!m_Open) return;
            m_Open = false;
            IsOpen = false;
            Time.timeScale = m_PrevScale <= 0f ? 1f : m_PrevScale;
            AudioListener.pause = m_PrevAudio;
            Cursor.lockState = m_PrevLock;
            Cursor.visible = m_PrevCursorVisible;
            SetVisible(false);
            Closed?.Invoke();
        }

        internal void ShowPage(Page next)
        {
            m_Page = next;
            m_Status = "";
            m_ConfirmOverwriteSlot = 0;
            RefreshChrome();
            RebuildPage();
        }

        void ReturnToMainWithStatus(string message)
        {
            m_Page = Page.Main;
            m_ConfirmOverwriteSlot = 0;
            m_Status = message ?? "";
            RefreshChrome();
            RebuildPage();
        }

        void SetVisible(bool visible)
        {
            if (m_Root != null) m_Root.SetActive(visible);
        }

        void RefreshChrome()
        {
            if (m_TitleText != null) m_TitleText.text = GameShell.Settings.pauseTitle ?? "";
            if (m_SubtitleText != null)
            {
                string sub = "";
                if (GameShell.PauseSubtitle != null)
                {
                    try { sub = GameShell.PauseSubtitle() ?? ""; }
                    catch (Exception e) { Debug.LogException(e); }
                }
                m_SubtitleText.text = sub;
            }
            if (m_StatusText != null) m_StatusText.text = m_Status ?? "";
            bool changed = false;
            changed |= ApplyLine(m_TitleText, m_TitleText != null && !string.IsNullOrEmpty(m_TitleText.text), TitleHeight);
            changed |= ApplyLine(m_SubtitleText, m_SubtitleText != null && !string.IsNullOrEmpty(m_SubtitleText.text), LineHeight);
            changed |= ApplyLine(m_StatusText, !string.IsNullOrEmpty(m_Status), LineHeight);
            RefreshFonts();
            if (changed && m_Open) m_LayoutDirty = true;
        }

        /// <summary>Re-reads GameShell.TextScale and resizes text if it changed (runs every frame while open).</summary>
        internal void ApplyTextScale()
        {
            float scale = GameShell.CurrentTextScale();
            if (Mathf.Approximately(scale, m_AppliedTextScale)) return;
            RefreshFonts();
            m_LayoutDirty = true;
        }

        void RefreshFonts()
        {
            m_AppliedTextScale = GameShell.CurrentTextScale();
            for (int i = m_SizedTexts.Count - 1; i >= 0; i--)
            {
                if (m_SizedTexts[i] == null)
                {
                    m_SizedTexts.RemoveAt(i);
                    m_SizedBases.RemoveAt(i);
                    continue;
                }
                m_SizedTexts[i].fontSize = GameShell.ScaledFont(m_SizedBases[i]);
            }
        }

        void Track(Text text, int baseSize)
        {
            if (text == null) return;
            m_SizedTexts.Add(text);
            m_SizedBases.Add(baseSize);
            text.fontSize = GameShell.ScaledFont(baseSize);
        }

        void ClearBodyFonts()
        {
            if (m_Content == null) return;
            for (int i = m_SizedTexts.Count - 1; i >= 0; i--)
            {
                var text = m_SizedTexts[i];
                if (text == null || text.transform.IsChildOf(m_Content))
                {
                    m_SizedTexts.RemoveAt(i);
                    m_SizedBases.RemoveAt(i);
                }
            }
        }

        internal void RebuildPage()
        {
            if (m_Content == null) return;
            ClearBodyFonts();
            for (int i = m_Content.childCount - 1; i >= 0; i--)
            {
                var child = m_Content.GetChild(i).gameObject;
                child.SetActive(false);
                child.transform.SetParent(transform, false);
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            if (m_Page == Page.Main) BuildMain();
            else if (m_Page == Page.Save) BuildSlots(true);
            else BuildSlots(false);

            m_LayoutDirty = true;
            FitPanel();
            RefreshLayout();
        }

        void RefreshLayout()
        {
            Canvas.ForceUpdateCanvases();
            if (m_PanelRt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_PanelRt);
        }

        float InnerWidth()
        {
            if (m_PanelLayout == null) return PanelWidth;
            return Mathf.Max(1f, PanelWidth - m_PanelLayout.padding.horizontal);
        }

        void FitWidth(LayoutElement le)
        {
            float width = InnerWidth();
            le.minWidth = width;
            le.preferredWidth = width;
            le.flexibleWidth = 0f;
        }

        void BuildMain()
        {
            var settings = GameShell.Settings;
            AddButton("Resume", () => { LogClick("Resume"); Close(); });
            if (settings.showSaveLoad)
            {
                AddButton("Save", () => { LogClick("Save"); ShowPage(Page.Save); });
                AddButton("Load", () => { LogClick("Load"); ShowPage(Page.Load); });
            }
            for (int i = 0; i < s_Extra.Count; i++)
            {
                var extra = s_Extra[i];
                AddButton(extra.caption, () =>
                {
                    LogClick(extra.caption);
                    if (extra.closesMenu) Close();
                    try { extra.action?.Invoke(); }
                    catch (Exception e) { Debug.LogException(e); }
                });
            }
            if (GameShell.OpenSettings != null)
            {
                AddButton("Settings", () =>
                {
                    LogClick("Settings");
                    try { GameShell.OpenSettings(); }
                    catch (Exception e) { Debug.LogException(e); }
                });
            }
            if (settings.showQuit) AddButton("Quit", () => { LogClick("Quit"); QuitGame(); });
        }

        void BuildSlots(bool saving)
        {
            if (saving && m_ConfirmOverwriteSlot > 0)
            {
                int slot = m_ConfirmOverwriteSlot;
                AddLabel("Overwrite slot " + slot + "?");
                AddLabel("Slot " + slot + "  —  " + GameShell.Summary(slot), 48f);
                AddButton("Overwrite", () =>
                {
                    LogClick("Overwrite slot " + slot);
                    bool ok = GameShell.Save(slot);
                    ReturnToMainWithStatus(ok ? "Saved to slot " + slot : "Save failed");
                });
                AddButton("Cancel", () =>
                {
                    LogClick("Cancel overwrite");
                    m_ConfirmOverwriteSlot = 0;
                    RebuildPage();
                });
                return;
            }

            AddLabel(saving ? "Save to a slot" : "Load a slot");
            // User slots 1 .. SlotCount-1 (slot 0 is autosave). Default SlotCount=4 → 1,2,3.
            int first = saving ? 1 : 0;
            for (int slot = first; slot < SaveStore.SlotCount; slot++)
            {
                int captured = slot;
                string name = captured == SaveStore.AutosaveSlot ? "Autosave" : "Slot " + captured;
                string caption = name + "  —  " + GameShell.Summary(captured);
                AddButton(caption, () =>
                {
                    LogClick(saving ? "Save slot " + captured : "Load slot " + captured);
                    if (saving)
                    {
                        if (SaveStore.Exists(captured))
                        {
                            m_ConfirmOverwriteSlot = captured;
                            RebuildPage();
                            return;
                        }
                        bool ok = GameShell.Save(captured);
                        ReturnToMainWithStatus(ok ? "Saved to slot " + captured : "Save failed");
                    }
                    else if (!SaveStore.Exists(captured))
                    {
                        m_Status = name + " is empty";
                        RefreshChrome();
                    }
                    else
                    {
                        Close();
                        var result = GameShell.Load(captured);
                        if (result == null || !result.ok) m_Status = "Load failed";
                    }
                });
            }
            if (GameShell.Settings.autosaveOnSceneChange)
                AddLabel("Leaving a scene also writes the autosave slot.", 48f);
            AddButton("Back", () => { LogClick("Back"); ShowPage(Page.Main); });
        }

        static void LogClick(string what)
        {
            Debug.Log("[GameShell] Pause click: " + what);
        }

        void EnsureEventSystem()
        {
            var existing = FindAnyObjectByType<EventSystem>();
            if (existing == null)
            {
                var go = new GameObject("EventSystem");
                go.transform.SetParent(transform, false);
                go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
                return;
            }

            var legacy = existing.GetComponent<StandaloneInputModule>();
            if (legacy != null) legacy.enabled = false;
            if (existing.GetComponent<InputSystemUIInputModule>() == null)
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        GameShellTheme ResolveTheme() => m_ThemeOverride != null ? m_ThemeOverride : GameShell.Settings.Theme;

        /// <summary>Re-colours and re-fonts the frame with <paramref name="theme"/> (null = the settings theme).</summary>
        public void ApplyTheme(GameShellTheme theme)
        {
            m_ThemeOverride = theme;
            if (!m_Built) return;
            m_Theme = ResolveTheme();
            m_Font = m_Theme.ResolveFont();
            if (m_Dimmer != null) m_Dimmer.color = m_Theme.dimmer;
            if (m_PanelImage != null) m_PanelImage.color = m_Theme.panel;
            foreach (var t in new[] { m_TitleText, m_SubtitleText, m_StatusText })
            {
                if (t == null) continue;
                t.font = m_Font;
                t.color = m_Theme.text;
            }
            if (m_VersionText != null)
            {
                m_VersionText.font = m_Font;
                m_VersionText.color = m_Theme.versionText;
            }
            RebuildPage();
        }

        void BuildUi()
        {
            if (m_Built) return;
            m_Built = true;
            var settings = GameShell.Settings;
            m_Theme = ResolveTheme();
            m_Font = m_Theme.ResolveFont();

            m_Root = new GameObject("PauseCanvas");
            m_Root.transform.SetParent(transform, false);
            m_Canvas = m_Root.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = settings.pauseSortingOrder;
            var scaler = m_Root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            m_Root.AddComponent<GraphicRaycaster>();

            var dimmerGo = new GameObject("Dimmer");
            dimmerGo.transform.SetParent(m_Root.transform, false);
            var dimmerRt = dimmerGo.AddComponent<RectTransform>();
            Stretch(dimmerRt);
            m_Dimmer = dimmerGo.AddComponent<Image>();
            m_Dimmer.color = m_Theme.dimmer;
            m_Dimmer.raycastTarget = true;

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(m_Root.transform, false);
            m_PanelRt = panelGo.AddComponent<RectTransform>();
            m_PanelRt.anchorMin = new Vector2(0.5f, 0.5f);
            m_PanelRt.anchorMax = new Vector2(0.5f, 0.5f);
            m_PanelRt.pivot = new Vector2(0.5f, 0.5f);
            m_PanelRt.anchoredPosition = Vector2.zero;
            m_PanelRt.sizeDelta = new Vector2(PanelWidth, 360f);
            m_PanelImage = panelGo.AddComponent<Image>();
            m_PanelImage.color = m_Theme.panel;
            m_PanelImage.raycastTarget = true;
            m_PanelLayout = panelGo.AddComponent<VerticalLayoutGroup>();
            m_PanelLayout.padding = new RectOffset(20, 20, 16, 16);
            m_PanelLayout.spacing = Spacing;
            m_PanelLayout.childAlignment = TextAnchor.UpperCenter;
            m_PanelLayout.childControlHeight = true;
            m_PanelLayout.childControlWidth = true;
            m_PanelLayout.childForceExpandHeight = false;
            m_PanelLayout.childForceExpandWidth = false;
            m_PanelFitter = panelGo.AddComponent<ContentSizeFitter>();
            m_PanelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            m_PanelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            m_TitleText = MakeText(panelGo.transform, settings.pauseTitle ?? "", TitleSize, FontStyle.Bold, TitleHeight);
            m_SubtitleText = MakeText(panelGo.transform, "", SmallSize, FontStyle.Normal, LineHeight);
            BuildScroll(panelGo.transform);
            m_StatusText = MakeText(panelGo.transform, "", SmallSize, FontStyle.Normal, LineHeight);

            m_Page = Page.Main;
            RebuildPage();
            if (settings.showVersionLabel) BuildVersionLabel();
        }

        void BuildVersionLabel()
        {
            var go = new GameObject("VersionLabel");
            go.transform.SetParent(transform, false);
            var overlay = go.AddComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            overlay.sortingOrder = GameShell.Settings.versionSortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var textGo = new GameObject("Version");
            textGo.transform.SetParent(go.transform, false);
            var rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(12f, 8f);
            rt.sizeDelta = new Vector2(640f, 48f);
            m_VersionText = textGo.AddComponent<Text>();
            m_VersionText.font = m_Font;
            m_VersionText.fontSize = 14;
            m_VersionText.fontStyle = FontStyle.Normal;
            m_VersionText.alignment = TextAnchor.LowerLeft;
            m_VersionText.color = m_Theme.versionText;
            m_VersionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            m_VersionText.verticalOverflow = VerticalWrapMode.Overflow;
            m_VersionText.raycastTarget = false;
            m_VersionText.text = GameShellVersion.Label;
        }

        internal void RefreshVersionLabel()
        {
            if (m_VersionText == null) return;
            string line = GameShellVersion.LabelLine();
            if (m_VersionText.text != line) m_VersionText.text = line;
        }

        void BuildScroll(Transform parent)
        {
            var hostGo = new GameObject("PageScroll");
            hostGo.transform.SetParent(parent, false);
            hostGo.AddComponent<RectTransform>();
            m_ScrollHostLe = hostGo.AddComponent<LayoutElement>();
            FitWidth(m_ScrollHostLe);
            m_ScrollHostLe.minHeight = ButtonHeight;
            m_ScrollHostLe.preferredHeight = ButtonHeight;
            m_ScrollHostLe.flexibleHeight = 0f;
            m_Scroll = hostGo.AddComponent<ScrollRect>();
            m_Scroll.horizontal = false;
            m_Scroll.vertical = false;
            m_Scroll.movementType = ScrollRect.MovementType.Clamped;
            m_Scroll.inertia = false;
            m_Scroll.scrollSensitivity = 24f;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(hostGo.transform, false);
            var viewportRt = viewportGo.AddComponent<RectTransform>();
            Stretch(viewportRt);
            m_ViewportMask = viewportGo.AddComponent<RectMask2D>();
            m_ViewportMask.enabled = false;
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = true;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportRt, false);
            m_Content = contentGo.AddComponent<RectTransform>();
            m_Content.anchorMin = new Vector2(0.5f, 1f);
            m_Content.anchorMax = new Vector2(0.5f, 1f);
            m_Content.pivot = new Vector2(0.5f, 1f);
            m_Content.anchoredPosition = Vector2.zero;
            m_Content.sizeDelta = new Vector2(InnerWidth(), 0f);
            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = Spacing;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = false;
            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            m_Scroll.viewport = viewportRt;
            m_Scroll.content = m_Content;

            var barGo = new GameObject("Scrollbar");
            barGo.transform.SetParent(hostGo.transform, false);
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(1f, 0f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(1f, 1f);
            barRt.sizeDelta = new Vector2(8f, 0f);
            barRt.anchoredPosition = Vector2.zero;
            var barBg = barGo.AddComponent<Image>();
            barBg.color = m_Theme.scrollbarBackground;
            var areaGo = new GameObject("Sliding Area");
            areaGo.transform.SetParent(barGo.transform, false);
            var areaRt = areaGo.AddComponent<RectTransform>();
            Stretch(areaRt);
            areaRt.offsetMin = new Vector2(1f, 1f);
            areaRt.offsetMax = new Vector2(-1f, -1f);
            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(areaGo.transform, false);
            var handleRt = handleGo.AddComponent<RectTransform>();
            Stretch(handleRt);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = m_Theme.accent;
            var bar = barGo.AddComponent<Scrollbar>();
            bar.handleRect = handleRt;
            bar.targetGraphic = handleImg;
            bar.direction = Scrollbar.Direction.BottomToTop;
            m_Scroll.verticalScrollbar = bar;
            m_Scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            m_Scroll.verticalScrollbarSpacing = 4f;
        }

        /// <summary>Panel grows with its content up to 90% of the screen height, then the body scrolls.</summary>
        internal void FitPanel()
        {
            if (m_PanelRt == null || m_Content == null || m_ScrollHostLe == null) return;
            bool snap = m_LayoutDirty;
            m_LayoutDirty = false;
            m_FitWidth = Screen.width;
            m_FitHeight = Screen.height;
            m_FitCanvasH = CanvasHeight();

            SetScrolling(false);
            float inner = PanelWidth - m_PanelLayout.padding.horizontal;
            m_Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(1f, inner));
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);
            float contentPref = Mathf.Max(ButtonHeight, LayoutUtility.GetPreferredHeight(m_Content));

            m_ScrollHostLe.flexibleHeight = 0f;
            m_ScrollHostLe.minHeight = contentPref;
            m_ScrollHostLe.preferredHeight = contentPref;
            m_PanelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            m_PanelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            m_PanelRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, PanelWidth);
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_PanelRt);

            float maxH = MaxPanelHeight();
            float prefH = LayoutUtility.GetPreferredHeight(m_PanelRt);
            if (prefH <= maxH + 1f)
            {
                if (snap) m_Scroll.verticalNormalizedPosition = 1f;
                return;
            }

            float chrome = Mathf.Max(0f, prefH - contentPref);
            float body = Mathf.Max(ButtonHeight, maxH - chrome);
            m_ScrollHostLe.minHeight = body;
            m_ScrollHostLe.preferredHeight = body;
            m_ScrollHostLe.flexibleHeight = 1f;
            m_PanelFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            m_PanelRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, PanelWidth);
            m_PanelRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxH);
            SetScrolling(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_PanelRt);
            if (snap) m_Scroll.verticalNormalizedPosition = 1f;
        }

        internal float MaxPanelHeight() => Mathf.Max(ButtonHeight * 2f, CanvasHeight() * MaxHeightFraction);

        void SetScrolling(bool scrolling)
        {
            m_Scroll.vertical = scrolling;
            if (m_ViewportMask != null) m_ViewportMask.enabled = scrolling;
        }

        internal float CanvasHeight()
        {
            var rootRt = m_Canvas != null ? m_Canvas.transform as RectTransform : null;
            float h = rootRt != null ? rootRt.rect.height : 0f;
            if (h > 1f) return h;
            float scale = m_Canvas != null && m_Canvas.scaleFactor > 0.01f ? m_Canvas.scaleFactor : 1f;
            return Screen.height / scale;
        }

        static bool ApplyLine(Text text, bool show, float height)
        {
            if (text == null) return false;
            bool changed = text.gameObject.activeSelf != show;
            if (changed) text.gameObject.SetActive(show);
            if (!show) return changed;
            var le = text.GetComponent<LayoutElement>();
            if (le == null) return changed;
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
            return changed;
        }

        Text MakeText(Transform parent, string value, int size, FontStyle style, float rowHeight)
        {
            var go = new GameObject(string.IsNullOrEmpty(value) ? "Label" : value);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = m_Font;
            text.text = value;
            text.fontStyle = style;
            text.color = m_Theme.text;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = rowHeight;
            le.preferredHeight = rowHeight;
            le.flexibleHeight = 0f;
            FitWidth(le);
            Track(text, size);
            if (string.IsNullOrEmpty(value)) go.SetActive(false);
            return text;
        }

        void AddLabel(string value)
        {
            AddLabel(value, LabelHeight);
        }

        void AddLabel(string value, float rowHeight)
        {
            var text = MakeText(m_Content, value, SmallSize, FontStyle.Normal, rowHeight);
            text.alignment = TextAnchor.MiddleLeft;
        }

        void AddButton(string caption, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(caption);
            go.transform.SetParent(m_Content, false);
            var image = go.AddComponent<Image>();
            image.color = m_Theme.buttonFace;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = m_Theme.buttonFace;
            colors.highlightedColor = m_Theme.accent;
            colors.pressedColor = m_Theme.buttonPressed;
            colors.selectedColor = m_Theme.accent;
            colors.disabledColor = m_Theme.buttonDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            button.onClick.AddListener(action);
            var le = go.AddComponent<LayoutElement>();
            FitWidth(le);
            le.minHeight = ButtonHeight;
            le.preferredHeight = ButtonHeight;
            le.flexibleHeight = 0f;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(le.preferredWidth, ButtonHeight);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            Stretch(textRt);
            var text = textGo.AddComponent<Text>();
            text.font = m_Font;
            text.text = caption;
            text.fontStyle = FontStyle.Bold;
            text.color = m_Theme.buttonText;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            Track(text, BodySize);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void QuitGame()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStatics()
        {
            Instance = null;
            IsOpen = false;
            Opened = null;
            Closed = null;
            s_Extra.Clear();
        }
    }
}
