#if AMBERSCREEN_DEVCONSOLE
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AmberScreen.DevConsole
{
    sealed class DevConsoleView
    {
        struct SelectableSlot
        {
            public string Section;
            public string Label;
            public Selectable Selectable;
        }

        readonly Transform m_Host;
        readonly List<DevConsoleConfirmWidget> m_Confirms = new List<DevConsoleConfirmWidget>();
        readonly List<DevConsoleReadoutWidget> m_Readouts = new List<DevConsoleReadoutWidget>();
        readonly List<DevConsoleDropdownWidget> m_Dropdowns = new List<DevConsoleDropdownWidget>();
        readonly List<DevConsoleButtonWidget> m_Buttons = new List<DevConsoleButtonWidget>();
        readonly List<DevConsoleToggleWidget> m_Toggles = new List<DevConsoleToggleWidget>();
        readonly List<SelectableSlot> m_Selectables = new List<SelectableSlot>();
        readonly Dictionary<string, bool> m_SectionExpanded = new Dictionary<string, bool>();

        Canvas m_Canvas;
        RectTransform m_Root;
        RectTransform m_Panel;
        RectTransform m_Content;
        RectTransform m_CollapsedHeader;
        Button m_CollapsedExpandButton;
        ScrollRect m_Scroll;
        ScrollRect m_HistoryScroll;
        RectTransform m_HistoryContent;
        TextMeshProUGUI m_Title;
        TextMeshProUGUI m_Status;
        TextMeshProUGUI m_History;
        Button m_CollapseButton;

        Canvas m_DevCanvas;
        Button m_DevButton;

        bool m_Collapsed;
        Rect m_LastSafeArea;
        GameObject m_LastSelected;
        float m_AvailRefreshAccum;

        public DevConsoleView(Transform host)
        {
            m_Host = host;
            BuildCanvas();
            BuildDevButtonCanvas();
        }

        public void Dispose()
        {
            if (m_Canvas != null)
                Object.Destroy(m_Canvas.gameObject);
            if (m_DevCanvas != null)
                Object.Destroy(m_DevCanvas.gameObject);
        }

        public void SetVisible(bool visible)
        {
            if (m_Root != null)
                m_Root.gameObject.SetActive(visible);
            if (!visible)
                SetCollapsed(false);
            UpdateDevButton();
        }

        public void SetCollapsed(bool collapsed)
        {
            if (m_Collapsed == collapsed)
            {
                ApplyLayout();
                return;
            }

            m_Collapsed = collapsed;
            DevConsole.SetCollapsed(collapsed);
            ApplyLayout();

            if (collapsed)
                SelectCollapsedHeader();
            else
                RestoreSelection(null, null);
        }

        public void Rebuild()
        {
            DevConsoleHost.ClearRegistryDirty();

            if (m_Content == null)
                return;

            string keepSection = null;
            string keepLabel = null;
            CaptureSelection(ref keepSection, ref keepLabel);

            for (int i = m_Content.childCount - 1; i >= 0; i--)
                Object.Destroy(m_Content.GetChild(i).gameObject);

            m_Confirms.Clear();
            m_Readouts.Clear();
            m_Dropdowns.Clear();
            m_Buttons.Clear();
            m_Toggles.Clear();
            m_Selectables.Clear();

            var settings = DevConsoleSettings.LoadOrDefault();
            var sections = DevConsoleRegistry.GetOrderedSections();

            for (int s = 0; s < sections.Count; s++)
            {
                var section = sections[s];
                if (!m_SectionExpanded.ContainsKey(section.Name))
                    m_SectionExpanded[section.Name] = true;

                BuildSection(section, settings);
            }

            RefreshTitle();
            RefreshNotes();
            ApplyLayout();
            RestoreSelection(keepSection, keepLabel);
        }

        public void RefreshTitle()
        {
            if (m_Title == null)
                return;
            string scene = SceneManager.GetActiveScene().name;
            m_Title.text = $"Dev Console  ·  {Application.version}  ·  {scene}";
        }

        public void RefreshNotes()
        {
            if (m_Status != null)
                m_Status.text = DevConsoleRegistry.Status ?? string.Empty;

            if (m_History == null)
                return;

            var notes = DevConsoleRegistry.Notes;
            if (notes.Count == 0)
            {
                m_History.text = string.Empty;
            }
            else
            {
                var sb = new System.Text.StringBuilder(notes.Count * 24);
                for (int i = notes.Count - 1; i >= 0; i--)
                {
                    if (sb.Length > 0)
                        sb.Append('\n');
                    sb.Append(notes[i]);
                }
                m_History.text = sb.ToString();
            }

            if (m_HistoryContent != null)
            {
                m_History.ForceMeshUpdate();
                float h = Mathf.Max(m_History.preferredHeight + 8f, 40f);
                m_HistoryContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            }
        }

        public bool TryCloseDropdown()
        {
            for (int i = 0; i < m_Dropdowns.Count; i++)
            {
                if (m_Dropdowns[i].TryCloseDropdown())
                    return true;
            }
            return false;
        }

        public void TickClosed()
        {
            UpdateDevButton();
        }

        public void TickOpen(float unscaledDeltaTime)
        {
            for (int i = 0; i < m_Confirms.Count; i++)
                m_Confirms[i].Tick(unscaledDeltaTime);
            for (int i = 0; i < m_Readouts.Count; i++)
                m_Readouts[i].Tick(unscaledDeltaTime);

            m_AvailRefreshAccum += unscaledDeltaTime;
            if (m_AvailRefreshAccum >= 0.25f)
            {
                m_AvailRefreshAccum = 0f;
                RefreshAvailabilityAndToggles();
            }

            var settings = DevConsoleSettings.LoadOrDefault();
            if (settings.layout == DevConsoleLayout.MobilePortrait)
            {
                Rect safe = Screen.safeArea;
                if (safe.x != m_LastSafeArea.x || safe.y != m_LastSafeArea.y ||
                    safe.width != m_LastSafeArea.width || safe.height != m_LastSafeArea.height)
                {
                    m_LastSafeArea = safe;
                    ApplyLayout();
                }
            }

            FollowSelectionScroll();
        }

        void RefreshAvailabilityAndToggles()
        {
            for (int i = 0; i < m_Buttons.Count; i++)
                m_Buttons[i].RefreshInteractable();
            for (int i = 0; i < m_Toggles.Count; i++)
                m_Toggles[i].RefreshValueOnly();
        }

        void CaptureSelection(ref string section, ref string label)
        {
            if (EventSystem.current == null)
                return;
            var go = EventSystem.current.currentSelectedGameObject;
            if (go == null)
                return;
            for (int i = 0; i < m_Selectables.Count; i++)
            {
                if (m_Selectables[i].Selectable != null &&
                    m_Selectables[i].Selectable.gameObject == go)
                {
                    section = m_Selectables[i].Section;
                    label = m_Selectables[i].Label;
                    return;
                }
            }
        }

        void RestoreSelection(string section, string label)
        {
            if (EventSystem.current == null)
                return;

            if (!string.IsNullOrEmpty(section) || !string.IsNullOrEmpty(label))
            {
                for (int i = 0; i < m_Selectables.Count; i++)
                {
                    if (m_Selectables[i].Section == section && m_Selectables[i].Label == label &&
                        m_Selectables[i].Selectable != null && m_Selectables[i].Selectable.IsInteractable())
                    {
                        EventSystem.current.SetSelectedGameObject(m_Selectables[i].Selectable.gameObject);
                        return;
                    }
                }
            }

            for (int i = 0; i < m_Selectables.Count; i++)
            {
                if (m_Selectables[i].Selectable != null && m_Selectables[i].Selectable.IsInteractable())
                {
                    EventSystem.current.SetSelectedGameObject(m_Selectables[i].Selectable.gameObject);
                    return;
                }
            }
        }

        void SelectCollapsedHeader()
        {
            if (EventSystem.current == null || m_CollapsedExpandButton == null)
                return;
            EventSystem.current.SetSelectedGameObject(m_CollapsedExpandButton.gameObject);
        }

        void FollowSelectionScroll()
        {
            if (EventSystem.current == null || m_Scroll == null || m_Content == null)
                return;

            var go = EventSystem.current.currentSelectedGameObject;
            if (go == m_LastSelected)
                return;
            m_LastSelected = go;
            if (go == null)
                return;

            var rt = go.transform as RectTransform;
            if (rt == null || !rt.IsChildOf(m_Content))
                return;

            Canvas.ForceUpdateCanvases();
            var viewport = m_Scroll.viewport;
            Bounds itemBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, rt);
            float viewH = viewport.rect.height;
            float contentH = m_Content.rect.height;
            if (contentH <= viewH)
                return;

            float yMin = itemBounds.min.y;
            float yMax = itemBounds.max.y;
            float viewMin = -viewH * 0.5f;
            float viewMax = viewH * 0.5f;

            float itemTopInView = yMax;
            float itemBotInView = yMin;

            if (itemTopInView > viewMax)
            {
                float delta = itemTopInView - viewMax;
                m_Content.anchoredPosition += new Vector2(0f, -delta);
            }
            else if (itemBotInView < viewMin)
            {
                float delta = viewMin - itemBotInView;
                m_Content.anchoredPosition += new Vector2(0f, delta);
            }
        }

        void BuildCanvas()
        {
            var settings = DevConsoleSettings.LoadOrDefault();
            DevConsoleUiUtil.ResolveFont(settings);

            var canvasGo = new GameObject("DevConsoleCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(m_Host, false);
            m_Canvas = canvasGo.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = settings.sortingOrder;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = settings.ReferenceResolution;
            scaler.matchWidthOrHeight = settings.layout == DevConsoleLayout.MobilePortrait ? 1f : 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            m_Root = canvasGo.GetComponent<RectTransform>();
            DevConsoleUiUtil.Stretch(m_Root);

            BuildPanel(settings);
            BuildCollapsedHeader(settings);
            ApplyLayout();
            SetVisible(false);
        }

        void BuildDevButtonCanvas()
        {
            var settings = DevConsoleSettings.LoadOrDefault();
            var go = new GameObject("DevConsoleDevButtonCanvas", typeof(RectTransform));
            go.transform.SetParent(m_Host, false);
            m_DevCanvas = go.AddComponent<Canvas>();
            m_DevCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_DevCanvas.sortingOrder = settings.sortingOrder + 1;
            go.AddComponent<GraphicRaycaster>();

            var root = go.GetComponent<RectTransform>();
            DevConsoleUiUtil.Stretch(root);

            float size = Mathf.Max(48f, DevConsoleUiUtil.MinTouch(settings, m_Canvas));
            var rt = DevConsoleUiUtil.CreateRect("DevButton", root);
            PlaceInSafeAreaTopLeft(rt, size);
            m_DevButton = DevConsoleUiUtil.AddButton(rt, DevConsoleUiUtil.Accent);
            var label = DevConsoleUiUtil.AddLabel(
                rt, "Label", "DEV", DevConsoleUiUtil.FontSize(settings, m_Canvas) * 0.8f,
                TextAlignmentOptions.Midline);
            label.color = Color.black;
            m_DevButton.onClick.AddListener(DevConsole.Toggle);
            UpdateDevButton();
        }

        static void PlaceInSafeAreaTopLeft(RectTransform rt, float size)
        {
            Rect safe = Screen.safeArea;
            float sx = safe.x / Screen.width;
            float sy = (safe.y + safe.height) / Screen.height;
            // Anchor to safe-area top-left via normalized offsets on full-screen parent.
            rt.anchorMin = new Vector2(sx, sy);
            rt.anchorMax = new Vector2(sx, sy);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(12f, -12f);
        }

        void UpdateDevButton()
        {
            if (m_DevButton == null || m_DevCanvas == null)
                return;
            var settings = DevConsoleSettings.LoadOrDefault();
            bool show = settings.showOnScreenDevButton && !DevConsole.IsOpen;
            m_DevCanvas.gameObject.SetActive(show);
            if (show)
            {
                float size = Mathf.Max(48f, DevConsoleUiUtil.MinTouch(settings, m_Canvas));
                PlaceInSafeAreaTopLeft(m_DevButton.GetComponent<RectTransform>(), size);
            }
        }

        void BuildPanel(DevConsoleSettings settings)
        {
            float font = DevConsoleUiUtil.FontSize(settings, m_Canvas);
            float touch = DevConsoleUiUtil.MinTouch(settings, m_Canvas);
            float pad = settings.layout == DevConsoleLayout.MobilePortrait
                ? 12f * DevConsoleUiUtil.DpToCanvas(m_Canvas)
                : 12f;

            m_Panel = DevConsoleUiUtil.CreateRect("Panel", m_Root);
            DevConsoleUiUtil.AddImage(m_Panel, DevConsoleUiUtil.PanelBg);
            var vlg = m_Panel.gameObject.AddComponent<VerticalLayoutGroup>();
            int pi = Mathf.RoundToInt(pad);
            vlg.padding = new RectOffset(pi, pi, pi, pi);
            vlg.spacing = settings.spacing;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = m_Panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = DevConsoleUiUtil.CreateRect("Header", m_Panel);
            var headerLe = header.gameObject.AddComponent<LayoutElement>();
            headerLe.minHeight = touch * 0.9f;
            headerLe.preferredHeight = touch * 0.9f;
            var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerRow.childAlignment = TextAnchor.MiddleCenter;
            headerRow.spacing = 8f;
            headerRow.childForceExpandWidth = false;
            headerRow.childForceExpandHeight = true;

            var titleRt = DevConsoleUiUtil.CreateRect("Title", header);
            var titleLe = titleRt.gameObject.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;
            m_Title = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
            var fontAsset = DevConsoleUiUtil.Font;
            if (fontAsset != null)
                m_Title.font = fontAsset;
            m_Title.fontSize = font;
            m_Title.color = DevConsoleUiUtil.Accent;
            m_Title.alignment = TextAlignmentOptions.MidlineLeft;
            m_Title.raycastTarget = false;

            m_CollapseButton = CreateTextButton(header, "Collapse", "-", font, 40f, () =>
            {
                if (DevConsoleSettings.LoadOrDefault().layout == DevConsoleLayout.DockRight)
                    SetCollapsed(true);
            });

            var scrollRt = DevConsoleUiUtil.CreateRect("Scroll", m_Panel);
            var scrollLe = scrollRt.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 120f;
            scrollLe.preferredHeight = 420f;
            m_Scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            DevConsoleUiUtil.AddImage(scrollRt, new Color(0, 0, 0, 0.15f));

            var viewport = DevConsoleUiUtil.CreateRect("Viewport", scrollRt);
            DevConsoleUiUtil.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = new Color(1, 1, 1, 0.01f);

            m_Content = DevConsoleUiUtil.CreateRect("Content", viewport);
            m_Content.anchorMin = new Vector2(0, 1);
            m_Content.anchorMax = new Vector2(1, 1);
            m_Content.pivot = new Vector2(0.5f, 1f);
            m_Content.sizeDelta = Vector2.zero;
            var contentV = m_Content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentV.spacing = settings.spacing;
            contentV.childAlignment = TextAnchor.UpperCenter;
            contentV.childControlWidth = true;
            contentV.childControlHeight = true;
            contentV.childForceExpandWidth = true;
            contentV.childForceExpandHeight = false;
            var contentFitter = m_Content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            m_Scroll.content = m_Content;
            m_Scroll.viewport = viewport;
            m_Scroll.horizontal = false;
            m_Scroll.vertical = true;
            m_Scroll.movementType = ScrollRect.MovementType.Clamped;

            var statusRt = DevConsoleUiUtil.CreateRect("Status", m_Panel);
            var statusLe = statusRt.gameObject.AddComponent<LayoutElement>();
            statusLe.minHeight = font + 6f;
            statusLe.preferredHeight = font + 6f;
            m_Status = statusRt.gameObject.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null)
                m_Status.font = fontAsset;
            m_Status.fontSize = font * 0.85f;
            m_Status.color = DevConsoleUiUtil.TextColor;
            m_Status.alignment = TextAlignmentOptions.MidlineLeft;
            m_Status.raycastTarget = false;

            var histScroll = DevConsoleUiUtil.CreateRect("HistoryScroll", m_Panel);
            var histLe = histScroll.gameObject.AddComponent<LayoutElement>();
            histLe.minHeight = 72f;
            histLe.preferredHeight = 96f;
            DevConsoleUiUtil.AddImage(histScroll, new Color(0, 0, 0, 0.25f));
            m_HistoryScroll = histScroll.gameObject.AddComponent<ScrollRect>();

            var histVp = DevConsoleUiUtil.CreateRect("Viewport", histScroll);
            DevConsoleUiUtil.Stretch(histVp);
            histVp.gameObject.AddComponent<RectMask2D>();

            m_HistoryContent = DevConsoleUiUtil.CreateRect("Content", histVp);
            m_HistoryContent.anchorMin = new Vector2(0, 1);
            m_HistoryContent.anchorMax = new Vector2(1, 1);
            m_HistoryContent.pivot = new Vector2(0.5f, 1f);
            m_HistoryContent.anchoredPosition = Vector2.zero;
            m_HistoryContent.sizeDelta = new Vector2(0, 40f);

            m_History = m_HistoryContent.gameObject.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null)
                m_History.font = fontAsset;
            m_History.fontSize = font * 0.75f;
            m_History.color = new Color(0.75f, 0.78f, 0.82f, 1f);
            m_History.alignment = TextAlignmentOptions.TopLeft;
            m_History.textWrappingMode = TextWrappingModes.Normal;
            m_History.overflowMode = TextOverflowModes.Overflow;
            m_History.raycastTarget = false;
            var histMargins = m_History.margin;
            histMargins.x = 6f;
            histMargins.z = 6f;
            histMargins.y = 4f;
            histMargins.w = 4f;
            m_History.margin = histMargins;

            m_HistoryScroll.content = m_HistoryContent;
            m_HistoryScroll.viewport = histVp;
            m_HistoryScroll.horizontal = false;
            m_HistoryScroll.vertical = true;
            m_HistoryScroll.movementType = ScrollRect.MovementType.Clamped;

            var closeRow = DevConsoleUiUtil.CreateRect("CloseRow", m_Panel);
            var closeLe = closeRow.gameObject.AddComponent<LayoutElement>();
            closeLe.minHeight = touch;
            closeLe.preferredHeight = touch;
            CreateTextButton(closeRow, "Close", "Close", font, 0f, DevConsole.Close);
        }

        void BuildCollapsedHeader(DevConsoleSettings settings)
        {
            float font = DevConsoleUiUtil.FontSize(settings, m_Canvas);
            m_CollapsedHeader = DevConsoleUiUtil.CreateRect("CollapsedHeader", m_Root);
            m_CollapsedHeader.anchorMin = new Vector2(1, 1);
            m_CollapsedHeader.anchorMax = new Vector2(1, 1);
            m_CollapsedHeader.pivot = new Vector2(1, 1);
            m_CollapsedHeader.sizeDelta = new Vector2(200f, 56f);
            m_CollapsedHeader.anchoredPosition = new Vector2(-8f, -8f);
            DevConsoleUiUtil.AddImage(m_CollapsedHeader, DevConsoleUiUtil.PanelBg);

            m_CollapsedExpandButton = DevConsoleUiUtil.AddButton(m_CollapsedHeader, DevConsoleUiUtil.PanelBg);
            DevConsoleUiUtil.AddLabel(
                m_CollapsedHeader, "Label", "Dev Console >", font * 0.9f,
                TextAlignmentOptions.Midline);
            m_CollapsedExpandButton.onClick.AddListener(() => SetCollapsed(false));
            m_CollapsedHeader.gameObject.SetActive(false);
        }

        void ApplyLayout()
        {
            var settings = DevConsoleSettings.LoadOrDefault();
            float scale = settings.ResolvedScaleFactor;

            if (m_Canvas != null)
            {
                m_Canvas.sortingOrder = settings.sortingOrder;
                var scaler = m_Canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.referenceResolution = settings.ReferenceResolution;
                    scaler.matchWidthOrHeight =
                        settings.layout == DevConsoleLayout.MobilePortrait ? 1f : 0.5f;
                }
            }

            bool dockCollapsed = settings.layout == DevConsoleLayout.DockRight && m_Collapsed;
            if (m_CollapsedHeader != null)
                m_CollapsedHeader.gameObject.SetActive(dockCollapsed && DevConsole.IsOpen);
            if (m_Panel != null)
                m_Panel.gameObject.SetActive(!dockCollapsed);

            if (m_CollapseButton != null)
                m_CollapseButton.gameObject.SetActive(settings.layout == DevConsoleLayout.DockRight);

            if (m_Panel == null)
                return;

            var fitter = m_Panel.GetComponent<ContentSizeFitter>();
            float dockW = settings.dockRightWidth * scale;
            float floatW = settings.floatingPanelWidth * scale;

            switch (settings.layout)
            {
                case DevConsoleLayout.DockRight:
                    m_Panel.anchorMin = new Vector2(1, 0);
                    m_Panel.anchorMax = new Vector2(1, 1);
                    m_Panel.pivot = new Vector2(1, 0.5f);
                    m_Panel.anchoredPosition = Vector2.zero;
                    m_Panel.offsetMin = new Vector2(-dockW, 0f);
                    m_Panel.offsetMax = Vector2.zero;
                    if (fitter != null)
                    {
                        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                    }
                    break;

                case DevConsoleLayout.MobilePortrait:
                    m_LastSafeArea = Screen.safeArea;
                    float sx = m_LastSafeArea.x / Screen.width;
                    float sy = m_LastSafeArea.y / Screen.height;
                    float sw = m_LastSafeArea.width / Screen.width;
                    float sh = m_LastSafeArea.height / Screen.height;
                    m_Panel.anchorMin = new Vector2(sx, sy);
                    m_Panel.anchorMax = new Vector2(sx + sw, sy + sh);
                    m_Panel.offsetMin = Vector2.zero;
                    m_Panel.offsetMax = Vector2.zero;
                    m_Panel.pivot = new Vector2(0.5f, 0.5f);
                    if (fitter != null)
                    {
                        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                    }
                    break;

                default:
                    m_Panel.anchorMin = new Vector2(0.5f, 0.5f);
                    m_Panel.anchorMax = new Vector2(0.5f, 0.5f);
                    m_Panel.pivot = new Vector2(0.5f, 0.5f);
                    m_Panel.anchoredPosition = Vector2.zero;
                    float maxH = Screen.height * 0.8f / Mathf.Max(m_Canvas.scaleFactor, 0.01f);
                    m_Panel.sizeDelta = new Vector2(floatW, 0f);
                    if (fitter != null)
                    {
                        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    }
                    var le = m_Panel.GetComponent<LayoutElement>();
                    if (le == null)
                        le = m_Panel.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = floatW;
                    le.minWidth = floatW;
                    // Cap via scroll preferred height inside panel.
                    if (m_Scroll != null)
                    {
                        var scrollLe = m_Scroll.GetComponent<LayoutElement>();
                        if (scrollLe != null)
                            scrollLe.preferredHeight = Mathf.Min(420f * scale, maxH * 0.7f);
                    }
                    break;
            }
        }

        void BuildSection(DevConsoleSection section, DevConsoleSettings settings)
        {
            float font = DevConsoleUiUtil.FontSize(settings, m_Canvas);
            bool expanded = m_SectionExpanded[section.Name];

            var header = DevConsoleUiUtil.CreateRect("Section_" + section.Name, m_Content);
            var headerLe = header.gameObject.AddComponent<LayoutElement>();
            float h = DevConsoleUiUtil.MinTouch(settings, m_Canvas) * 0.9f;
            headerLe.minHeight = h;
            headerLe.preferredHeight = h;
            var headerBtn = DevConsoleUiUtil.AddButton(header, new Color(0.16f, 0.18f, 0.22f, 1f));
            string arrow = expanded ? "v" : ">";
            string title = string.IsNullOrEmpty(section.Name) ? "(default)" : section.Name;
            DevConsoleUiUtil.AddLabel(header, "Label", $"{arrow} {title}", font,
                TextAlignmentOptions.MidlineLeft);
            string sectionName = section.Name;
            headerBtn.onClick.AddListener(() =>
            {
                m_SectionExpanded[sectionName] = !m_SectionExpanded[sectionName];
                Rebuild();
            });
            m_Selectables.Add(new SelectableSlot
            {
                Section = section.Name,
                Label = "__header__",
                Selectable = headerBtn
            });

            if (!expanded)
                return;

            int columns = settings.layout == DevConsoleLayout.MobilePortrait
                ? 1
                : Mathf.Max(1, settings.columns);
            Transform entryParent = m_Content;

            if (columns > 1)
            {
                var gridRt = DevConsoleUiUtil.CreateRect("Grid_" + section.Name, m_Content);
                var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
                float cellH = DevConsoleUiUtil.MinTouch(settings, m_Canvas);
                grid.cellSize = new Vector2(200f, cellH);
                grid.spacing = new Vector2(settings.spacing, settings.spacing);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = columns;
                grid.childAlignment = TextAnchor.UpperLeft;
                var gridLe = gridRt.gameObject.AddComponent<LayoutElement>();
                gridLe.flexibleWidth = 1f;
                entryParent = gridRt;
            }

            for (int i = 0; i < section.Entries.Count; i++)
            {
                var entry = section.Entries[i];
                if (!entry.Visible)
                    continue;

                switch (entry.Kind)
                {
                    case DevConsoleEntryKind.Button:
                    {
                        var w = new DevConsoleButtonWidget(entryParent, entry, settings, m_Canvas);
                        m_Buttons.Add(w);
                        m_Selectables.Add(new SelectableSlot
                        {
                            Section = entry.Section,
                            Label = entry.Label,
                            Selectable = w.Selectable
                        });
                        break;
                    }
                    case DevConsoleEntryKind.Toggle:
                    {
                        var w = new DevConsoleToggleWidget(entryParent, entry, settings, m_Canvas);
                        m_Toggles.Add(w);
                        m_Selectables.Add(new SelectableSlot
                        {
                            Section = entry.Section,
                            Label = entry.Label,
                            Selectable = w.Selectable
                        });
                        break;
                    }
                    case DevConsoleEntryKind.Dropdown:
                    {
                        var w = new DevConsoleDropdownWidget(entryParent, entry, settings, m_Canvas);
                        m_Dropdowns.Add(w);
                        m_Selectables.Add(new SelectableSlot
                        {
                            Section = entry.Section,
                            Label = entry.Label,
                            Selectable = w.Selectable
                        });
                        break;
                    }
                    case DevConsoleEntryKind.Confirm:
                    {
                        var w = new DevConsoleConfirmWidget(entryParent, entry, settings, m_Canvas);
                        m_Confirms.Add(w);
                        m_Selectables.Add(new SelectableSlot
                        {
                            Section = entry.Section,
                            Label = entry.Label,
                            Selectable = w.Selectable
                        });
                        break;
                    }
                    case DevConsoleEntryKind.Readout:
                    {
                        var w = new DevConsoleReadoutWidget(entryParent, entry, settings, m_Canvas);
                        m_Readouts.Add(w);
                        break;
                    }
                }
            }
        }

        static Button CreateTextButton(
            Transform parent,
            string name,
            string text,
            float font,
            float width,
            UnityEngine.Events.UnityAction onClick)
        {
            var rt = DevConsoleUiUtil.CreateRect(name, parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                le.minWidth = width;
                le.preferredWidth = width;
            }
            else
            {
                le.flexibleWidth = 1f;
            }

            var btn = DevConsoleUiUtil.AddButton(rt, DevConsoleUiUtil.ButtonBg);
            DevConsoleUiUtil.AddLabel(rt, "Label", text, font, TextAlignmentOptions.Midline);
            btn.onClick.AddListener(onClick);
            return btn;
        }
    }
}
#endif
