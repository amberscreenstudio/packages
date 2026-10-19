#if AMBERSCREEN_DEVCONSOLE
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AmberScreen.Dev
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
        CanvasScaler m_Scaler;
        RectTransform m_Root;
        RectTransform m_Panel;
        RectTransform m_Content;
        RectTransform m_CollapsedHeader;
        Button m_CollapsedExpandButton;
        ScrollRect m_Scroll;
        LayoutElement m_ScrollLe;
        ScrollRect m_HistoryScroll;
        LayoutElement m_HistoryLe;
        RectTransform m_HistoryContent;
        TextMeshProUGUI m_Title;
        TextMeshProUGUI m_Status;
        TextMeshProUGUI m_History;
        Button m_CollapseButton;
        Button m_HistoryToggleButton;
        TextMeshProUGUI m_HistoryToggleLabel;

        Canvas m_DevCanvas;
        CanvasScaler m_DevScaler;
        Button m_DevButton;
        RectTransform m_DevButtonRt;
        Button m_CloseButton;
        LayoutElement m_HeaderLe;
        VerticalLayoutGroup m_PanelVlg;

        bool m_Collapsed;
        bool m_HistoryExpanded = true;
        bool m_ChromeSized;
        Rect m_LastSafeArea;
        Rect m_DevLastSafeArea;
        Vector2Int m_LastScreen;
        Vector2Int m_DevLastScreen;
        GameObject m_LastSelected;
        float m_AvailRefreshAccum;

        public bool IsDevButtonVisible =>
            m_DevCanvas != null && m_DevCanvas.gameObject.activeSelf;

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
            if (visible)
            {
                EnsureChromeSizes();
                SyncCanvasScaler();
                ApplyLayout();
            }

            if (!visible)
                SetCollapsed(false);
            UpdateDevButton();
        }

        public void OnSettingsReloaded()
        {
            m_ChromeSized = false;
            DevConsoleUiUtil.ResetFontCache();
            DevConsoleUiUtil.ResolveFont(DevConsoleHost.Settings);
            SyncCanvasScaler();
            ApplyLayout();
            SyncDevScaler();
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

            EnsureChromeSizes();
            SyncCanvasScaler();
            ApplyLayout();
            Canvas.ForceUpdateCanvases();

            var settings = DevConsoleHost.Settings;
            var sections = DevConsoleRegistry.GetOrderedSections();
            float contentWidth = m_Scroll != null && m_Scroll.viewport != null
                ? m_Scroll.viewport.rect.width
                : m_Content.rect.width;

            for (int s = 0; s < sections.Count; s++)
            {
                var section = sections[s];
                if (!m_SectionExpanded.ContainsKey(section.Name))
                    m_SectionExpanded[section.Name] = true;

                BuildSection(section, settings, contentWidth);
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
            m_Title.text = "Dev Console  ·  " + Application.version + "  ·  " + scene;
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
                int start = Mathf.Max(0, notes.Count - 4);
                for (int i = notes.Count - 1; i >= start; i--)
                {
                    if (sb.Length > 0)
                        sb.Append('\n');
                    sb.Append(notes[i]);
                }

                m_History.text = sb.ToString();
            }

            UpdateHistoryLayout();
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

            m_AvailRefreshAccum += unscaledDeltaTime;
            if (m_AvailRefreshAccum >= 0.25f)
            {
                m_AvailRefreshAccum = 0f;
                RefreshAvailabilityAndToggles();
            }

            var screen = new Vector2Int(Screen.width, Screen.height);
            Rect safe = Screen.safeArea;
            bool screenChanged = screen != m_LastScreen;
            bool safeChanged = safe.x != m_LastSafeArea.x || safe.y != m_LastSafeArea.y ||
                               safe.width != m_LastSafeArea.width || safe.height != m_LastSafeArea.height;

            if (screenChanged || safeChanged)
            {
                m_LastScreen = screen;
                m_LastSafeArea = safe;
                SyncCanvasScaler();
                ApplyLayout();
                Rebuild();
            }

            FollowSelectionScroll();
            MaybeRepositionDevButton();
        }

        void RefreshAvailabilityAndToggles()
        {
            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            if (selected != null)
            {
                for (int i = 0; i < m_Buttons.Count; i++)
                {
                    if (m_Buttons[i].Selectable != null &&
                        m_Buttons[i].Selectable.gameObject == selected &&
                        !m_Buttons[i].WillBeInteractable())
                    {
                        MoveSelectionAwayFrom(selected);
                        break;
                    }
                }
            }

            for (int i = 0; i < m_Buttons.Count; i++)
                m_Buttons[i].RefreshInteractable();
            for (int i = 0; i < m_Toggles.Count; i++)
                m_Toggles[i].RefreshValueOnly();
        }

        void MoveSelectionAwayFrom(GameObject current)
        {
            if (EventSystem.current == null)
                return;

            int idx = -1;
            for (int i = 0; i < m_Selectables.Count; i++)
            {
                if (m_Selectables[i].Selectable != null &&
                    m_Selectables[i].Selectable.gameObject == current)
                {
                    idx = i;
                    break;
                }
            }

            for (int step = 1; step <= m_Selectables.Count; step++)
            {
                int i = idx < 0 ? step - 1 : (idx + step) % m_Selectables.Count;
                var sel = m_Selectables[i].Selectable;
                if (sel == null || sel.gameObject == current || !sel.IsInteractable())
                    continue;

                bool aboutToDisable = false;
                for (int b = 0; b < m_Buttons.Count; b++)
                {
                    if (m_Buttons[b].Selectable == sel && !m_Buttons[b].WillBeInteractable())
                    {
                        aboutToDisable = true;
                        break;
                    }
                }

                if (aboutToDisable)
                    continue;

                EventSystem.current.SetSelectedGameObject(sel.gameObject);
                return;
            }
        }

        void CaptureSelection(ref string section, ref string label)
        {
            if (EventSystem.current == null)
                return;
            var go = EventSystem.current.currentSelectedGameObject;
            if (go == null)
                return;

            if (m_CloseButton != null && go == m_CloseButton.gameObject)
            {
                section = "__chrome__";
                label = "__close__";
                return;
            }

            if (m_CollapseButton != null && go == m_CollapseButton.gameObject)
            {
                section = "__chrome__";
                label = "__collapse__";
                return;
            }

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

            if (section == "__chrome__" && label == "__close__" && m_CloseButton != null)
            {
                EventSystem.current.SetSelectedGameObject(m_CloseButton.gameObject);
                return;
            }

            if (section == "__chrome__" && label == "__collapse__" && m_CollapseButton != null &&
                m_CollapseButton.gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(m_CollapseButton.gameObject);
                return;
            }

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

            if (yMax > viewMax)
            {
                m_Content.anchoredPosition += new Vector2(0f, -(yMax - viewMax));
                m_Scroll.velocity = Vector2.zero;
            }
            else if (yMin < viewMin)
            {
                m_Content.anchoredPosition += new Vector2(0f, viewMin - yMin);
                m_Scroll.velocity = Vector2.zero;
            }
        }

        void BuildCanvas()
        {
            var settings = DevConsoleHost.Settings;
            DevConsoleUiUtil.ResolveFont(settings);
            DevConsoleUiUtil.ResolveSemiBold(settings);

            var canvasGo = new GameObject("DevConsoleCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(m_Host, false);
            m_Canvas = canvasGo.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = settings.sortingOrder;

            m_Scaler = canvasGo.AddComponent<CanvasScaler>();
            SyncCanvasScaler();

            canvasGo.AddComponent<GraphicRaycaster>();

            m_Root = canvasGo.GetComponent<RectTransform>();
            DevConsoleUiUtil.Stretch(m_Root);

            BuildPanel(settings);
            BuildCollapsedHeader(settings);
            m_LastScreen = new Vector2Int(Screen.width, Screen.height);
            m_LastSafeArea = Screen.safeArea;
            ApplyLayout();
            SetVisible(false);
        }

        void BuildDevButtonCanvas()
        {
            var settings = DevConsoleHost.Settings;
            var go = new GameObject("DevConsoleDevButtonCanvas", typeof(RectTransform));
            go.transform.SetParent(m_Host, false);
            m_DevCanvas = go.AddComponent<Canvas>();
            m_DevCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_DevCanvas.sortingOrder = settings.sortingOrder + 1;
            m_DevScaler = go.AddComponent<CanvasScaler>();
            SyncDevScaler();
            go.AddComponent<GraphicRaycaster>();

            var root = go.GetComponent<RectTransform>();
            DevConsoleUiUtil.Stretch(root);

            m_DevButtonRt = DevConsoleUiUtil.CreateRect("DevButton", root);
            m_DevButton = DevConsoleUiUtil.AddButton(m_DevButtonRt, DevConsoleUiUtil.Accent);
            var label = DevConsoleUiUtil.AddLabel(
                m_DevButtonRt, "Label", "DEV", 14f, TextAlignmentOptions.Midline);
            label.color = Color.black;
            m_DevButton.onClick.AddListener(DevConsole.Toggle);
            m_DevCanvas.gameObject.SetActive(false);
            UpdateDevButton();
        }

        void SyncCanvasScaler()
        {
            if (m_Scaler == null)
                return;

            var settings = DevConsoleHost.Settings;
            m_Canvas.sortingOrder = settings.sortingOrder;

            if (settings.layout == DevConsoleLayout.MobilePortrait)
            {
                m_Scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                m_Scaler.referenceResolution = settings.ReferenceResolution;
                m_Scaler.matchWidthOrHeight = 1f;
            }
            else
            {
                m_Scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                m_Scaler.scaleFactor = DevConsoleUiUtil.LandscapeScaleFactor(settings);
            }
        }

        void SyncDevScaler()
        {
            if (m_DevScaler == null)
                return;

            var settings = DevConsoleHost.Settings;
            if (m_DevCanvas != null)
                m_DevCanvas.sortingOrder = settings.sortingOrder + 1;

            if (settings.layout == DevConsoleLayout.MobilePortrait)
            {
                m_DevScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                m_DevScaler.referenceResolution = settings.ReferenceResolution;
                m_DevScaler.matchWidthOrHeight = 1f;
            }
            else
            {
                m_DevScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                m_DevScaler.scaleFactor = DevConsoleUiUtil.LandscapeScaleFactor(settings);
            }
        }

        void PlaceDevButtonInSafeArea()
        {
            if (m_DevButtonRt == null || m_DevCanvas == null)
                return;

            var settings = DevConsoleHost.Settings;
            float dp = DevConsoleUiUtil.DpToCanvas(m_DevCanvas);
            float size = Mathf.Max(48f * dp, DevConsoleUiUtil.MinTouch(settings, m_DevCanvas));

            Rect safe = Screen.safeArea;
            float sx = Screen.width > 0 ? safe.x / Screen.width : 0f;
            float sy = Screen.height > 0 ? (safe.y + safe.height) / Screen.height : 1f;
            m_DevButtonRt.anchorMin = new Vector2(sx, sy);
            m_DevButtonRt.anchorMax = new Vector2(sx, sy);
            m_DevButtonRt.pivot = new Vector2(0f, 1f);
            m_DevButtonRt.sizeDelta = new Vector2(size, size);
            m_DevButtonRt.anchoredPosition = new Vector2(8f * dp, -8f * dp);

            m_DevLastSafeArea = safe;
            m_DevLastScreen = new Vector2Int(Screen.width, Screen.height);

            var label = m_DevButtonRt.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.fontSize = DevConsoleUiUtil.FontSize(settings, m_DevCanvas) * 0.8f;
        }

        internal void MaybeRepositionDevButton()
        {
            if (!IsDevButtonVisible || m_DevButtonRt == null)
                return;

            Rect safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (safe.x == m_DevLastSafeArea.x && safe.y == m_DevLastSafeArea.y &&
                safe.width == m_DevLastSafeArea.width && safe.height == m_DevLastSafeArea.height &&
                screen == m_DevLastScreen)
                return;

            SyncDevScaler();
            PlaceDevButtonInSafeArea();
        }

        void UpdateDevButton()
        {
            if (m_DevButton == null || m_DevCanvas == null)
                return;

            var settings = DevConsoleHost.Settings;
            bool show = settings.showOnScreenDevButton && !DevConsole.IsOpen;
            bool wasShown = m_DevCanvas.gameObject.activeSelf;
            m_DevCanvas.gameObject.SetActive(show);

            if (show)
            {
                SyncDevScaler();
                PlaceDevButtonInSafeArea();
                if (!wasShown)
                    DevConsoleHost.NotifyDevButtonShown(true);
            }
            else if (wasShown)
            {
                DevConsoleHost.NotifyDevButtonShown(false);
            }
        }

        void EnsureChromeSizes()
        {
            if (m_ChromeSized || m_Canvas == null)
                return;

            Canvas.ForceUpdateCanvases();
            var settings = DevConsoleHost.Settings;
            float font = DevConsoleUiUtil.FontSize(settings, m_Canvas);
            float titleH = DevConsoleUiUtil.TitleBarHeight(settings, m_Canvas);
            float pad = settings.layout == DevConsoleLayout.MobilePortrait
                ? 8f * DevConsoleUiUtil.DpToCanvas(m_Canvas)
                : 8f;
            int pi = Mathf.RoundToInt(pad);

            if (m_PanelVlg != null)
            {
                m_PanelVlg.padding = new RectOffset(pi, pi, pi, pi);
                m_PanelVlg.spacing = settings.spacing;
            }

            if (m_HeaderLe != null)
            {
                m_HeaderLe.minHeight = titleH;
                m_HeaderLe.preferredHeight = titleH;
            }

            if (m_Title != null)
            {
                m_Title.font = DevConsoleUiUtil.FontSemiBold;
                m_Title.fontSize = font;
                DevConsoleUiUtil.ApplyCrisp(m_Title);
            }

            if (m_Status != null)
            {
                m_Status.font = DevConsoleUiUtil.Font;
                m_Status.fontSize = font * 0.85f;
                DevConsoleUiUtil.ApplyCrisp(m_Status);
            }

            if (m_History != null)
            {
                m_History.font = DevConsoleUiUtil.Font;
                m_History.fontSize = font * 0.75f;
                DevConsoleUiUtil.ApplyCrisp(m_History);
            }

            UpdateHistoryLayout();
            m_ChromeSized = true;
        }

        void UpdateHistoryLayout()
        {
            if (m_HistoryLe == null || m_HistoryScroll == null)
                return;

            var settings = DevConsoleHost.Settings;
            float font = DevConsoleUiUtil.FontSize(settings, m_Canvas);
            if (m_HistoryToggleLabel != null)
                m_HistoryToggleLabel.text = m_HistoryExpanded ? "v" : ">";

            if (!m_HistoryExpanded)
            {
                m_HistoryScroll.gameObject.SetActive(false);
                m_HistoryLe.minHeight = 0f;
                m_HistoryLe.preferredHeight = 0f;
                m_HistoryLe.flexibleHeight = 0f;
                return;
            }

            m_HistoryScroll.gameObject.SetActive(true);
            float line = Mathf.Max(font * 0.75f * 1.2f, 14f);
            float h = line * 4f + 8f;
            m_HistoryLe.minHeight = h;
            m_HistoryLe.preferredHeight = h;
            m_HistoryLe.flexibleHeight = 0f;

            if (m_HistoryContent != null && m_History != null)
            {
                m_History.ForceMeshUpdate();
                float contentH = Mathf.Min(Mathf.Max(m_History.preferredHeight + 8f, line), h);
                m_HistoryContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentH);
            }
        }

        void BuildPanel(DevConsoleSettings settings)
        {
            m_Panel = DevConsoleUiUtil.CreateRect("Panel", m_Root);
            DevConsoleUiUtil.AddImage(m_Panel, DevConsoleUiUtil.PanelBg);
            m_PanelVlg = m_Panel.gameObject.AddComponent<VerticalLayoutGroup>();
            m_PanelVlg.padding = new RectOffset(8, 8, 8, 8);
            m_PanelVlg.spacing = settings.spacing;
            m_PanelVlg.childAlignment = TextAnchor.UpperCenter;
            m_PanelVlg.childControlWidth = true;
            m_PanelVlg.childControlHeight = true;
            m_PanelVlg.childForceExpandWidth = true;
            m_PanelVlg.childForceExpandHeight = false;

            var header = DevConsoleUiUtil.CreateRect("Header", m_Panel);
            m_HeaderLe = header.gameObject.AddComponent<LayoutElement>();
            m_HeaderLe.minHeight = 32f;
            m_HeaderLe.preferredHeight = 32f;
            var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerRow.childAlignment = TextAnchor.MiddleCenter;
            headerRow.spacing = 6f;
            headerRow.childForceExpandWidth = false;
            headerRow.childForceExpandHeight = true;
            headerRow.padding = new RectOffset(4, 4, 0, 0);

            var titleRt = DevConsoleUiUtil.CreateRect("Title", header);
            var titleLe = titleRt.gameObject.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1.2f;
            titleLe.minWidth = 80f;
            m_Title = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
            var semi = DevConsoleUiUtil.FontSemiBold;
            if (semi != null)
                m_Title.font = semi;
            m_Title.fontSize = 16f;
            m_Title.color = DevConsoleUiUtil.Accent;
            m_Title.alignment = TextAlignmentOptions.MidlineLeft;
            m_Title.raycastTarget = false;
            DevConsoleUiUtil.ApplyCrisp(m_Title);

            var statusRt = DevConsoleUiUtil.CreateRect("Status", header);
            var statusLe = statusRt.gameObject.AddComponent<LayoutElement>();
            statusLe.flexibleWidth = 1f;
            statusLe.minWidth = 40f;
            m_Status = statusRt.gameObject.AddComponent<TextMeshProUGUI>();
            var regular = DevConsoleUiUtil.Font;
            if (regular != null)
                m_Status.font = regular;
            m_Status.fontSize = 14f;
            m_Status.color = DevConsoleUiUtil.TextColor;
            m_Status.alignment = TextAlignmentOptions.MidlineRight;
            m_Status.raycastTarget = false;
            DevConsoleUiUtil.ApplyCrisp(m_Status);

            m_HistoryToggleButton = CreateTextButton(header, "HistToggle", "v", 14f, 28f, ToggleHistory);
            m_HistoryToggleLabel = m_HistoryToggleButton.GetComponentInChildren<TextMeshProUGUI>();

            m_CollapseButton = CreateTextButton(header, "Collapse", "-", 16f, 28f, () =>
            {
                if (DevConsoleHost.Settings.layout == DevConsoleLayout.DockRight)
                    SetCollapsed(true);
            });

            m_CloseButton = CreateTextButton(header, "Close", "X", 16f, 28f, DevConsole.Close);

            var scrollRt = DevConsoleUiUtil.CreateRect("Scroll", m_Panel);
            m_ScrollLe = scrollRt.gameObject.AddComponent<LayoutElement>();
            m_ScrollLe.flexibleHeight = 1f;
            m_ScrollLe.minHeight = 80f;
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

            var histScroll = DevConsoleUiUtil.CreateRect("HistoryScroll", m_Panel);
            m_HistoryLe = histScroll.gameObject.AddComponent<LayoutElement>();
            m_HistoryLe.minHeight = 48f;
            m_HistoryLe.preferredHeight = 48f;
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
            if (regular != null)
                m_History.font = regular;
            m_History.fontSize = 12f;
            m_History.color = new Color(0.75f, 0.78f, 0.82f, 1f);
            m_History.alignment = TextAlignmentOptions.TopLeft;
            m_History.textWrappingMode = TextWrappingModes.Normal;
            m_History.overflowMode = TextOverflowModes.Ellipsis;
            m_History.raycastTarget = false;
            DevConsoleUiUtil.ApplyCrisp(m_History);
            var histMargins = m_History.margin;
            histMargins.x = 6f;
            histMargins.z = 6f;
            histMargins.y = 2f;
            histMargins.w = 2f;
            m_History.margin = histMargins;

            m_HistoryScroll.content = m_HistoryContent;
            m_HistoryScroll.viewport = histVp;
            m_HistoryScroll.horizontal = false;
            m_HistoryScroll.vertical = true;
            m_HistoryScroll.movementType = ScrollRect.MovementType.Clamped;
        }

        void ToggleHistory()
        {
            m_HistoryExpanded = !m_HistoryExpanded;
            UpdateHistoryLayout();
        }

        void BuildCollapsedHeader(DevConsoleSettings settings)
        {
            float font = settings.fontSize;
            m_CollapsedHeader = DevConsoleUiUtil.CreateRect("CollapsedHeader", m_Root);
            m_CollapsedHeader.anchorMin = new Vector2(1, 1);
            m_CollapsedHeader.anchorMax = new Vector2(1, 1);
            m_CollapsedHeader.pivot = new Vector2(1, 1);
            m_CollapsedHeader.sizeDelta = new Vector2(180f, 40f);
            m_CollapsedHeader.anchoredPosition = new Vector2(-8f, -8f);

            m_CollapsedExpandButton = DevConsoleUiUtil.AddButton(m_CollapsedHeader, DevConsoleUiUtil.PanelBg);
            DevConsoleUiUtil.AddLabel(
                m_CollapsedHeader, "Label", "Dev Console >", font,
                TextAlignmentOptions.Midline, semiBold: true);
            m_CollapsedExpandButton.onClick.AddListener(() => SetCollapsed(false));
            m_CollapsedHeader.gameObject.SetActive(false);
        }

        void ApplyLayout()
        {
            var settings = DevConsoleHost.Settings;
            SyncCanvasScaler();
            SyncDevScaler();

            bool dockCollapsed = settings.layout == DevConsoleLayout.DockRight && m_Collapsed;
            if (m_CollapsedHeader != null)
                m_CollapsedHeader.gameObject.SetActive(dockCollapsed && DevConsole.IsOpen);
            if (m_Panel != null)
                m_Panel.gameObject.SetActive(!dockCollapsed);

            if (m_CollapseButton != null)
                m_CollapseButton.gameObject.SetActive(settings.layout == DevConsoleLayout.DockRight);

            if (m_Panel == null || m_Canvas == null)
                return;

            float scale = Mathf.Max(m_Canvas.scaleFactor, 0.01f);
            Rect safe = Screen.safeArea;
            m_LastSafeArea = safe;

            switch (settings.layout)
            {
                case DevConsoleLayout.DockRight:
                {
                    float fracW = safe.width * settings.dockRightScreenFraction / scale;
                    float dockW = Mathf.Max(settings.dockRightWidth, fracW);
                    m_Panel.anchorMin = new Vector2(1, 0);
                    m_Panel.anchorMax = new Vector2(1, 1);
                    m_Panel.pivot = new Vector2(1, 0.5f);
                    m_Panel.anchoredPosition = Vector2.zero;
                    m_Panel.offsetMin = new Vector2(-dockW, 0f);
                    m_Panel.offsetMax = Vector2.zero;
                    break;
                }

                case DevConsoleLayout.MobilePortrait:
                {
                    float sx = safe.x / Screen.width;
                    float sy = safe.y / Screen.height;
                    float sw = safe.width / Screen.width;
                    float sh = safe.height / Screen.height;
                    m_Panel.anchorMin = new Vector2(sx, sy);
                    m_Panel.anchorMax = new Vector2(sx + sw, sy + sh);
                    m_Panel.offsetMin = Vector2.zero;
                    m_Panel.offsetMax = Vector2.zero;
                    m_Panel.pivot = new Vector2(0.5f, 0.5f);
                    break;
                }

                default:
                {
                    float sx = safe.x / Screen.width;
                    float sy = safe.y / Screen.height;
                    float sw = safe.width / Screen.width;
                    float sh = safe.height / Screen.height;
                    float fx = Mathf.Clamp01(settings.floatingScreenFraction.x);
                    float fy = Mathf.Clamp01(settings.floatingScreenFraction.y);
                    float insetX = sw * (1f - fx) * 0.5f;
                    float insetY = sh * (1f - fy) * 0.5f;
                    m_Panel.anchorMin = new Vector2(sx + insetX, sy + insetY);
                    m_Panel.anchorMax = new Vector2(sx + sw - insetX, sy + sh - insetY);
                    m_Panel.offsetMin = Vector2.zero;
                    m_Panel.offsetMax = Vector2.zero;
                    m_Panel.pivot = new Vector2(0.5f, 0.5f);
                    m_Panel.anchoredPosition = Vector2.zero;
                    m_Panel.sizeDelta = Vector2.zero;
                    break;
                }
            }

            if (m_ScrollLe != null)
            {
                m_ScrollLe.flexibleHeight = 1f;
                m_ScrollLe.minHeight = 80f;
                m_ScrollLe.preferredHeight = -1f;
            }

            UpdateHistoryLayout();
        }

        void BuildSection(DevConsoleSection section, DevConsoleSettings settings, float contentWidth)
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
            DevConsoleUiUtil.AddLabel(
                header, "Label", $"{arrow} {title}", font,
                TextAlignmentOptions.MidlineLeft, semiBold: true);
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

            int cols = ResolveColumnCount(settings, contentWidth);
            float cellH = DevConsoleUiUtil.MinTouch(settings, m_Canvas);
            float cellW = cols <= 1
                ? contentWidth
                : (contentWidth - settings.spacing * (cols - 1)) / cols;
            if (cellW < 1f)
                cellW = Mathf.Max(settings.minEntryWidth, 80f);

            var gridRt = DevConsoleUiUtil.CreateRect("Grid_" + section.Name, m_Content);
            var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cellW, cellH);
            grid.spacing = new Vector2(settings.spacing, settings.spacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            var gridLe = gridRt.gameObject.AddComponent<LayoutElement>();
            gridLe.flexibleWidth = 1f;

            for (int i = 0; i < section.Entries.Count; i++)
            {
                var entry = section.Entries[i];
                if (!entry.Visible)
                    continue;

                switch (entry.Kind)
                {
                    case DevConsoleEntryKind.Button:
                    {
                        var w = new DevConsoleButtonWidget(gridRt, entry, settings, m_Canvas);
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
                        var w = new DevConsoleToggleWidget(gridRt, entry, settings, m_Canvas);
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
                        var w = new DevConsoleDropdownWidget(gridRt, entry, settings, m_Canvas);
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
                        var w = new DevConsoleConfirmWidget(gridRt, entry, settings, m_Canvas);
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
                        var w = new DevConsoleReadoutWidget(gridRt, entry, settings, m_Canvas);
                        m_Readouts.Add(w);
                        break;
                    }
                }
            }
        }

        static int ResolveColumnCount(DevConsoleSettings settings, float contentWidth)
        {
            if (settings.layout == DevConsoleLayout.MobilePortrait)
                return DevConsoleUiUtil.SafeAreaWidthDp() >= 360f ? 2 : 1;

            if (settings.autoColumns)
                return DevConsoleUiUtil.ComputeAutoColumns(
                    contentWidth, settings.minEntryWidth, settings.spacing);

            return Mathf.Max(1, settings.columns);
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
