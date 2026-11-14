using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if AMBERSCREEN_SETTINGS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace AmberScreen.Settings
{
    /// <summary>
    /// A plain uGUI settings menu built in code (no prefab, no TextMeshPro, no render-pipeline parts).
    /// Add it to an empty GameObject or call <see cref="Create"/>, then <see cref="Open"/>.
    /// Games with their own UI can skip this and call <see cref="GameSettings"/> directly.
    /// </summary>
    public sealed class SettingsMenu : MonoBehaviour
    {
        [Tooltip("Leave empty to use Unity's built-in LegacyRuntime font.")]
        public Font font;
        public int sortingOrder = 500;
        public bool startOpen;
        public bool closeWithEscape = true;
        public bool saveOnClose = true;
        [Tooltip("Free the mouse cursor while open and restore it on close.")]
        public bool unlockCursorWhileOpen = true;

        public UnityEvent onOpened = new UnityEvent();
        public UnityEvent onClosed = new UnityEvent();
        public event Action Opened;
        public event Action Closed;

        static readonly Color k_Panel = new Color(0.10f, 0.10f, 0.12f, 0.97f);
        static readonly Color k_Dim = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color k_Text = new Color(0.92f, 0.92f, 0.92f, 1f);
        static readonly Color k_Accent = new Color(0.95f, 0.68f, 0.25f, 1f);

        static readonly FullScreenMode[] k_Modes =
            { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
        static readonly string[] k_ModeNames = { "Fullscreen", "Borderless", "Windowed" };

        Canvas m_Canvas;
        GameObject m_FirstSelectable;
        GameObject m_OwnedEventSystem;
        readonly List<Action> m_Refreshers = new List<Action>();
        bool m_Built;
        CursorLockMode m_PrevLock;
        bool m_PrevVisible;

        public bool IsOpen => m_Canvas != null && m_Canvas.gameObject.activeSelf;

        /// <summary>Makes a new menu object (closed).</summary>
        public static SettingsMenu Create(Transform parent = null)
        {
            var go = new GameObject("SettingsMenu");
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<SettingsMenu>();
        }

        void Awake()
        {
            Build();
            m_Canvas.gameObject.SetActive(false);
        }

        void Start()
        {
            if (startOpen) Open();
        }

        void OnEnable() => GameSettings.Changed += OnSettingsChanged;
        void OnDisable() => GameSettings.Changed -= OnSettingsChanged;

        void OnDestroy()
        {
            if (m_OwnedEventSystem != null) Destroy(m_OwnedEventSystem);
        }

        void Update()
        {
            // Esc / gamepad B always close (checked every frame while open).
            if (IsOpen && closeWithEscape && SettingsMenuInput.BackPressedThisFrame()) Close();
        }

        public void Open()
        {
            if (!GameSettings.IsInitialized) GameSettings.Initialize();
            Build();
            EnsureEventSystem();
            m_Canvas.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            FitToCanvas();
            if (m_Scroll != null) m_Scroll.verticalNormalizedPosition = 1f;
            Refresh();
            if (unlockCursorWhileOpen)
            {
                m_PrevLock = Cursor.lockState;
                m_PrevVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (EventSystem.current != null && m_FirstSelectable != null)
                EventSystem.current.SetSelectedGameObject(m_FirstSelectable);
            Opened?.Invoke();
            onOpened.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;
            m_Canvas.gameObject.SetActive(false);
            if (saveOnClose && GameSettings.IsInitialized) GameSettings.Save();
            if (unlockCursorWhileOpen)
            {
                Cursor.lockState = m_PrevLock;
                Cursor.visible = m_PrevVisible;
            }
            Closed?.Invoke();
            onClosed.Invoke();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Refresh()
        {
            if (!GameSettings.IsInitialized) return;
            foreach (var r in m_Refreshers) r();
        }

        void OnSettingsChanged(GameSettingsData _)
        {
            if (IsOpen) Refresh();
        }

        // ---------------- build ----------------

        /// <summary>Reference resolution of the menu canvas (CanvasScaler, Scale With Screen Size, match 0.5).</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        /// <summary>Panel size at the reference resolution; shrunk to fit smaller canvases.</summary>
        public static readonly Vector2 ReferencePanelSize = new Vector2(920f, 990f);
        public const float ScreenMargin = 24f;
        public const float HeaderHeight = 80f;
        public const float FooterHeight = 76f;
        const int k_TitleSize = 40;
        const int k_HeaderSize = 28;
        const int k_RowSize = 26;
        const float k_RowHeight = 48f;

        RectTransform m_CanvasRect;
        RectTransform m_Panel;
        ScrollRect m_Scroll;
        Button m_CloseButton;
        Vector2 m_LastCanvasSize;

        internal RectTransform CanvasRect => m_CanvasRect;
        internal RectTransform PanelRect => m_Panel;
        internal Button CloseButton => m_CloseButton;
        internal ScrollRect Scroll => m_Scroll;
        internal Canvas MenuCanvas => m_Canvas;

        /// <summary>Panel size for a canvas of this size: the reference size, shrunk to fit with a margin.</summary>
        public static Vector2 PanelSizeFor(Vector2 canvasSize)
        {
            float w = Mathf.Min(ReferencePanelSize.x, canvasSize.x - 2f * ScreenMargin);
            float h = Mathf.Min(ReferencePanelSize.y, canvasSize.y - 2f * ScreenMargin);
            return new Vector2(Mathf.Max(0f, w), Mathf.Max(0f, h));
        }

        /// <summary>Canvas size the CanvasScaler produces for a screen (Scale With Screen Size, match 0.5).</summary>
        public static Vector2 ScaledCanvasSize(Vector2 screen)
        {
            float logW = Mathf.Log(screen.x / ReferenceResolution.x, 2f);
            float logH = Mathf.Log(screen.y / ReferenceResolution.y, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(logW, logH, 0.5f));
            return screen / scale;
        }

        /// <summary>Resizes the panel to fit the canvas. Runs on open and whenever the screen size changes.</summary>
        public void FitToCanvas()
        {
            if (m_Panel == null || m_CanvasRect == null) return;
            Vector2 size = m_CanvasRect.rect.size;
            m_LastCanvasSize = size;
            m_Panel.sizeDelta = PanelSizeFor(size);
            m_Panel.anchoredPosition = Vector2.zero;
        }

        void LateUpdate()
        {
            if (!IsOpen || m_CanvasRect == null) return;
            if (m_CanvasRect.rect.size != m_LastCanvasSize) FitToCanvas();
            KeepSelectionVisible();
        }

        /// <summary>Builds the UI now (normally done in Awake). Safe to call more than once.</summary>
        public void Build()
        {
            if (m_Built) return;
            m_Built = true;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("SettingsCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            m_CanvasRect = (RectTransform)canvasGo.transform;
            m_Canvas = canvasGo.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var dim = NewUi("Dim", canvasGo.transform);
            Stretch(dim);
            dim.gameObject.AddComponent<Image>().color = k_Dim;

            // Panel: fixed reference size at the centre, clamped to the canvas in FitToCanvas.
            m_Panel = NewUi("Panel", canvasGo.transform);
            m_Panel.anchorMin = m_Panel.anchorMax = m_Panel.pivot = new Vector2(0.5f, 0.5f);
            m_Panel.sizeDelta = ReferencePanelSize;
            m_Panel.gameObject.AddComponent<Image>().color = k_Panel;
            m_Panel.gameObject.AddComponent<RectMask2D>();

            // Header (always visible): title + Close.
            var header = NewUi("Header", m_Panel);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, HeaderHeight);
            header.anchoredPosition = Vector2.zero;
            header.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.19f, 1f);

            var title = NewUi("Title", header);
            title.anchorMin = Vector2.zero;
            title.anchorMax = Vector2.one;
            title.offsetMin = new Vector2(28f, 0f);
            title.offsetMax = new Vector2(-220f, 0f);
            var titleText = title.gameObject.AddComponent<Text>();
            SetText(titleText, "Settings", k_TitleSize, k_Text, TextAnchor.MiddleLeft);

            m_CloseButton = AddButton(header, "Close (Esc)", Close, 0f, 0f, false);
            var closeRect = (RectTransform)m_CloseButton.transform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(190f, 56f);
            closeRect.anchoredPosition = new Vector2(-16f, 0f);

            // Footer (always visible): reset.
            var footer = NewUi("Footer", m_Panel);
            footer.anchorMin = new Vector2(0f, 0f);
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(0.5f, 0f);
            footer.sizeDelta = new Vector2(0f, FooterHeight);
            footer.anchoredPosition = Vector2.zero;
            var reset = AddButton(footer, "Reset to defaults", () => GameSettings.ResetToDefaults(), 0f, 0f, false);
            var resetRect = (RectTransform)reset.transform;
            resetRect.anchorMin = resetRect.anchorMax = resetRect.pivot = new Vector2(0.5f, 0.5f);
            resetRect.sizeDelta = new Vector2(300f, 52f);
            resetRect.anchoredPosition = Vector2.zero;

            // Body: scrolls when the panel is shorter than the options.
            var body = NewUi("Body", m_Panel);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(0f, FooterHeight);
            body.offsetMax = new Vector2(0f, -HeaderHeight);
            m_Scroll = body.gameObject.AddComponent<ScrollRect>();
            m_Scroll.horizontal = false;
            m_Scroll.vertical = true;
            m_Scroll.movementType = ScrollRect.MovementType.Clamped;
            m_Scroll.scrollSensitivity = 40f;

            var viewport = NewUi("Viewport", body);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // catches wheel/drag

            var content = NewUi("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 12, 16);
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bar = DefaultControls.CreateScrollbar(new DefaultControls.Resources());
            bar.name = "Scrollbar";
            var barRect = (RectTransform)bar.transform;
            barRect.SetParent(body, false);
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 0.5f);
            barRect.sizeDelta = new Vector2(16f, 0f);
            barRect.anchoredPosition = Vector2.zero;
            var scrollbar = bar.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            foreach (var img in bar.GetComponentsInChildren<Image>(true))
                img.color = img.gameObject == bar ? new Color(0.16f, 0.16f, 0.19f, 1f) : new Color(0.45f, 0.45f, 0.5f, 1f);
            var barNav = scrollbar.navigation;
            barNav.mode = Navigation.Mode.None;
            scrollbar.navigation = barNav;

            m_Scroll.viewport = viewport;
            m_Scroll.content = content;
            m_Scroll.verticalScrollbar = scrollbar;
            m_Scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            m_Scroll.verticalScrollbarSpacing = 4f;

            BuildOptions(content);
            FitToCanvas();
        }

        void BuildOptions(RectTransform panel)
        {
            AddHeader(panel, "Graphics");
            AddStepper(panel, "Quality",
                () => SafeName(GameSettings.QualityNames, GameSettings.Current.qualityLevel),
                dir =>
                {
                    int count = GameSettings.QualityNames.Length;
                    if (count == 0) return;
                    GameSettings.ApplyPreset(Wrap(GameSettings.Current.qualityLevel + dir, count));
                });
            AddStepper(panel, "Resolution",
                () =>
                {
                    var r = CurrentResolution();
                    return $"{r.x} x {r.y}";
                },
                dir =>
                {
                    var options = GameSettings.GetResolutionOptions();
                    int index = Wrap(IndexOf(options, CurrentResolution()) + dir, options.Length);
                    GameSettings.SetResolution(options[index].x, options[index].y);
                });
            AddStepper(panel, "Display mode",
                () => k_ModeNames[Mathf.Max(0, Array.IndexOf(k_Modes, GameSettings.Current.fullScreenMode))],
                dir =>
                {
                    int i = Mathf.Max(0, Array.IndexOf(k_Modes, GameSettings.Current.fullScreenMode));
                    GameSettings.SetFullScreenMode(k_Modes[Wrap(i + dir, k_Modes.Length)]);
                });
            AddStepper(panel, "VSync",
                () => GameSettings.Current.vSync ? "On" : "Off",
                _ => GameSettings.SetVSync(!GameSettings.Current.vSync));
            AddStepper(panel, "Frame limit",
                () => FpsLabel(GameSettings.Current.targetFrameRate),
                dir =>
                {
                    var options = GameSettings.FrameRateOptions;
                    int i = SettingsMath.ClosestIndex(options, GameSettings.Current.targetFrameRate);
                    GameSettings.SetTargetFrameRate(options[Wrap(i + dir, options.Length)]);
                });

            AddHeader(panel, "Audio");
            AddVolume(panel, "Master volume", VolumeChannel.Master);
            AddVolume(panel, "Music volume", VolumeChannel.Music);
            AddVolume(panel, "Effects volume", VolumeChannel.Sfx);

            AddHeader(panel, "Controls");
            var profile = GameSettings.Profile;
            float min = profile != null ? profile.minMouseSensitivity : 0.1f;
            float max = profile != null ? profile.maxMouseSensitivity : 5f;
            AddSlider(panel, "Mouse sensitivity", min, max,
                () => GameSettings.Current.mouseSensitivity,
                v => GameSettings.SetMouseSensitivity(v),
                v => v.ToString("0.00"));
            AddStepper(panel, "Invert Y",
                () => GameSettings.Current.invertY ? "On" : "Off",
                _ => GameSettings.SetInvertY(!GameSettings.Current.invertY));
        }

        /// <summary>Scrolls so a gamepad/keyboard-selected option inside the list stays in view.</summary>
        void KeepSelectionVisible()
        {
            var es = EventSystem.current;
            if (es == null || m_Scroll == null || es.currentSelectedGameObject == null) return;
            var selected = es.currentSelectedGameObject.transform as RectTransform;
            if (selected == null || !selected.IsChildOf(m_Scroll.content)) return;
            var viewport = m_Scroll.viewport;
            var content = m_Scroll.content;
            float overflow = content.rect.height - viewport.rect.height;
            if (overflow <= 0f) return;

            var corners = new Vector3[4];
            selected.GetWorldCorners(corners);
            Vector3 bottom = viewport.InverseTransformPoint(corners[0]);
            Vector3 top = viewport.InverseTransformPoint(corners[1]);
            Rect view = viewport.rect;
            float shift = 0f;
            if (top.y > view.yMax) shift = top.y - view.yMax;
            else if (bottom.y < view.yMin) shift = bottom.y - view.yMin;
            if (Mathf.Approximately(shift, 0f)) return;
            // Content moves down (y up) to reveal lower items.
            var pos = content.anchoredPosition;
            pos.y = Mathf.Clamp(pos.y - shift, 0f, overflow);
            content.anchoredPosition = pos;
        }

        static Vector2Int CurrentResolution()
        {
            var d = GameSettings.Current;
            if (d.resolutionWidth > 0 && d.resolutionHeight > 0) return new Vector2Int(d.resolutionWidth, d.resolutionHeight);
            return new Vector2Int(Screen.width, Screen.height);
        }

        static int IndexOf(Vector2Int[] options, Vector2Int value)
        {
            for (int i = 0; i < options.Length; i++)
                if (options[i] == value) return i;
            return 0;
        }

        static string FpsLabel(int fps) => fps <= 0 ? "Unlimited" : fps + " FPS";

        static string SafeName(string[] names, int index) =>
            names.Length == 0 ? "-" : names[Mathf.Clamp(index, 0, names.Length - 1)];

        static int Wrap(int i, int count) => count <= 0 ? 0 : ((i % count) + count) % count;

        void AddHeader(RectTransform parent, string text) =>
            AddLabel(parent, text, k_HeaderSize, k_Accent, 46f, TextAnchor.LowerLeft);

        Text AddLabel(Transform parent, string text, int size, Color color, float height, TextAnchor anchor)
        {
            var rt = NewUi(text, parent);
            var t = rt.gameObject.AddComponent<Text>();
            SetText(t, text, size, color, anchor);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            return t;
        }

        void SetText(Text t, string text, int size, Color color, TextAnchor anchor)
        {
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
        }

        RectTransform Row(Transform parent, string name, float height = k_RowHeight)
        {
            var row = NewUi(name, parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        RectTransform RowWithLabel(Transform parent, string label)
        {
            var row = Row(parent, label);
            var t = AddLabel(row, label, k_RowSize, k_Text, k_RowHeight, TextAnchor.MiddleLeft);
            t.GetComponent<LayoutElement>().preferredWidth = 330f;
            return row;
        }

        void AddStepper(Transform parent, string label, Func<string> read, Action<int> step)
        {
            var row = RowWithLabel(parent, label);
            var left = AddButton(row, "<", () => { step(-1); Refresh(); }, 0f, 56f);
            var value = AddLabel(row, "", k_RowSize, k_Text, k_RowHeight, TextAnchor.MiddleCenter);
            value.GetComponent<LayoutElement>().flexibleWidth = 1f;
            AddButton(row, ">", () => { step(1); Refresh(); }, 0f, 56f);
            if (m_FirstSelectable == null) m_FirstSelectable = left.gameObject;
            m_Refreshers.Add(() => value.text = read());
        }

        void AddVolume(Transform parent, string label, VolumeChannel channel) =>
            AddSlider(parent, label, 0f, 1f,
                () => GameSettings.GetVolume(channel),
                v => GameSettings.SetVolume(channel, v),
                v => Mathf.RoundToInt(v * 100f) + "%");

        void AddSlider(Transform parent, string label, float min, float max, Func<float> read, Action<float> write,
            Func<float, string> format)
        {
            var row = RowWithLabel(parent, label);
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.transform.SetParent(row, false);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = 24f;
            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            StyleSelectable(slider);
            foreach (var img in go.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject.name == "Background") img.color = new Color(0.22f, 0.22f, 0.25f, 1f);
                else if (img.gameObject.name == "Fill") img.color = k_Accent;
                else img.color = Color.white;
            }

            var value = AddLabel(row, "", k_RowSize, k_Text, k_RowHeight, TextAnchor.MiddleRight);
            value.GetComponent<LayoutElement>().preferredWidth = 90f;
            slider.onValueChanged.AddListener(v =>
            {
                write(v);
                value.text = format(read());
            });
            m_Refreshers.Add(() =>
            {
                slider.SetValueWithoutNotify(read());
                value.text = format(read());
            });
        }

        Button AddButton(Transform parent, string text, UnityAction onClick, float flexibleWidth, float width = 0f,
            bool inLayout = true)
        {
            var rt = NewUi(text, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            StyleSelectable(button);
            button.onClick.AddListener(onClick);
            if (inLayout)
            {
                var le = rt.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = flexibleWidth;
                if (width > 0f) le.preferredWidth = width;
            }

            var label = NewUi("Text", rt);
            Stretch(label);
            SetText(label.gameObject.AddComponent<Text>(), text, k_RowSize, k_Text, TextAnchor.MiddleCenter);
            return button;
        }

        static void StyleSelectable(Selectable s)
        {
            var c = s.colors;
            c.normalColor = new Color(0.24f, 0.24f, 0.27f, 1f);
            c.highlightedColor = new Color(0.36f, 0.36f, 0.40f, 1f);
            c.selectedColor = new Color(0.48f, 0.40f, 0.26f, 1f);
            c.pressedColor = k_Accent;
            c.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            c.colorMultiplier = 1f;
            c.fadeDuration = 0.06f;
            s.colors = c;
        }

        static RectTransform NewUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            m_OwnedEventSystem = new GameObject("SettingsMenuEventSystem");
            m_OwnedEventSystem.transform.SetParent(transform, false);
            m_OwnedEventSystem.AddComponent<EventSystem>();
#if AMBERSCREEN_SETTINGS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            m_OwnedEventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            m_OwnedEventSystem.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
