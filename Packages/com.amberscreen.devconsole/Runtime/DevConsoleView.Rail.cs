#if AMBERSCREEN_DEVCONSOLE
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    sealed partial class DevConsoleView
    {
        RectTransform m_Rail;
        RectTransform m_RailTab;
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

        void BuildRailUi(DevConsoleSettings settings)
        {
            m_Blocker = DevConsoleUiUtil.CreateRect("Blocker", m_Root);
            DevConsoleUiUtil.Stretch(m_Blocker);
            var blockerImg = DevConsoleUiUtil.AddImage(m_Blocker, new Color(0, 0, 0, 0f));
            blockerImg.raycastTarget = true;
            var blockerBtn = m_Blocker.gameObject.AddComponent<Button>();
            blockerBtn.transition = Selectable.Transition.None;
            blockerBtn.onClick.AddListener(() => DevConsole.CloseSection());
            m_Blocker.gameObject.SetActive(false);

            m_SectionWindow = DevConsoleUiUtil.CreateRect("SectionWindow", m_Root);
            DevConsoleUiUtil.AddImage(m_SectionWindow, DevConsoleUiUtil.PanelBg);
            var winV = m_SectionWindow.gameObject.AddComponent<VerticalLayoutGroup>();
            winV.padding = new RectOffset(8, 8, 8, 8);
            winV.spacing = settings.spacing;
            winV.childControlWidth = true;
            winV.childControlHeight = true;
            winV.childForceExpandWidth = true;
            winV.childForceExpandHeight = false;
            BuildSectionWindowChrome(settings);
            m_SectionWindow.gameObject.SetActive(false);

            m_Rail = DevConsoleUiUtil.CreateRect("Rail", m_Root);
            DevConsoleUiUtil.AddImage(m_Rail, new Color(0.08f, 0.09f, 0.11f, 0.88f));
            m_Panel = m_Rail;
            m_PanelVlg = m_Rail.gameObject.AddComponent<VerticalLayoutGroup>();
            m_PanelVlg.padding = new RectOffset(6, 6, 6, 6);
            m_PanelVlg.spacing = settings.spacing;
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
            DevConsoleUiUtil.AddImage(sectionScrollRt, new Color(0, 0, 0, 0.12f));
            var secVp = DevConsoleUiUtil.CreateRect("Viewport", sectionScrollRt);
            DevConsoleUiUtil.Stretch(secVp);
            secVp.gameObject.AddComponent<RectMask2D>();
            m_SectionListContent = DevConsoleUiUtil.CreateRect("Content", secVp);
            m_SectionListContent.anchorMin = new Vector2(0, 1);
            m_SectionListContent.anchorMax = new Vector2(1, 1);
            m_SectionListContent.pivot = new Vector2(0.5f, 1f);
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
            m_RailStatusButton = DevConsoleUiUtil.AddButton(statusRt, new Color(0, 0, 0, 0.2f));
            m_RailStatus = DevConsoleUiUtil.AddLabel(
                statusRt, "Label", string.Empty, 14f, TextAlignmentOptions.MidlineLeft);
            m_RailStatusButton.onClick.AddListener(OpenLogWindow);

            m_RailTab = DevConsoleUiUtil.CreateRect("RailTab", m_Root);
            m_RailTab.sizeDelta = new Vector2(44f, 22f);
            var tabBtn = DevConsoleUiUtil.AddChromeButton(
                m_RailTab, DevConsoleUiUtil.ChromeButtonBg, DevConsoleUiUtil.ChromeButtonHighlight);
            DevConsoleUiUtil.AddLabel(m_RailTab, "Label", "DEV", 12f, TextAlignmentOptions.Midline, true);
            tabBtn.onClick.AddListener(() => SetCollapsed(false));
            m_RailTab.gameObject.SetActive(false);
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
            m_Title.fontSize = settings.fontSize;
            m_Title.alignment = TextAlignmentOptions.MidlineLeft;
            m_Title.raycastTarget = false;

            m_CollapseButton = CreateChromeButton(
                header, "Collapse", "-", false, () => SetCollapsed(true),
                out m_CollapseLe, out m_CollapseLabel);
            m_CloseButton = CreateChromeButton(
                header, "Close", "X", true, DevConsole.Close,
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
            m_WindowTitle.fontSize = settings.fontSize;
            m_WindowTitle.color = DevConsoleUiUtil.Accent;
            m_WindowTitle.alignment = TextAlignmentOptions.MidlineLeft;

            var filterRt = DevConsoleUiUtil.CreateRect("Filter", header);
            var filterLe = filterRt.gameObject.AddComponent<LayoutElement>();
            filterLe.flexibleWidth = 1f;
            filterLe.preferredHeight = titleH - 6f;
            DevConsoleUiUtil.AddImage(filterRt, new Color(0, 0, 0, 0.35f));
            m_FilterField = filterRt.gameObject.AddComponent<TMP_InputField>();
            var filterTextRt = DevConsoleUiUtil.CreateRect("Text", filterRt);
            DevConsoleUiUtil.Stretch(filterTextRt);
            filterTextRt.offsetMin = new Vector2(6, 2);
            filterTextRt.offsetMax = new Vector2(-6, -2);
            var filterText = filterTextRt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(filterText, false);
            filterText.fontSize = 14f;
            filterText.color = DevConsoleUiUtil.TextColor;
            var placeholderRt = DevConsoleUiUtil.CreateRect("Placeholder", filterRt);
            DevConsoleUiUtil.Stretch(placeholderRt);
            placeholderRt.offsetMin = new Vector2(6, 2);
            placeholderRt.offsetMax = new Vector2(-6, -2);
            var placeholder = placeholderRt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(placeholder, false);
            placeholder.text = "Filter";
            placeholder.fontSize = 14f;
            placeholder.color = new Color(1, 1, 1, 0.35f);
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
            DevConsoleUiUtil.AddImage(body, new Color(0, 0, 0, 0.15f));
            var vp = DevConsoleUiUtil.CreateRect("Viewport", body);
            DevConsoleUiUtil.Stretch(vp);
            vp.gameObject.AddComponent<RectMask2D>();
            m_WindowContent = DevConsoleUiUtil.CreateRect("Content", vp);
            m_WindowContent.anchorMin = new Vector2(0, 1);
            m_WindowContent.anchorMax = new Vector2(1, 1);
            m_WindowContent.pivot = new Vector2(0.5f, 1f);
            var grid = m_WindowContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
            grid.spacing = new Vector2(settings.spacing, settings.spacing);
            m_WindowContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            m_WindowScroll.content = m_WindowContent;
            m_WindowScroll.viewport = vp;
            m_WindowScroll.horizontal = false;
            m_WindowScroll.vertical = true;
            m_Content = m_WindowContent;
            m_Scroll = m_WindowScroll;
        }

        void RebuildRail()
        {
            if (m_Rail == null)
                return;

            EnsureChromeSizes();
            SyncCanvasScaler();
            ApplyRailLayout();
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
            float w = Mathf.Max(40f, (m_RailWidth - 12f - settings.spacing) * 0.5f);
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
                    ? Color.Lerp(DevConsoleUiUtil.ButtonBg, DevConsoleUiUtil.Accent, 0.35f)
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
            if (m_Blocker != null)
                m_Blocker.gameObject.SetActive(show && !DevConsole.IsPortraitRail());
            if (m_SectionWindow != null)
                m_SectionWindow.gameObject.SetActive(show);

            if (!show)
            {
                RebuildSectionButtons();
                return;
            }

            if (log)
            {
                m_WindowTitle.text = "Log";
                if (m_WindowCloseLabel != null)
                    m_WindowCloseLabel.text = DevConsole.IsPortraitRail() ? "<" : "X";
                RebuildLogWindowBody();
                RebuildSectionButtons();
                return;
            }

            m_WindowTitle.text = string.IsNullOrEmpty(section) ? "(default)" : section;
            if (m_WindowCloseLabel != null)
                m_WindowCloseLabel.text = DevConsole.IsPortraitRail() ? "<" : "X";
            RebuildSectionWindowBody(section);
            RebuildSectionButtons();
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

            Canvas.ForceUpdateCanvases();
            float contentWidth = m_WindowScroll.viewport.rect.width;
            int cols = DevConsoleUiUtil.ComputeAutoColumns(
                contentWidth, settings.minEntryWidth, settings.spacing);
            float cellH = DevConsoleUiUtil.MinTouch(settings, m_Canvas);
            float cellW = cols <= 1
                ? contentWidth
                : (contentWidth - settings.spacing * (cols - 1)) / cols;
            var grid = m_WindowContent.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(Mathf.Max(80f, cellW), cellH);
            grid.spacing = new Vector2(settings.spacing, settings.spacing);
            grid.constraintCount = cols;

            string filter = m_FilterText ?? string.Empty;
            for (int i = 0; i < section.Entries.Count; i++)
            {
                var entry = section.Entries[i];
                if (!entry.Visible)
                    continue;
                if (filter.Length > 0 &&
                    (entry.Label == null ||
                     entry.Label.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0))
                    continue;

                switch (entry.Kind)
                {
                    case DevConsoleEntryKind.Button:
                    {
                        var w = new DevConsoleButtonWidget(m_WindowContent, entry, settings, m_Canvas);
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
                        m_Readouts.Add(new DevConsoleReadoutWidget(m_WindowContent, entry, settings, m_Canvas));
                        break;
                }
            }
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
            if (m_Rail != null)
                m_Rail.gameObject.SetActive(!collapsed && DevConsole.IsOpen);
            if (m_RailTab != null)
                m_RailTab.gameObject.SetActive(collapsed && DevConsole.IsOpen);
            if (collapsed)
            {
                if (m_SectionWindow != null)
                    m_SectionWindow.gameObject.SetActive(false);
                if (m_Blocker != null)
                    m_Blocker.gameObject.SetActive(false);
            }
            else
            {
                RefreshRailSectionWindow();
            }
        }

        void ApplyRailLayout()
        {
            var settings = DevConsoleHost.Settings;
            float scale = Mathf.Max(m_Canvas != null ? m_Canvas.scaleFactor : 1f, 0.01f);
            Rect safe = Screen.safeArea;
            m_LastSafeArea = safe;
            bool portrait = Screen.height > Screen.width;
            float margin = settings.railEdgeMargin;

            float canvasW = safe.width / scale;
            float width = Mathf.Clamp(canvasW * settings.railScreenFraction,
                settings.railMinWidth, settings.railMaxWidth);
            if (portrait)
                width = canvasW - (safe.x / scale);
            m_RailWidth = width;

            if (m_Rail != null)
            {
                float top = (Screen.height - (safe.y + safe.height)) / scale + margin;
                float bot = safe.y / scale + margin;
                float right = (Screen.width - (safe.x + safe.width)) / scale + margin;
                m_Rail.anchorMin = new Vector2(1, 0);
                m_Rail.anchorMax = new Vector2(1, 1);
                m_Rail.pivot = new Vector2(1, 0.5f);
                m_Rail.offsetMin = new Vector2(-width, bot);
                m_Rail.offsetMax = new Vector2(-right, -top);
            }

            if (m_RailTab != null)
            {
                m_RailTab.anchorMin = new Vector2(1, 0.6f);
                m_RailTab.anchorMax = new Vector2(1, 0.6f);
                m_RailTab.pivot = new Vector2(1, 0.5f);
                m_RailTab.anchoredPosition = new Vector2(-margin, 0f);
                m_RailTab.sizeDelta = new Vector2(44f, 22f);
            }

            if (m_SectionWindow != null && m_SectionWindow.gameObject.activeSelf)
            {
                float winMargin = canvasW < 900f ? 8f : settings.windowMargin;
                float left = safe.x / scale + (portrait ? 0f : winMargin);
                float rightGap = width + margin + winMargin;
                if (portrait)
                {
                    DevConsoleUiUtil.Stretch(m_SectionWindow);
                    m_SectionWindow.offsetMin = Vector2.zero;
                    m_SectionWindow.offsetMax = Vector2.zero;
                }
                else
                {
                    m_SectionWindow.anchorMin = Vector2.zero;
                    m_SectionWindow.anchorMax = Vector2.one;
                    m_SectionWindow.offsetMin = new Vector2(left, safe.y / scale + winMargin);
                    m_SectionWindow.offsetMax = new Vector2(
                        -((Screen.width - (safe.x + safe.width)) / scale + rightGap),
                        -((Screen.height - (safe.y + safe.height)) / scale + winMargin));
                }
            }

            if (m_HeaderLe != null)
            {
                float titleH = 32f;
                ApplyHeaderChrome(titleH, settings.fontSize);
            }

            RefreshRailChrome();
            if (m_Rail != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Rail);
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
            if (m_RailTab != null && m_RailTab.gameObject.activeInHierarchy && RectContainsScreenPoint(m_RailTab))
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
