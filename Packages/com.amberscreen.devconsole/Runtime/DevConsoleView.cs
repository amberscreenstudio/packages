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
        readonly Transform m_Host;
        readonly List<DevConsoleConfirmWidget> m_Confirms = new List<DevConsoleConfirmWidget>();
        readonly List<DevConsoleReadoutWidget> m_Readouts = new List<DevConsoleReadoutWidget>();
        readonly List<DevConsoleDropdownWidget> m_Dropdowns = new List<DevConsoleDropdownWidget>();
        readonly List<Selectable> m_Selectables = new List<Selectable>();
        readonly Dictionary<string, bool> m_SectionExpanded = new Dictionary<string, bool>();

        Canvas m_Canvas;
        RectTransform m_Root;
        RectTransform m_Panel;
        RectTransform m_Content;
        RectTransform m_CollapsedHeader;
        TextMeshProUGUI m_Title;
        TextMeshProUGUI m_Status;
        TextMeshProUGUI m_History;
        Button m_CollapseButton;
        Button m_DevButton;
        bool m_Collapsed;

        public DevConsoleView(Transform host)
        {
            m_Host = host;
            BuildCanvas();
        }

        public void Dispose()
        {
            if (m_Canvas != null)
                Object.Destroy(m_Canvas.gameObject);
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
            m_Collapsed = collapsed;
            DevConsole.SetCollapsed(collapsed);
            ApplyLayout();
        }

        public void Rebuild()
        {
            if (m_Content == null)
                return;

            for (int i = m_Content.childCount - 1; i >= 0; i--)
                Object.Destroy(m_Content.GetChild(i).gameObject);

            m_Confirms.Clear();
            m_Readouts.Clear();
            m_Dropdowns.Clear();
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
                return;
            }

            // Build once while open — acceptable allocation when console is visible.
            var sb = new System.Text.StringBuilder(notes.Count * 24);
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                if (sb.Length > 0)
                    sb.Append('\n');
                sb.Append(notes[i]);
            }
            m_History.text = sb.ToString();
        }

        public void SelectFirst()
        {
            if (m_Selectables.Count == 0 || EventSystem.current == null)
                return;
            EventSystem.current.SetSelectedGameObject(m_Selectables[0].gameObject);
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

        public void TickOpen(float unscaledDeltaTime)
        {
            for (int i = 0; i < m_Confirms.Count; i++)
                m_Confirms[i].Tick(unscaledDeltaTime);
            for (int i = 0; i < m_Readouts.Count; i++)
                m_Readouts[i].Tick(unscaledDeltaTime);
        }

        void BuildCanvas()
        {
            var settings = DevConsoleSettings.LoadOrDefault();

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
            BuildDevButton(settings);
            ApplyLayout();
            SetVisible(false);
        }

        void BuildPanel(DevConsoleSettings settings)
        {
            float font = DevConsoleUiUtil.FontSize(settings);

            m_Panel = DevConsoleUiUtil.CreateRect("Panel", m_Root);
            DevConsoleUiUtil.AddImage(m_Panel, DevConsoleUiUtil.PanelBg);
            var vlg = m_Panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 10, 10);
            vlg.spacing = settings.spacing;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = m_Panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header row
            var header = DevConsoleUiUtil.CreateRect("Header", m_Panel);
            var headerLe = header.gameObject.AddComponent<LayoutElement>();
            headerLe.minHeight = 40f;
            headerLe.preferredHeight = 40f;
            var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerRow.childAlignment = TextAnchor.MiddleCenter;
            headerRow.spacing = 8f;
            headerRow.childForceExpandWidth = false;
            headerRow.childForceExpandHeight = true;

            var titleRt = DevConsoleUiUtil.CreateRect("Title", header);
            var titleLe = titleRt.gameObject.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;
            m_Title = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
            m_Title.font = DevConsoleUiUtil.Font;
            m_Title.fontSize = font;
            m_Title.color = DevConsoleUiUtil.Accent;
            m_Title.alignment = TextAlignmentOptions.MidlineLeft;
            m_Title.raycastTarget = false;

            m_CollapseButton = CreateTextButton(header, "Collapse", "▾", font, 40f, () =>
            {
                if (settings.layout == DevConsoleLayout.DockRight)
                    SetCollapsed(true);
            });

            // Scroll content
            var scrollRt = DevConsoleUiUtil.CreateRect("Scroll", m_Panel);
            var scrollLe = scrollRt.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 120f;
            scrollLe.preferredHeight = 420f;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
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

            scroll.content = m_Content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Status + history
            var statusRt = DevConsoleUiUtil.CreateRect("Status", m_Panel);
            var statusLe = statusRt.gameObject.AddComponent<LayoutElement>();
            statusLe.minHeight = 24f;
            statusLe.preferredHeight = 24f;
            m_Status = statusRt.gameObject.AddComponent<TextMeshProUGUI>();
            m_Status.font = DevConsoleUiUtil.Font;
            m_Status.fontSize = font * 0.85f;
            m_Status.color = DevConsoleUiUtil.TextColor;
            m_Status.alignment = TextAlignmentOptions.MidlineLeft;
            m_Status.raycastTarget = false;

            var histScroll = DevConsoleUiUtil.CreateRect("HistoryScroll", m_Panel);
            var histLe = histScroll.gameObject.AddComponent<LayoutElement>();
            histLe.minHeight = 72f;
            histLe.preferredHeight = 96f;
            DevConsoleUiUtil.AddImage(histScroll, new Color(0, 0, 0, 0.25f));
            var histSr = histScroll.gameObject.AddComponent<ScrollRect>();

            var histVp = DevConsoleUiUtil.CreateRect("Viewport", histScroll);
            DevConsoleUiUtil.Stretch(histVp);
            histVp.gameObject.AddComponent<RectMask2D>();

            var histContent = DevConsoleUiUtil.CreateRect("Content", histVp);
            DevConsoleUiUtil.Stretch(histContent);
            histContent.offsetMin = new Vector2(6, 4);
            histContent.offsetMax = new Vector2(-6, -4);
            m_History = histContent.gameObject.AddComponent<TextMeshProUGUI>();
            m_History.font = DevConsoleUiUtil.Font;
            m_History.fontSize = font * 0.75f;
            m_History.color = new Color(0.75f, 0.78f, 0.82f, 1f);
            m_History.alignment = TextAlignmentOptions.TopLeft;
            m_History.textWrappingMode = TextWrappingModes.Normal;
            m_History.raycastTarget = false;

            histSr.content = histContent;
            histSr.viewport = histVp;
            histSr.horizontal = false;
            histSr.vertical = true;

            var closeRow = DevConsoleUiUtil.CreateRect("CloseRow", m_Panel);
            var closeLe = closeRow.gameObject.AddComponent<LayoutElement>();
            closeLe.minHeight = DevConsoleUiUtil.MinTouch(settings);
            closeLe.preferredHeight = DevConsoleUiUtil.MinTouch(settings);
            CreateTextButton(closeRow, "Close", "Close", font, 0f, DevConsole.Close);
        }

        void BuildCollapsedHeader(DevConsoleSettings settings)
        {
            float font = DevConsoleUiUtil.FontSize(settings);
            m_CollapsedHeader = DevConsoleUiUtil.CreateRect("CollapsedHeader", m_Root);
            m_CollapsedHeader.anchorMin = new Vector2(1, 1);
            m_CollapsedHeader.anchorMax = new Vector2(1, 1);
            m_CollapsedHeader.pivot = new Vector2(1, 1);
            m_CollapsedHeader.sizeDelta = new Vector2(200f, 56f);
            m_CollapsedHeader.anchoredPosition = new Vector2(-8f, -8f);
            DevConsoleUiUtil.AddImage(m_CollapsedHeader, DevConsoleUiUtil.PanelBg);

            var btn = DevConsoleUiUtil.AddButton(m_CollapsedHeader, DevConsoleUiUtil.PanelBg);
            DevConsoleUiUtil.AddLabel(
                m_CollapsedHeader, "Label", "Dev Console ▸", font * 0.9f,
                TextAlignmentOptions.Midline);
            btn.onClick.AddListener(() => SetCollapsed(false));
            m_CollapsedHeader.gameObject.SetActive(false);
        }

        void BuildDevButton(DevConsoleSettings settings)
        {
            float size = Mathf.Max(48f, DevConsoleUiUtil.MinTouch(settings));
            var rt = DevConsoleUiUtil.CreateRect("DevButton", m_Root);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(12f, -12f);
            m_DevButton = DevConsoleUiUtil.AddButton(rt, DevConsoleUiUtil.Accent);
            var label = DevConsoleUiUtil.AddLabel(
                rt, "Label", "DEV", DevConsoleUiUtil.FontSize(settings) * 0.8f,
                TextAlignmentOptions.Midline);
            label.color = Color.black;
            m_DevButton.onClick.AddListener(DevConsole.Toggle);
            UpdateDevButton();
        }

        void UpdateDevButton()
        {
            if (m_DevButton == null)
                return;
            var settings = DevConsoleSettings.LoadOrDefault();
            m_DevButton.gameObject.SetActive(settings.showOnScreenDevButton && !DevConsole.IsOpen);
        }

        void ApplyLayout()
        {
            var settings = DevConsoleSettings.LoadOrDefault();
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

            switch (settings.layout)
            {
                case DevConsoleLayout.DockRight:
                    m_Panel.anchorMin = new Vector2(1, 0);
                    m_Panel.anchorMax = new Vector2(1, 1);
                    m_Panel.pivot = new Vector2(1, 0.5f);
                    m_Panel.anchoredPosition = Vector2.zero;
                    m_Panel.sizeDelta = new Vector2(320f, 0f);
                    m_Panel.offsetMin = new Vector2(-320f, 0f);
                    m_Panel.offsetMax = Vector2.zero;
                    if (fitter != null)
                    {
                        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                    }
                    break;

                case DevConsoleLayout.MobilePortrait:
                    var safe = Screen.safeArea;
                    float sx = safe.x / Screen.width;
                    float sy = safe.y / Screen.height;
                    float sw = safe.width / Screen.width;
                    float sh = safe.height / Screen.height;
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
                    m_Panel.sizeDelta = new Vector2(560f, 0f);
                    if (fitter != null)
                    {
                        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    }
                    break;
            }
        }

        void BuildSection(DevConsoleSection section, DevConsoleSettings settings)
        {
            float font = DevConsoleUiUtil.FontSize(settings);
            bool expanded = m_SectionExpanded[section.Name];

            var header = DevConsoleUiUtil.CreateRect("Section_" + section.Name, m_Content);
            var headerLe = header.gameObject.AddComponent<LayoutElement>();
            float h = DevConsoleUiUtil.MinTouch(settings) * 0.9f;
            headerLe.minHeight = h;
            headerLe.preferredHeight = h;
            var headerBtn = DevConsoleUiUtil.AddButton(header, new Color(0.16f, 0.18f, 0.22f, 1f));
            string arrow = expanded ? "▾" : "▸";
            string title = string.IsNullOrEmpty(section.Name) ? "(default)" : section.Name;
            DevConsoleUiUtil.AddLabel(header, "Label", $"{arrow} {title}", font,
                TextAlignmentOptions.MidlineLeft);
            string sectionName = section.Name;
            headerBtn.onClick.AddListener(() =>
            {
                m_SectionExpanded[sectionName] = !m_SectionExpanded[sectionName];
                Rebuild();
            });
            m_Selectables.Add(headerBtn);

            if (!expanded)
                return;

            int columns = Mathf.Max(1, settings.columns);
            Transform entryParent = m_Content;

            if (columns > 1)
            {
                var gridRt = DevConsoleUiUtil.CreateRect("Grid_" + section.Name, m_Content);
                var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
                float cellH = DevConsoleUiUtil.MinTouch(settings);
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
                        var w = new DevConsoleButtonWidget(entryParent, entry, settings);
                        m_Selectables.Add(w.Selectable);
                        break;
                    }
                    case DevConsoleEntryKind.Toggle:
                    {
                        var w = new DevConsoleToggleWidget(entryParent, entry, settings);
                        m_Selectables.Add(w.Selectable);
                        break;
                    }
                    case DevConsoleEntryKind.Dropdown:
                    {
                        var w = new DevConsoleDropdownWidget(entryParent, entry, settings);
                        m_Dropdowns.Add(w);
                        m_Selectables.Add(w.Selectable);
                        break;
                    }
                    case DevConsoleEntryKind.Confirm:
                    {
                        var w = new DevConsoleConfirmWidget(entryParent, entry, settings);
                        m_Confirms.Add(w);
                        m_Selectables.Add(w.Selectable);
                        break;
                    }
                    case DevConsoleEntryKind.Readout:
                    {
                        var w = new DevConsoleReadoutWidget(entryParent, entry, settings);
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
