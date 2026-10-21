#if AMBERSCREEN_DEVCONSOLE
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    sealed partial class DevConsoleView
    {
        RectTransform m_Rail;
        RectTransform m_RailStrip;
        RectTransform m_RailStripIcons;
        Button m_RailStripExpand;
        TextMeshProUGUI m_RailStripExpandLabel;
        readonly List<Button> m_StripSectionButtons = new List<Button>();
        bool m_StripPortraitLayout;
        RectTransform m_Blocker;
        RectTransform m_SectionWindow;
        RectTransform m_QuickRoot;
        RectTransform m_PinRoot;
        RectTransform m_SectionListContent;
        ScrollRect m_SectionListScroll;
        TextMeshProUGUI m_RailStatus;
        Button m_RailStatusButton;
        TMP_InputField m_FilterField;
        TextMeshProUGUI m_WindowTitle;
        RectTransform m_WindowContent;
        ScrollRect m_WindowScroll;
        Button m_WindowCloseButton;
        TextMeshProUGUI m_WindowCloseLabel;
        readonly List<Button> m_SectionButtons = new List<Button>();
        readonly List<DevConsoleButtonWidget> m_QuickButtons = new List<DevConsoleButtonWidget>();
        readonly List<TextMeshProUGUI> m_PinLabels = new List<TextMeshProUGUI>();
        readonly List<DevConsoleEntry> m_PinEntries = new List<DevConsoleEntry>();
        readonly List<float> m_PinAccum = new List<float>();
        string m_FilterText = string.Empty;
        bool m_LogWindowOpen;
        float m_RailWidth;
        TMP_InputField m_CommandField;
        RectTransform m_CommandRow;
        bool m_CommandLineBuilt;
        /// <summary>True when the section window body was built before the viewport had a real width.</summary>
        bool m_PendingFlowRebuild;

        /// <summary>Edit-mode tests set these so layout does not depend on the Game view size.</summary>
        internal static int s_LayoutScreenWidth;
        internal static int s_LayoutScreenHeight;
        /// <summary>Edit-mode dpi override; 0 means use <see cref="Screen.dpi"/>.</summary>
        internal static float s_LayoutDpi;

        internal static int LayoutScreenWidth =>
            s_LayoutScreenWidth > 0 ? s_LayoutScreenWidth : Screen.width;

        internal static int LayoutScreenHeight =>
            s_LayoutScreenHeight > 0 ? s_LayoutScreenHeight : Screen.height;

        internal static float LayoutDpi =>
            s_LayoutDpi > 0f ? s_LayoutDpi : Screen.dpi;

        static Rect LayoutSafeArea =>
            s_LayoutScreenWidth > 0
                ? new Rect(0f, 0f, s_LayoutScreenWidth, s_LayoutScreenHeight)
                : Screen.safeArea;

        internal static void SetLayoutScreenOverride(int width, int height, float dpi = 0f)
        {
            s_LayoutScreenWidth = width;
            s_LayoutScreenHeight = height;
            s_LayoutDpi = dpi;
        }

        internal static void SetLayoutDpiOverride(float dpi) => s_LayoutDpi = dpi;

        internal static void ClearLayoutScreenOverride()
        {
            s_LayoutScreenWidth = 0;
            s_LayoutScreenHeight = 0;
            s_LayoutDpi = 0f;
        }

        void BuildRailUi(DevConsoleSettings settings)
        {
            // Draw order (bottom → top): Blocker, Rail, RailStrip, SectionWindow.
            m_Blocker = DevConsoleUiUtil.CreateRect("Blocker", m_Root);
            DevConsoleUiUtil.Stretch(m_Blocker);
            var blockerImg = DevConsoleUiUtil.AddImage(m_Blocker, new Color(0, 0, 0, 0f));
            blockerImg.raycastTarget = true;
            var blockerBtn = m_Blocker.gameObject.AddComponent<Button>();
            blockerBtn.transition = Selectable.Transition.None;
            blockerBtn.onClick.AddListener(() => DevConsole.CloseSection());
            m_Blocker.gameObject.SetActive(false);

            var theme = DevConsoleHost.ActiveTheme;
            m_Rail = DevConsoleUiUtil.CreateRect("Rail", m_Root);
            var railImg = DevConsoleUiUtil.AddImage(m_Rail, theme.railBackground.color);
            DevConsoleTheme.ApplySurface(railImg, theme.railBackground);
            m_Panel = m_Rail;
            m_PanelVlg = m_Rail.gameObject.AddComponent<VerticalLayoutGroup>();
            int pad = Mathf.RoundToInt(theme.padding);
            m_PanelVlg.padding = new RectOffset(pad, pad, pad, pad);
            m_PanelVlg.spacing = theme.spacing > 0f ? theme.spacing : settings.spacing;
            m_PanelVlg.childControlWidth = true;
            m_PanelVlg.childControlHeight = true;
            m_PanelVlg.childForceExpandWidth = true;
            m_PanelVlg.childForceExpandHeight = false;
            m_PanelVlg.childAlignment = TextAnchor.UpperCenter;

            BuildRailTitleBar(settings);

            m_QuickRoot = DevConsoleUiUtil.CreateRect("Quicks", m_Rail);
            var quickLe = m_QuickRoot.gameObject.AddComponent<LayoutElement>();
            quickLe.minHeight = 0f;
            m_QuickRoot.gameObject.AddComponent<GridLayoutGroup>();

            m_PinRoot = DevConsoleUiUtil.CreateRect("Pins", m_Rail);
            var pinV = m_PinRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            pinV.spacing = 2f;
            pinV.childControlHeight = true;
            pinV.childForceExpandHeight = false;
            m_PinRoot.gameObject.AddComponent<LayoutElement>().minHeight = 0f;

            var sectionScrollRt = DevConsoleUiUtil.CreateRect("SectionScroll", m_Rail);
            m_SectionListScroll = sectionScrollRt.gameObject.AddComponent<ScrollRect>();
            var sectionLe = sectionScrollRt.gameObject.AddComponent<LayoutElement>();
            sectionLe.flexibleHeight = 1f;
            sectionLe.minHeight = 80f;
            DevConsoleUiUtil.AddImage(sectionScrollRt, theme.scrollWell);
            var secVp = DevConsoleUiUtil.CreateRect("Viewport", sectionScrollRt);
            DevConsoleUiUtil.Stretch(secVp);
            secVp.gameObject.AddComponent<RectMask2D>();
            m_SectionListContent = DevConsoleUiUtil.CreateRect("Content", secVp);
            m_SectionListContent.anchorMin = new Vector2(0, 1);
            m_SectionListContent.anchorMax = new Vector2(1, 1);
            m_SectionListContent.pivot = new Vector2(0.5f, 1f);
            DevConsoleUiUtil.FitTopStretchContent(m_SectionListContent);
            var secV = m_SectionListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            secV.spacing = settings.spacing;
            secV.childControlWidth = true;
            secV.childControlHeight = true;
            secV.childForceExpandWidth = true;
            secV.childForceExpandHeight = false;
            m_SectionListContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            m_SectionListScroll.content = m_SectionListContent;
            m_SectionListScroll.viewport = secVp;
            m_SectionListScroll.horizontal = false;
            m_SectionListScroll.vertical = true;

            var statusRt = DevConsoleUiUtil.CreateRect("RailStatus", m_Rail);
            var statusLe = statusRt.gameObject.AddComponent<LayoutElement>();
            statusLe.minHeight = 22f;
            statusLe.preferredHeight = 22f;
            m_RailStatusButton = DevConsoleUiUtil.AddButton(statusRt, DevConsoleHost.ActiveTheme.statusWell);
            m_RailStatus = DevConsoleUiUtil.AddLabel(
                statusRt, "Label", string.Empty, DevConsoleHost.ActiveTheme.bodyFontSize * 0.85f,
                TextAlignmentOptions.MidlineLeft);
            m_RailStatusButton.onClick.AddListener(OpenLogWindow);

            BuildRailStrip(settings);

            m_SectionWindow = DevConsoleUiUtil.CreateRect("SectionWindow", m_Root);
            var winImg = DevConsoleUiUtil.AddImage(m_SectionWindow, DevConsoleUiUtil.WindowBg);
            DevConsoleTheme.ApplySurface(winImg, DevConsoleHost.ActiveTheme.windowBackground);
            var winV = m_SectionWindow.gameObject.AddComponent<VerticalLayoutGroup>();
            int winPad = Mathf.RoundToInt(DevConsoleHost.ActiveTheme.padding + 2f);
            winV.padding = new RectOffset(winPad, winPad, winPad, winPad);
            winV.spacing = DevConsoleHost.ActiveTheme.spacing > 0f
                ? DevConsoleHost.ActiveTheme.spacing
                : settings.spacing;
            winV.childControlWidth = true;
            winV.childControlHeight = true;
            winV.childForceExpandWidth = true;
            winV.childForceExpandHeight = false;
            BuildSectionWindowChrome(settings);
            m_SectionWindow.gameObject.SetActive(false);

            ApplyRailDrawOrder();
            SyncCommandLineVisibility();
        }

        void ApplyRailDrawOrder()
        {
            if (m_Blocker != null)
                m_Blocker.SetAsFirstSibling();
            if (m_Rail != null)
                m_Rail.SetSiblingIndex(m_Blocker != null ? 1 : 0);
            if (m_RailStrip != null)
                m_RailStrip.SetSiblingIndex(m_Rail != null ? m_Rail.GetSiblingIndex() + 1 : 1);
            if (m_SectionWindow != null)
                m_SectionWindow.SetAsLastSibling();
        }

        void BuildRailStrip(DevConsoleSettings settings)
        {
            var theme = DevConsoleHost.ActiveTheme;
            m_RailStrip = DevConsoleUiUtil.CreateRect("RailStrip", m_Root);
            var stripImg = DevConsoleUiUtil.AddImage(m_RailStrip, theme.railBackground.color);
            DevConsoleTheme.ApplySurface(stripImg, theme.railBackground);

            m_RailStripIcons = DevConsoleUiUtil.CreateRect("Icons", m_RailStrip);
            var iconsLe = m_RailStripIcons.gameObject.AddComponent<LayoutElement>();
            iconsLe.flexibleWidth = 1f;
            iconsLe.flexibleHeight = 1f;

            var expandRt = DevConsoleUiUtil.CreateRect("Expand", m_RailStrip);
            var expandLe = expandRt.gameObject.AddComponent<LayoutElement>();
            expandLe.minWidth = settings.railCollapsedWidth - 4f;
            expandLe.minHeight = settings.railCollapsedWidth - 4f;
            expandLe.preferredWidth = settings.railCollapsedWidth - 4f;
            expandLe.preferredHeight = settings.railCollapsedWidth - 4f;
            expandLe.flexibleWidth = 0f;
            expandLe.flexibleHeight = 0f;
            m_RailStripExpand = DevConsoleUiUtil.AddButton(expandRt, theme.buttonNormal);
            m_RailStripExpandLabel = DevConsoleUiUtil.AddLabel(
                expandRt, "Label", "⟨", theme.bodyFontSize, TextAlignmentOptions.Midline, true);
            m_RailStripExpand.onClick.AddListener(() => SetCollapsed(false));

            m_RailStrip.gameObject.SetActive(false);
        }

        void BuildRailTitleBar(DevConsoleSettings settings)
        {
            float titleH = 32f;
            var header = DevConsoleUiUtil.CreateRect("Header", m_Rail);
            m_HeaderLe = header.gameObject.AddComponent<LayoutElement>();
            m_HeaderLe.minHeight = titleH;
            m_HeaderLe.preferredHeight = titleH;
            m_HeaderLe.flexibleHeight = 0f;
            m_HeaderLe.layoutPriority = 2;
            m_HeaderRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            m_HeaderRow.childControlWidth = true;
            m_HeaderRow.childControlHeight = true;
            m_HeaderRow.childForceExpandWidth = false;
            m_HeaderRow.childForceExpandHeight = true;
            m_HeaderRow.childScaleWidth = false;
            m_HeaderRow.childScaleHeight = false;
            m_HeaderRow.padding = new RectOffset(6, 6, 2, 2);
            m_HeaderRow.spacing = 6f;

            var titleRt = DevConsoleUiUtil.CreateRect("Title", header);
            m_TitleLe = titleRt.gameObject.AddComponent<LayoutElement>();
            m_TitleLe.flexibleWidth = 1f;
            m_TitleLe.preferredHeight = titleH;
            m_TitleLe.flexibleHeight = 0f;
            m_Title = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(m_Title, true);
            m_Title.color = DevConsoleUiUtil.Accent;
            m_Title.fontSize = DevConsoleHost.ActiveTheme.titleFontSize;
            m_Title.alignment = TextAlignmentOptions.MidlineLeft;
            m_Title.raycastTarget = false;

            m_CollapseButton = CreateChromeButton(
                header, "Collapse", "-", false, () => SetCollapsed(true),
                out m_CollapseLe, out m_CollapseLabel);
            m_CloseButton = CreateChromeButton(
                header, "Close", "×", true, DevConsole.Close,
                out m_CloseLe, out m_CloseLabel);
        }

        void BuildSectionWindowChrome(DevConsoleSettings settings)
        {
            float titleH = 32f;
            var header = DevConsoleUiUtil.CreateRect("WindowHeader", m_SectionWindow);
            var headerLe = header.gameObject.AddComponent<LayoutElement>();
            headerLe.minHeight = titleH;
            headerLe.preferredHeight = titleH;
            headerLe.flexibleHeight = 0f;
            headerLe.layoutPriority = 2;
            var row = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
            row.padding = new RectOffset(4, 4, 2, 2);
            row.spacing = 6f;

            var titleRt = DevConsoleUiUtil.CreateRect("Title", header);
            var titleLe = titleRt.gameObject.AddComponent<LayoutElement>();
            titleLe.preferredWidth = 140f;
            titleLe.preferredHeight = titleH;
            m_WindowTitle = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(m_WindowTitle, true);
            var theme = DevConsoleHost.ActiveTheme;
            m_WindowTitle.fontSize = theme.titleFontSize;
            m_WindowTitle.color = theme.accentColor;
            m_WindowTitle.alignment = TextAlignmentOptions.MidlineLeft;

            var filterRt = DevConsoleUiUtil.CreateRect("Filter", header);
            var filterLe = filterRt.gameObject.AddComponent<LayoutElement>();
            filterLe.flexibleWidth = 1f;
            filterLe.preferredHeight = titleH - 6f;
            DevConsoleUiUtil.AddImage(filterRt, theme.filterWell);
            m_FilterField = filterRt.gameObject.AddComponent<TMP_InputField>();
            var filterTextRt = DevConsoleUiUtil.CreateRect("Text", filterRt);
            DevConsoleUiUtil.Stretch(filterTextRt);
            filterTextRt.offsetMin = new Vector2(6, 2);
            filterTextRt.offsetMax = new Vector2(-6, -2);
            var filterText = filterTextRt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(filterText, false);
            filterText.fontSize = theme.bodyFontSize * 0.9f;
            filterText.color = theme.labelColor;
            var placeholderRt = DevConsoleUiUtil.CreateRect("Placeholder", filterRt);
            DevConsoleUiUtil.Stretch(placeholderRt);
            placeholderRt.offsetMin = new Vector2(6, 2);
            placeholderRt.offsetMax = new Vector2(-6, -2);
            var placeholder = placeholderRt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(placeholder, false);
            placeholder.text = "Filter";
            placeholder.fontSize = theme.bodyFontSize * 0.9f;
            placeholder.color = theme.placeholderColor;
            m_FilterField.textViewport = filterTextRt;
            m_FilterField.textComponent = filterText;
            m_FilterField.placeholder = placeholder;
            m_FilterField.onValueChanged.AddListener(OnFilterChanged);

            m_WindowCloseButton = CreateChromeButton(
                header, "WinClose", "X", true, () =>
                {
                    if (m_LogWindowOpen)
                    {
                        m_LogWindowOpen = false;
                        RefreshRailSectionWindow();
                        return;
                    }

                    DevConsole.CloseSection();
                },
                out _, out m_WindowCloseLabel);

            var body = DevConsoleUiUtil.CreateRect("WindowBody", m_SectionWindow);
            var bodyLe = body.gameObject.AddComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1f;
            bodyLe.minHeight = 120f;
            m_WindowScroll = body.gameObject.AddComponent<ScrollRect>();
            DevConsoleUiUtil.AddImage(body, theme.scrollWell);
            var vp = DevConsoleUiUtil.CreateRect("Viewport", body);
            DevConsoleUiUtil.Stretch(vp);
            vp.gameObject.AddComponent<RectMask2D>();
            m_WindowContent = DevConsoleUiUtil.CreateRect("Content", vp);
            m_WindowContent.anchorMin = new Vector2(0, 1);
            m_WindowContent.anchorMax = new Vector2(1, 1);
            m_WindowContent.pivot = new Vector2(0.5f, 1f);
            DevConsoleUiUtil.FitTopStretchContent(m_WindowContent);
            var grid = m_WindowContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
            float sp = theme.spacing > 0f ? theme.spacing : settings.spacing;
            grid.spacing = new Vector2(sp, sp);
            grid.padding = new RectOffset(0, 0, 0, 0);
            m_WindowContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            m_WindowScroll.content = m_WindowContent;
            m_WindowScroll.viewport = vp;
            m_WindowScroll.horizontal = false;
            m_WindowScroll.vertical = true;

            m_Content = m_WindowContent;
            m_Scroll = m_WindowScroll;
            SyncCommandLineVisibility();
        }

        void RebuildRail()
        {
            if (m_Rail == null)
                return;

            EnsureChromeSizes();
            SyncCanvasScaler();
            ApplyRailLayout();
            if (m_Rail != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Rail);
            Canvas.ForceUpdateCanvases();

            RebuildQuickGrid();
            RebuildPins();
            RebuildSectionButtons();
            RefreshRailSectionWindow();
            RefreshTitle();
            RefreshRailNotes();
            RefreshRailChrome();
        }

        void RebuildQuickGrid()
        {
            for (int i = m_QuickRoot.childCount - 1; i >= 0; i--)
                Object.Destroy(m_QuickRoot.GetChild(i).gameObject);
            m_QuickButtons.Clear();

            var quicks = DevConsoleRegistry.Quicks;
            bool empty = quicks.Count == 0;
            m_QuickRoot.gameObject.SetActive(!empty);
            if (empty)
                return;

            var settings = DevConsoleHost.Settings;
            var grid = m_QuickRoot.GetComponent<GridLayoutGroup>();
            float cellH = settings.buttonHeight;
            if (m_Rail != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Rail);
            float padL = m_PanelVlg != null ? m_PanelVlg.padding.left : 6f;
            float padR = m_PanelVlg != null ? m_PanelVlg.padding.right : 6f;
            float inner = m_Rail != null
                ? Mathf.Max(40f, m_Rail.rect.width - padL - padR)
                : Mathf.Max(40f, m_RailWidth - padL - padR);
            float w = Mathf.Max(40f, (inner - settings.spacing) * 0.5f);
            grid.cellSize = new Vector2(w, cellH);
            grid.spacing = new Vector2(settings.spacing, settings.spacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            var le = m_QuickRoot.GetComponent<LayoutElement>();
            int rows = (quicks.Count + 1) / 2;
            le.minHeight = rows * cellH + (rows - 1) * settings.spacing;
            le.preferredHeight = le.minHeight;

            for (int i = 0; i < quicks.Count; i++)
            {
                var q = quicks[i];
                var entry = new DevConsoleEntry
                {
                    Section = "__quick__",
                    Label = q.Label,
                    Caption = q.Label,
                    Kind = DevConsoleEntryKind.Button,
                    Action = q.Action,
                    Available = q.Available,
                    Visible = true
                };
                var wdg = new DevConsoleButtonWidget(m_QuickRoot, entry, settings, m_Canvas);
                m_QuickButtons.Add(wdg);
            }
        }

        void RebuildPins()
        {
            for (int i = m_PinRoot.childCount - 1; i >= 0; i--)
                Object.Destroy(m_PinRoot.GetChild(i).gameObject);
            m_PinLabels.Clear();
            m_PinEntries.Clear();
            m_PinAccum.Clear();

            var pins = DevConsoleRegistry.Pins;
            bool any = false;
            for (int i = 0; i < pins.Count; i++)
            {
                var entry = DevConsoleRegistry.Find(pins[i].Section, pins[i].Label);
                if (entry == null || entry.Kind != DevConsoleEntryKind.Readout)
                    continue;
                any = true;
                var rt = DevConsoleUiUtil.CreateRect("Pin_" + entry.Label, m_PinRoot);
                var le = rt.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 18f;
                le.preferredHeight = 18f;
                var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
                DevConsoleUiUtil.StyleLabel(label, false);
                label.fontSize = 14f;
                label.color = DevConsoleUiUtil.TextColor;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.raycastTarget = false;
                m_PinLabels.Add(label);
                m_PinEntries.Add(entry);
                m_PinAccum.Add(999f);
            }

            m_PinRoot.gameObject.SetActive(any);
            TickPins(1f);
        }

        void RebuildSectionButtons()
        {
            for (int i = m_SectionListContent.childCount - 1; i >= 0; i--)
                Object.Destroy(m_SectionListContent.GetChild(i).gameObject);
            m_SectionButtons.Clear();

            var settings = DevConsoleHost.Settings;
            var sections = DevConsoleRegistry.GetOrderedSections();
            string open = DevConsole.OpenSectionName;
            for (int i = 0; i < sections.Count; i++)
            {
                var sec = sections[i];
                string name = string.IsNullOrEmpty(sec.Name) ? "(default)" : sec.Name;
                int count = DevConsoleRegistry.CountVisibleEntries(sec);
                var rt = DevConsoleUiUtil.CreateRect("Sec_" + name, m_SectionListContent);
                var le = rt.gameObject.AddComponent<LayoutElement>();
                le.minHeight = settings.buttonHeight;
                le.preferredHeight = settings.buttonHeight;
                bool active = string.Equals(sec.Name, open, System.StringComparison.Ordinal);
                Color bg = active
                    ? DevConsoleHost.ActiveTheme.buttonSelected
                    : DevConsoleUiUtil.ButtonBg;
                var btn = DevConsoleUiUtil.AddButton(rt, bg);
                var label = DevConsoleUiUtil.AddLabel(
                    rt, "Label", name, settings.fontSize, TextAlignmentOptions.MidlineLeft);
                label.rectTransform.offsetMin = new Vector2(8, 0);
                label.rectTransform.offsetMax = new Vector2(-40, 0);
                var countLabel = DevConsoleUiUtil.AddLabel(
                    rt, "Count", count.ToString(), 12f, TextAlignmentOptions.MidlineRight);
                countLabel.rectTransform.offsetMin = new Vector2(-36, 0);
                countLabel.rectTransform.offsetMax = new Vector2(-8, 0);
                string captured = sec.Name;
                btn.onClick.AddListener(() => DevConsole.OpenSection(captured));
                m_SectionButtons.Add(btn);
            }
        }

        void RefreshRailSectionWindow()
        {
            string section = DevConsole.OpenSectionName;
            if (!string.IsNullOrEmpty(section))
                m_LogWindowOpen = false;

            bool log = m_LogWindowOpen;
            bool show = !string.IsNullOrEmpty(section) || log;
            bool showBlocker = show && !(LayoutScreenHeight > LayoutScreenWidth);
            if (m_Blocker != null)
            {
                m_Blocker.gameObject.SetActive(showBlocker);
                if (showBlocker)
                    DevConsoleUiUtil.Stretch(m_Blocker);
            }

            if (m_SectionWindow != null)
            {
                m_SectionWindow.gameObject.SetActive(show);
                if (show)
                    ApplyRailDrawOrder();
            }

            if (!show)
            {
                RebuildSectionButtons();
                if (m_Collapsed)
                    RebuildRailStripIcons();
                return;
            }

            ApplySectionWindowRect();
            if (m_SectionWindow != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_SectionWindow);
            Canvas.ForceUpdateCanvases();

            if (log)
            {
                m_WindowTitle.text = "Log";
                if (m_WindowCloseLabel != null)
                    m_WindowCloseLabel.text = LayoutScreenHeight > LayoutScreenWidth ? "<" : "X";
                RebuildLogWindowBody();
                RebuildSectionButtons();
                if (m_Collapsed)
                    RebuildRailStripIcons();
                return;
            }

            m_WindowTitle.text = string.IsNullOrEmpty(section) ? "(default)" : section;
            if (m_WindowCloseLabel != null)
                m_WindowCloseLabel.text = LayoutScreenHeight > LayoutScreenWidth ? "<" : "X";
            RebuildSectionWindowBody(section);
            RebuildSectionButtons();
            if (m_Collapsed)
                RebuildRailStripIcons();
        }

        void RebuildSectionWindowBody(string sectionName)
        {
            for (int i = m_WindowContent.childCount - 1; i >= 0; i--)
                Object.Destroy(m_WindowContent.GetChild(i).gameObject);
            m_Buttons.Clear();
            m_Toggles.Clear();
            m_Dropdowns.Clear();
            m_Confirms.Clear();
            m_Readouts.Clear();
            m_Selectables.Clear();

            var settings = DevConsoleHost.Settings;
            var theme = DevConsoleHost.ActiveTheme;
            DevConsoleSection section = null;
            var sections = DevConsoleRegistry.GetOrderedSections();
            for (int i = 0; i < sections.Count; i++)
            {
                if (string.Equals(sections[i].Name, sectionName, System.StringComparison.Ordinal))
                {
                    section = sections[i];
                    break;
                }
            }

            if (section == null)
                return;

            // Prefer vertical flow over fixed grid so buttons auto-size and wrap.
            var grid = m_WindowContent.GetComponent<GridLayoutGroup>();
            if (grid != null)
                Object.DestroyImmediate(grid);
            var vlg = m_WindowContent.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = m_WindowContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = settings.spacing;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            float viewportWidth = m_WindowScroll != null ? m_WindowScroll.viewport.rect.width : 0f;
            if (viewportWidth <= 80f)
            {
                Canvas.ForceUpdateCanvases();
                if (m_SectionWindow != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_SectionWindow);
                viewportWidth = m_WindowScroll != null ? m_WindowScroll.viewport.rect.width : 0f;
            }

            if (viewportWidth <= 80f)
            {
                // Layout not settled yet — build with a temporary width, rebuild next tick.
                m_PendingFlowRebuild = true;
                float fallback = m_SectionWindow != null ? m_SectionWindow.rect.width - 24f : 0f;
                viewportWidth = Mathf.Max(320f, fallback);
            }
            else
            {
                m_PendingFlowRebuild = false;
            }

            float cellH = DevConsoleUiUtil.MinTouch(settings, m_Canvas);
            float font = DevConsoleUiUtil.FontSize(settings, m_Canvas);
            string filter = m_FilterText ?? string.Empty;

            RectTransform flowRow = null;
            float flowUsed = 0f;

            void EndFlowRow()
            {
                flowRow = null;
                flowUsed = 0f;
            }

            RectTransform EnsureFlowRow()
            {
                if (flowRow != null)
                    return flowRow;
                flowRow = DevConsoleUiUtil.CreateRect("FlowRow", m_WindowContent);
                var le = flowRow.gameObject.AddComponent<LayoutElement>();
                le.minHeight = cellH;
                le.preferredHeight = cellH;
                le.flexibleWidth = 1f;
                var hlg = flowRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = settings.spacing;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;
                flowUsed = 0f;
                return flowRow;
            }

            float EstimateButtonWidth(DevConsoleEntry e)
            {
                string text = string.IsNullOrEmpty(e.Caption) ? e.Label : e.Caption;
                float pad = Mathf.Max(16f, settings.spacing * 4f);
                return Mathf.Max(cellH, font * 0.55f * (text?.Length ?? 0) + pad);
            }

            for (int i = 0; i < section.Entries.Count; i++)
            {
                var entry = section.Entries[i];
                if (!DevConsoleRegistry.IsEffectivelyVisible(entry))
                    continue;
                if (filter.Length > 0 &&
                    entry.Kind != DevConsoleEntryKind.SubHeader &&
                    (entry.Label == null ||
                     entry.Label.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0))
                    continue;

                switch (entry.Kind)
                {
                    case DevConsoleEntryKind.SubHeader:
                    {
                        EndFlowRow();
                        var rt = DevConsoleUiUtil.CreateRect("SubHeader_" + entry.Label, m_WindowContent);
                        var le = rt.gameObject.AddComponent<LayoutElement>();
                        le.minHeight = cellH * 0.85f;
                        le.preferredHeight = cellH * 0.85f;
                        le.flexibleWidth = 1f;
                        DevConsoleUiUtil.AddImage(rt, theme.sectionHeader);
                        var tmp = DevConsoleUiUtil.AddLabel(
                            rt, "Label", entry.Label, font * 0.95f, TextAlignmentOptions.MidlineLeft);
                        tmp.color = theme.accentColor;
                        break;
                    }
                    case DevConsoleEntryKind.Button:
                    {
                        float need = EstimateButtonWidth(entry);
                        if (flowRow != null && flowUsed + need > viewportWidth && flowUsed > 0f)
                            EndFlowRow();
                        var row = EnsureFlowRow();
                        var w = new DevConsoleButtonWidget(row, entry, settings, m_Canvas, flowSized: true);
                        m_Buttons.Add(w);
                        m_Selectables.Add(new SelectableSlot
                        {
                            Section = entry.Section,
                            Label = entry.Label,
                            Selectable = w.Selectable
                        });
                        flowUsed += need + settings.spacing;
                        break;
                    }
                    case DevConsoleEntryKind.Toggle:
                    {
                        EndFlowRow();
                        var w = new DevConsoleToggleWidget(m_WindowContent, entry, settings, m_Canvas);
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
                        EndFlowRow();
                        var w = new DevConsoleDropdownWidget(m_WindowContent, entry, settings, m_Canvas);
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
                        EndFlowRow();
                        var w = new DevConsoleConfirmWidget(m_WindowContent, entry, settings, m_Canvas);
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
                        EndFlowRow();
                        m_Readouts.Add(new DevConsoleReadoutWidget(m_WindowContent, entry, settings, m_Canvas));
                        break;
                }
            }

            EndFlowRow();
        }

        void RebuildLogWindowBody()
        {
            for (int i = m_WindowContent.childCount - 1; i >= 0; i--)
                Object.Destroy(m_WindowContent.GetChild(i).gameObject);
            var notes = DevConsoleRegistry.Notes;
            var grid = m_WindowContent.GetComponent<GridLayoutGroup>();
            grid.constraintCount = 1;
            grid.cellSize = new Vector2(Mathf.Max(100f, m_WindowScroll.viewport.rect.width), 22f);
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                var rt = DevConsoleUiUtil.CreateRect("Note", m_WindowContent);
                var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
                DevConsoleUiUtil.StyleLabel(label, false);
                label.fontSize = 14f;
                label.text = notes[i];
                label.color = DevConsoleUiUtil.TextColor;
            }
        }

        void OnFilterChanged(string value)
        {
            m_FilterText = value ?? string.Empty;
            if (!string.IsNullOrEmpty(DevConsole.OpenSectionName) && !m_LogWindowOpen)
                RebuildSectionWindowBody(DevConsole.OpenSectionName);
        }

        void OpenLogWindow()
        {
            m_LogWindowOpen = true;
            DevConsole.SetOpenSection(null);
            RefreshRailSectionWindow();
        }

        void RefreshRailNotes()
        {
            if (m_RailStatus != null)
                m_RailStatus.text = DevConsoleRegistry.Status ?? string.Empty;
        }

        void RefreshRailChrome()
        {
            bool collapsed = m_Collapsed;
            bool open = DevConsole.IsOpen;
            if (m_Rail != null)
                m_Rail.gameObject.SetActive(!collapsed && open);
            if (m_RailStrip != null)
                m_RailStrip.gameObject.SetActive(collapsed && open);
            if (collapsed && open)
            {
                RebuildRailStripIcons();
                ApplyRailStripLayout();
            }

            SyncCommandLineVisibility();
            RefreshRailSectionWindow();
        }

        float EffectiveRailReserveWidth(DevConsoleSettings settings, float canvasW, bool portrait)
        {
            if (m_Collapsed)
                return Mathf.Max(24f, settings.railCollapsedWidth);
            float width = m_RailWidth;
            if (width <= 0f)
            {
                width = Mathf.Clamp(canvasW * settings.railScreenFraction,
                    settings.railMinWidth, settings.railMaxWidth);
                if (portrait)
                    width = Mathf.Max(40f, canvasW - (LayoutSafeArea.x / Mathf.Max(
                        m_Canvas != null ? m_Canvas.scaleFactor : 1f, 0.01f)));
            }

            return width;
        }

        void ApplyRailLayout()
        {
            ApplyLayoutScreenCanvasSize();
            var settings = DevConsoleHost.Settings;
            float scale = Mathf.Max(m_Canvas != null ? m_Canvas.scaleFactor : 1f, 0.01f);
            Rect safe = LayoutSafeArea;
            m_LastSafeArea = safe;
            bool portrait = LayoutScreenHeight > LayoutScreenWidth;
            float margin = settings.railEdgeMargin;

            float canvasW = safe.width / scale;
            float hudTop = DevHudLayout.HudTopReserveCanvasUnits(settings, scale);
            float width = Mathf.Clamp(canvasW * settings.railScreenFraction,
                settings.railMinWidth, settings.railMaxWidth);
            if (portrait)
                width = Mathf.Max(40f, canvasW - (safe.x / scale));
            m_RailWidth = width;

            if (m_Rail != null)
            {
                float top = (LayoutScreenHeight - (safe.y + safe.height)) / scale + margin + hudTop;
                float bot = safe.y / scale + margin;
                float right = (LayoutScreenWidth - (safe.x + safe.width)) / scale + margin;
                m_Rail.anchorMin = new Vector2(1, 0);
                m_Rail.anchorMax = new Vector2(1, 1);
                m_Rail.pivot = new Vector2(1, 0.5f);
                m_Rail.offsetMin = new Vector2(-width, bot);
                m_Rail.offsetMax = new Vector2(-right, -top);
            }

            ApplyRailStripLayout();
            ApplySectionWindowRect();

            if (m_HeaderLe != null)
            {
                float titleH = 32f;
                ApplyHeaderChrome(titleH, settings.fontSize);
            }

            RefreshRailChrome();
            if (m_Rail != null && m_Rail.gameObject.activeSelf)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Rail);
            if (m_RailStrip != null && m_RailStrip.gameObject.activeSelf)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_RailStrip);
        }

        void ApplyRailStripLayout()
        {
            if (m_RailStrip == null)
                return;

            var settings = DevConsoleHost.Settings;
            float scale = Mathf.Max(m_Canvas != null ? m_Canvas.scaleFactor : 1f, 0.01f);
            Rect safe = LayoutSafeArea;
            bool portrait = LayoutScreenHeight > LayoutScreenWidth;
            float margin = settings.railEdgeMargin;
            float strip = Mathf.Max(24f, settings.railCollapsedWidth);
            float hudTop = DevHudLayout.HudTopReserveCanvasUnits(settings, scale);
            float top = (LayoutScreenHeight - (safe.y + safe.height)) / scale + margin + hudTop;
            float bot = safe.y / scale + margin;
            float left = safe.x / scale + margin;
            float right = (LayoutScreenWidth - (safe.x + safe.width)) / scale + margin;

            if (portrait != m_StripPortraitLayout ||
                m_RailStrip.GetComponent<HorizontalOrVerticalLayoutGroup>() == null)
            {
                m_StripPortraitLayout = portrait;
                EnsureRailStripLayoutGroups(portrait);
            }

            if (portrait)
            {
                m_RailStrip.anchorMin = new Vector2(0f, 1f);
                m_RailStrip.anchorMax = new Vector2(1f, 1f);
                m_RailStrip.pivot = new Vector2(0.5f, 1f);
                m_RailStrip.offsetMin = new Vector2(left, -top - strip);
                m_RailStrip.offsetMax = new Vector2(-right, -top);
                if (m_RailStripExpandLabel != null)
                    m_RailStripExpandLabel.text = "∨";
            }
            else
            {
                m_RailStrip.anchorMin = new Vector2(1f, 0f);
                m_RailStrip.anchorMax = new Vector2(1f, 1f);
                m_RailStrip.pivot = new Vector2(1f, 0.5f);
                m_RailStrip.offsetMin = new Vector2(-strip, bot);
                m_RailStrip.offsetMax = new Vector2(-right, -top);
                if (m_RailStripExpandLabel != null)
                    m_RailStripExpandLabel.text = "⟨";
            }

            if (m_RailStripExpand != null)
                m_RailStripExpand.transform.SetAsLastSibling();
        }

        void EnsureRailStripLayoutGroups(bool portrait)
        {
            var stripLg = m_RailStrip.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (stripLg != null)
                Object.DestroyImmediate(stripLg);
            if (m_RailStripIcons != null)
            {
                var iconsLg = m_RailStripIcons.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (iconsLg != null)
                    Object.DestroyImmediate(iconsLg);
            }

            int pad = Mathf.RoundToInt(DevConsoleHost.ActiveTheme.padding * 0.5f);
            if (portrait)
            {
                var row = m_RailStrip.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.padding = new RectOffset(pad, pad, pad, pad);
                row.spacing = 2f;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlWidth = true;
                row.childControlHeight = true;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = true;

                if (m_RailStripIcons != null)
                {
                    var iconsLe = m_RailStripIcons.GetComponent<LayoutElement>();
                    if (iconsLe != null)
                    {
                        iconsLe.flexibleWidth = 1f;
                        iconsLe.flexibleHeight = 1f;
                    }

                    var iconsRow = m_RailStripIcons.gameObject.AddComponent<HorizontalLayoutGroup>();
                    iconsRow.spacing = 2f;
                    iconsRow.childAlignment = TextAnchor.MiddleLeft;
                    iconsRow.childControlWidth = true;
                    iconsRow.childControlHeight = true;
                    iconsRow.childForceExpandWidth = false;
                    iconsRow.childForceExpandHeight = true;
                }
            }
            else
            {
                var col = m_RailStrip.gameObject.AddComponent<VerticalLayoutGroup>();
                col.padding = new RectOffset(pad, pad, pad, pad);
                col.spacing = 2f;
                col.childAlignment = TextAnchor.UpperCenter;
                col.childControlWidth = true;
                col.childControlHeight = true;
                col.childForceExpandWidth = true;
                col.childForceExpandHeight = false;

                if (m_RailStripIcons != null)
                {
                    var iconsLe = m_RailStripIcons.GetComponent<LayoutElement>();
                    if (iconsLe != null)
                    {
                        iconsLe.flexibleHeight = 1f;
                        iconsLe.flexibleWidth = 1f;
                    }

                    var iconsCol = m_RailStripIcons.gameObject.AddComponent<VerticalLayoutGroup>();
                    iconsCol.spacing = 2f;
                    iconsCol.childAlignment = TextAnchor.UpperCenter;
                    iconsCol.childControlWidth = true;
                    iconsCol.childControlHeight = true;
                    iconsCol.childForceExpandWidth = true;
                    iconsCol.childForceExpandHeight = false;
                }
            }
        }

        void RebuildRailStripIcons()
        {
            if (m_RailStripIcons == null)
                return;

            for (int i = m_RailStripIcons.childCount - 1; i >= 0; i--)
                Object.Destroy(m_RailStripIcons.GetChild(i).gameObject);
            m_StripSectionButtons.Clear();

            var settings = DevConsoleHost.Settings;
            var theme = DevConsoleHost.ActiveTheme;
            float cell = Mathf.Max(20f, settings.railCollapsedWidth - 8f);
            string open = DevConsole.OpenSectionName;
            var sections = DevConsoleRegistry.GetOrderedSections();
            for (int i = 0; i < sections.Count; i++)
            {
                var sec = sections[i];
                string name = string.IsNullOrEmpty(sec.Name) ? "?" : sec.Name;
                var rt = DevConsoleUiUtil.CreateRect("Strip_" + name, m_RailStripIcons);
                var le = rt.gameObject.AddComponent<LayoutElement>();
                le.minWidth = cell;
                le.minHeight = cell;
                le.preferredWidth = cell;
                le.preferredHeight = cell;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 0f;

                bool active = string.Equals(sec.Name, open, System.StringComparison.Ordinal);
                Color bg = active ? theme.buttonSelected : theme.buttonNormal;
                var btn = DevConsoleUiUtil.AddButton(rt, bg);
                var icon = DevConsoleRegistry.GetSectionIcon(sec.Name);
                if (icon != null)
                {
                    var imgRt = DevConsoleUiUtil.CreateRect("Icon", rt);
                    DevConsoleUiUtil.Stretch(imgRt);
                    imgRt.offsetMin = new Vector2(4, 4);
                    imgRt.offsetMax = new Vector2(-4, -4);
                    var img = DevConsoleUiUtil.AddImage(imgRt, Color.white);
                    img.sprite = icon;
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                }
                else
                {
                    char letter = char.ToUpperInvariant(name[0]);
                    DevConsoleUiUtil.AddLabel(
                        rt, "Letter", letter.ToString(),
                        Mathf.Max(12f, theme.bodyFontSize * 0.85f),
                        TextAlignmentOptions.Midline, true);
                }

                string captured = sec.Name;
                btn.onClick.AddListener(() => DevConsole.OpenSection(captured));
                m_StripSectionButtons.Add(btn);
            }

            if (m_RailStripExpand != null)
                m_RailStripExpand.transform.SetAsLastSibling();
        }

        void ApplySectionWindowRect()
        {
            if (m_SectionWindow == null)
                return;

            var settings = DevConsoleHost.Settings;
            float scale = Mathf.Max(m_Canvas != null ? m_Canvas.scaleFactor : 1f, 0.01f);
            Rect safe = LayoutSafeArea;
            bool portrait = LayoutScreenHeight > LayoutScreenWidth;
            float margin = settings.railEdgeMargin;
            float canvasW = safe.width / scale;
            float hudLeft = DevHudLayout.HudLeftReserveCanvasUnits(settings, scale);
            float hudTop = DevHudLayout.HudTopReserveCanvasUnits(settings, scale);
            float railW = EffectiveRailReserveWidth(settings, canvasW, portrait);
            float stripH = m_Collapsed ? Mathf.Max(24f, settings.railCollapsedWidth) : 0f;

            float winMargin = canvasW < 900f ? 8f : settings.windowMargin;
            if (portrait)
            {
                DevConsoleUiUtil.Stretch(m_SectionWindow);
                float topInset = hudTop + (m_Collapsed ? stripH + margin : 0f);
                // Expanded portrait: full sheet owns the area; window fills under HUD.
                // Collapsed: window sits under the icon strip.
                if (m_Collapsed)
                {
                    m_SectionWindow.offsetMin = new Vector2(safe.x / scale, safe.y / scale);
                    m_SectionWindow.offsetMax = new Vector2(
                        -((LayoutScreenWidth - (safe.x + safe.width)) / scale),
                        -((LayoutScreenHeight - (safe.y + safe.height)) / scale + topInset));
                }
                else
                {
                    m_SectionWindow.offsetMin = new Vector2(safe.x / scale, safe.y / scale);
                    m_SectionWindow.offsetMax = new Vector2(
                        -((LayoutScreenWidth - (safe.x + safe.width)) / scale),
                        -((LayoutScreenHeight - (safe.y + safe.height)) / scale + hudTop));
                }
            }
            else
            {
                float left = safe.x / scale + hudLeft + winMargin;
                float rightGap = railW + margin + winMargin;
                m_SectionWindow.anchorMin = Vector2.zero;
                m_SectionWindow.anchorMax = Vector2.one;
                m_SectionWindow.pivot = new Vector2(0.5f, 0.5f);
                m_SectionWindow.anchoredPosition = Vector2.zero;
                m_SectionWindow.sizeDelta = Vector2.zero;
                m_SectionWindow.offsetMin = new Vector2(left, safe.y / scale + winMargin);
                m_SectionWindow.offsetMax = new Vector2(
                    -((LayoutScreenWidth - (safe.x + safe.width)) / scale + rightGap),
                    -((LayoutScreenHeight - (safe.y + safe.height)) / scale + winMargin));
            }
        }

        void EnsureCommandLineRow(DevConsoleSettings settings)
        {
            // Prefer the expanded rail; fall back to the floating/dock panel.
            Transform parent = m_Rail != null ? m_Rail : (Transform)m_Panel;
            if (m_CommandLineBuilt || parent == null)
                return;
            m_CommandLineBuilt = true;
            var theme = DevConsoleHost.ActiveTheme;
            m_CommandRow = DevConsoleUiUtil.CreateRect("CommandLine", parent);
            var le = m_CommandRow.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 28f;
            le.preferredHeight = 28f;
            le.flexibleHeight = 0f;
            le.layoutPriority = 2;
            DevConsoleUiUtil.AddImage(m_CommandRow, theme.filterWell);

            m_CommandField = m_CommandRow.gameObject.AddComponent<TMP_InputField>();
            var textRt = DevConsoleUiUtil.CreateRect("Text", m_CommandRow);
            DevConsoleUiUtil.Stretch(textRt);
            textRt.offsetMin = new Vector2(8, 2);
            textRt.offsetMax = new Vector2(-8, -2);
            var text = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(text, false);
            text.fontSize = theme.bodyFontSize * 0.9f;
            text.color = theme.labelColor;

            var phRt = DevConsoleUiUtil.CreateRect("Placeholder", m_CommandRow);
            DevConsoleUiUtil.Stretch(phRt);
            phRt.offsetMin = new Vector2(8, 2);
            phRt.offsetMax = new Vector2(-8, -2);
            var ph = phRt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(ph, false);
            ph.text = "command…";
            ph.fontSize = theme.bodyFontSize * 0.9f;
            ph.color = theme.placeholderColor;

            m_CommandField.textViewport = textRt;
            m_CommandField.textComponent = text;
            m_CommandField.placeholder = ph;
            m_CommandField.lineType = TMP_InputField.LineType.SingleLine;
            // TMP + Input System often skips onSubmit; TickCommandLineHistory also handles Enter.
            m_CommandField.onSubmit.AddListener(OnCommandSubmit);
            // Keep at bottom of the rail / panel.
            m_CommandRow.SetAsLastSibling();
            SyncCommandLineVisibility();
        }

        void SyncCommandLineVisibility()
        {
            bool on = DevConsoleHost.IsCommandLineEnabled;
            if (on)
                EnsureCommandLineRow(DevConsoleHost.Settings);
            if (m_CommandRow != null)
            {
                m_CommandRow.gameObject.SetActive(on && !m_Collapsed && DevConsole.IsOpen);
                if (on)
                    m_CommandRow.SetAsLastSibling();
            }
        }

        void OnCommandSubmit(string line)
        {
            if (!DevConsoleHost.IsCommandLineEnabled)
                return;

            string trimmed = line != null ? line.Trim() : string.Empty;
            if (string.IsNullOrEmpty(trimmed))
            {
                ReactivateCommandField();
                return;
            }

            // Clear before run so a duplicate Enter (onSubmit + poll) is a no-op.
            if (m_CommandField != null)
                m_CommandField.SetTextWithoutNotify(string.Empty);

            DevConsoleCommands.TryRun(trimmed, out _);
            // Show output immediately: status line + open the log window.
            RefreshRailNotes();
            OpenLogWindow();
            ReactivateCommandField();
        }

        void ReactivateCommandField()
        {
            if (m_CommandField == null || !m_CommandField.gameObject.activeInHierarchy)
                return;
            m_CommandField.ActivateInputField();
            m_CommandField.Select();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(m_CommandField.gameObject);
        }

        void TickCommandLineHistory()
        {
            if (!DevConsoleHost.IsCommandLineEnabled || m_CommandField == null)
                return;
            if (!IsCommandLineFocused)
                return;
            var kb = Keyboard.current;
            if (kb == null)
                return;

            // Enter / keypad Enter — reliable path when TMP onSubmit does not fire under Input System.
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                OnCommandSubmit(m_CommandField.text);
                return;
            }

            if (kb.upArrowKey.wasPressedThisFrame)
                m_CommandField.text = DevConsoleCommands.HistoryPrev(m_CommandField.text);
            else if (kb.downArrowKey.wasPressedThisFrame)
                m_CommandField.text = DevConsoleCommands.HistoryNext(m_CommandField.text);
        }

        void TickPins(float dt)
        {
            for (int i = 0; i < m_PinEntries.Count; i++)
            {
                m_PinAccum[i] += dt;
                if (m_PinAccum[i] < m_PinEntries[i].ReadoutRefreshSeconds)
                    continue;
                m_PinAccum[i] = 0f;
                string value = "-";
                try { value = m_PinEntries[i].ReadoutValue?.Invoke() ?? "-"; }
                catch { value = "err"; }
                m_PinLabels[i].text = m_PinEntries[i].Label + ": " + value;
            }
        }

        bool IsPointerOverRailUi()
        {
            if (!DevConsole.IsOpen)
                return false;
            if (m_Rail != null && m_Rail.gameObject.activeInHierarchy && RectContainsScreenPoint(m_Rail))
                return true;
            if (m_RailStrip != null && m_RailStrip.gameObject.activeInHierarchy &&
                RectContainsScreenPoint(m_RailStrip))
                return true;
            if (m_SectionWindow != null && m_SectionWindow.gameObject.activeInHierarchy &&
                RectContainsScreenPoint(m_SectionWindow))
                return true;
            return false;
        }

        bool RectContainsScreenPoint(RectTransform rt)
        {
            if (rt == null || m_Canvas == null)
                return false;
            Vector2 screen = default;
            if (Mouse.current != null)
                screen = Mouse.current.position.ReadValue();
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                screen = Touchscreen.current.primaryTouch.position.ReadValue();
            else
                return false;

            Camera cam = m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : m_Canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, screen, cam);
        }
    }
}
#endif
