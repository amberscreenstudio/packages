#if AMBERSCREEN_DEVCONSOLE
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    /// <summary>
    /// Debug HUD canvas. Layout: PinnedStrip (top) → Sections (clickable headers) → Keys.
    /// Structure is rebuilt when the registry / key hints are dirty; values are polled at hudUpdateHz.
    /// </summary>
    sealed class DevHudView
    {
        sealed class TextSlot
        {
            public TextMeshProUGUI Tmp;
            public string Cached;
        }

        sealed class CellWidget
        {
            public GameObject Root;
            public TextSlot Value;
            public DevHudCell Cell;
        }

        sealed class RowWidget
        {
            public GameObject Root;
            public readonly List<CellWidget> Cells = new List<CellWidget>();
        }

        sealed class TableRowWidget
        {
            public GameObject Root;
            public TextSlot[] Cells;
        }

        sealed class TableWidget
        {
            public GameObject Root;
            public RectTransform Body;
            public DevHudTable Table;
            public readonly List<TableRowWidget> DataRows = new List<TableRowWidget>();
        }

        sealed class SectionWidget
        {
            public DevHudSection Section;
            public GameObject Root;
            public GameObject Header;
            public GameObject Body;
            public TextSlot Title;
            public TextSlot Chevron;
            public bool Collapsible;
            public readonly List<RowWidget> Rows = new List<RowWidget>();
            public readonly List<TableWidget> Tables = new List<TableWidget>();
        }

        readonly Transform m_Host;
        Canvas m_Canvas;
        CanvasScaler m_Scaler;
        RectTransform m_Root;
        RectTransform m_Rail;
        Image m_RailBg;
        RectTransform m_PinnedStrip;
        Image m_PinnedBg;
        RectTransform m_SectionsRoot;
        VerticalLayoutGroup m_RailLayout;

        readonly List<SectionWidget> m_StripWidgets = new List<SectionWidget>();
        readonly List<SectionWidget> m_BodyWidgets = new List<SectionWidget>();
        readonly List<string[]> m_TableScratch = new List<string[]>();

        TextMeshProUGUI m_KeysText;
        string m_CachedKeys = string.Empty;
        readonly StringBuilder m_Builder = new StringBuilder(256);
        float m_Accum;
        float m_HudWidthCanvas;
        bool m_NeedsRebuild = true;
        bool m_EventSystemPending;
        Vector2Int m_LastScreen;
        Rect m_LastSafeArea;
        string m_LastControlScheme = string.Empty;

        // Test hooks
        internal int ProviderInvokeCount { get; private set; }
        internal int SetTextCallCount { get; private set; }

        public DevHudView(Transform host)
        {
            m_Host = host;
            Build();
            ApplyVisibilityAndLayout();
            ForceRefreshContent();
        }

        public void Dispose()
        {
            if (m_Canvas != null)
                UnityEngine.Object.Destroy(m_Canvas.gameObject);
            m_Canvas = null;
            m_Root = null;
            m_Rail = null;
            m_PinnedStrip = null;
            m_SectionsRoot = null;
            m_StripWidgets.Clear();
            m_BodyWidgets.Clear();
            m_KeysText = null;
        }

        public Canvas Canvas => m_Canvas;
        public RectTransform Rail => m_Rail;
        public float HudWidthCanvas => m_HudWidthCanvas;

        /// <summary>True when the HUD has clickable section headers (needs an EventSystem).</summary>
        public bool WantsEventSystem { get; private set; }

        public void Tick(float unscaledDt)
        {
            if (m_Canvas == null)
                return;

            var settings = DevConsoleHost.Settings;
            bool show = DevHud.Visible && DevHudRegistry.HasHudContent();
            if (!show)
            {
                if (m_Rail != null && m_Rail.gameObject.activeSelf)
                    m_Rail.gameObject.SetActive(false);
                return;
            }

            if (m_EventSystemPending)
            {
                m_EventSystemPending = false;
                DevConsoleHost.EnsureEventSystemForHud();
            }

            int sw = DevHudLayout.ScreenW;
            int sh = DevHudLayout.ScreenH;
            Rect safe = DevHudLayout.LayoutSafeArea;
            if (sw != m_LastScreen.x || sh != m_LastScreen.y || safe != m_LastSafeArea)
            {
                m_LastScreen = new Vector2Int(sw, sh);
                m_LastSafeArea = safe;
                SyncScaler();
                ApplyLayout();
            }

            string scheme = CurrentControlScheme();
            bool schemeChanged = !string.Equals(scheme, m_LastControlScheme, System.StringComparison.Ordinal);
            if (schemeChanged)
                m_LastControlScheme = scheme;

            if (m_NeedsRebuild || DevHudRegistry.IsDirty || DevHudKeyHints.IsDirty)
            {
                ForceRefreshContent(); // rebuilds structure first
                ApplyLayout();
                return;
            }

            float hz = Mathf.Max(1f, settings.hudUpdateHz);
            float interval = 1f / hz;
            m_Accum += unscaledDt;
            if (m_Accum < interval && !schemeChanged)
                return;
            m_Accum = 0f;
            RefreshContent();
        }

        public void OnSettingsOrVisibilityChanged()
        {
            m_NeedsRebuild = true; // theme / font / opacity may have changed
            SyncScaler();
            ApplyVisibilityAndLayout();
            if (DevHud.Visible && DevHudRegistry.HasHudContent())
                ForceRefreshContent();
        }

        public Rect GetRailScreenRect()
        {
            if (m_Rail == null || !DevHud.Visible || !DevHudRegistry.HasHudContent())
                return Rect.zero;

            var settings = DevConsoleHost.Settings;
            float scale = Mathf.Max(
                m_Canvas != null ? m_Canvas.scaleFactor : DevHudLayout.SharedUiScaleFactor(settings),
                0.01f);
            Rect safe = DevHudLayout.LayoutSafeArea;
            float marginPx = settings.railEdgeMargin * scale;
            bool portrait = DevHudLayout.IsPortrait;
            if (portrait)
            {
                float hPx = DevHudLayout.HudStripHeightCanvasUnits(settings, scale) * scale;
                float left = safe.x + marginPx;
                float widthPx = Mathf.Max(0f, safe.width - 2f * marginPx);
                float topInsetPx = (DevHudLayout.ScreenH - (safe.y + safe.height)) + marginPx;
                float y = DevHudLayout.ScreenH - topInsetPx - hPx;
                return new Rect(left, y, widthPx, hPx);
            }

            float widthPxL = DevHudLayout.HudWidthCanvasUnits(settings, scale) * scale;
            float x = safe.x + marginPx;
            float bot = safe.y + marginPx;
            float top = DevHudLayout.ScreenH -
                        ((DevHudLayout.ScreenH - (safe.y + safe.height)) + marginPx);
            return new Rect(x, bot, widthPxL, Mathf.Max(0f, top - bot));
        }

        // ----- build ----------------------------------------------------------------------

        void Build()
        {
            var settings = DevConsoleHost.Settings;
            var theme = DevConsoleHost.ActiveTheme;
            var go = new GameObject("DevHudCanvas", typeof(RectTransform));
            go.transform.SetParent(m_Host, false);
            m_Canvas = go.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = settings.sortingOrder - 1;
            m_Scaler = go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>(); // section headers are clickable
            m_Root = go.GetComponent<RectTransform>();
            DevConsoleUiUtil.Stretch(m_Root);

            m_Rail = DevConsoleUiUtil.CreateRect("DevHudRail", m_Root);
            m_RailBg = DevConsoleUiUtil.AddImage(m_Rail, theme.hudPanelColor);
            m_RailBg.raycastTarget = false;
            m_RailLayout = m_Rail.gameObject.AddComponent<VerticalLayoutGroup>();
            m_RailLayout.childControlWidth = true;
            m_RailLayout.childControlHeight = true;
            m_RailLayout.childForceExpandWidth = true;
            m_RailLayout.childForceExpandHeight = false;
            m_RailLayout.childAlignment = TextAnchor.UpperLeft;

            m_PinnedStrip = DevConsoleUiUtil.CreateRect("HudPinnedStrip", m_Rail);
            m_PinnedBg = DevConsoleUiUtil.AddImage(m_PinnedStrip, theme.hudPinnedStripColor);
            m_PinnedBg.raycastTarget = false;
            ConfigureVertical(m_PinnedStrip.gameObject, 0f, 0f, 0f);

            m_SectionsRoot = DevConsoleUiUtil.CreateRect("HudSections", m_Rail);
            ConfigureVertical(m_SectionsRoot.gameObject, 0f, 0f, 0f);

            ApplyRailStyle(theme, settings);
            SyncScaler();
            ApplyLayout();
            m_Rail.gameObject.SetActive(false);
        }

        static VerticalLayoutGroup ConfigureVertical(GameObject go, float spacing, float padX, float padY)
        {
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(
                Mathf.RoundToInt(padX), Mathf.RoundToInt(padX),
                Mathf.RoundToInt(padY), Mathf.RoundToInt(padY));
            vlg.spacing = spacing;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;
            return vlg;
        }

        static float Opacity(DevConsoleTheme theme, DevConsoleSettings settings) =>
            Mathf.Clamp01(theme.hudOpacity) * Mathf.Clamp01(settings.hudOpacity);

        static Color WithAlpha(Color c, float mul)
        {
            c.a *= mul;
            return c;
        }

        void ApplyRailStyle(DevConsoleTheme theme, DevConsoleSettings settings)
        {
            if (m_RailBg == null)
                return;
            float opacity = Opacity(theme, settings);
            // Sprite / slicing from hudBackground, colour from hudPanelColor.
            DevConsoleTheme.ApplySurface(m_RailBg, theme.hudBackground);
            m_RailBg.color = WithAlpha(theme.hudPanelColor, opacity);
            if (m_PinnedBg != null)
                m_PinnedBg.color = WithAlpha(theme.hudPinnedStripColor, opacity);

            int pad = Mathf.RoundToInt(theme.padding);
            m_RailLayout.padding = new RectOffset(pad, pad, pad, pad);
            m_RailLayout.spacing = theme.spacing;
            if (m_PinnedStrip != null)
            {
                var vlg = m_PinnedStrip.GetComponent<VerticalLayoutGroup>();
                int p = Mathf.RoundToInt(theme.hudCellPadding);
                vlg.padding = new RectOffset(p, p, p, p);
            }
        }

        void SyncScaler()
        {
            if (m_Scaler == null || m_Canvas == null)
                return;
            var settings = DevConsoleHost.Settings;
            m_Canvas.sortingOrder = settings.sortingOrder - 1;
            m_Scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            m_Scaler.scaleFactor = DevHudLayout.SharedUiScaleFactor(settings);
            ApplyLayoutScreenCanvasSize();
        }

        void ApplyLayoutScreenCanvasSize()
        {
            if (DevConsoleView.s_LayoutScreenWidth <= 0 || m_Root == null || m_Canvas == null)
                return;
            float sf = m_Scaler != null ? Mathf.Max(m_Scaler.scaleFactor, 0.01f) : 1f;
            m_Canvas.renderMode = RenderMode.WorldSpace;
            m_Root.anchorMin = new Vector2(0.5f, 0.5f);
            m_Root.anchorMax = new Vector2(0.5f, 0.5f);
            m_Root.pivot = new Vector2(0.5f, 0.5f);
            m_Root.anchoredPosition3D = Vector3.zero;
            m_Root.localScale = Vector3.one;
            m_Root.sizeDelta = new Vector2(
                DevConsoleView.s_LayoutScreenWidth / sf,
                DevConsoleView.s_LayoutScreenHeight / sf);
        }

        void ApplyVisibilityAndLayout()
        {
            SyncScaler();
            ApplyLayout();
            bool show = DevHud.Visible && DevHudRegistry.HasHudContent();
            if (m_Rail != null)
                m_Rail.gameObject.SetActive(show);
            ApplyRailStyle(DevConsoleHost.ActiveTheme, DevConsoleHost.Settings);
        }

        void ApplyLayout()
        {
            if (m_Rail == null)
                return;
            var settings = DevConsoleHost.Settings;
            float scale = Mathf.Max(m_Canvas != null ? m_Canvas.scaleFactor : 1f, 0.01f);
            Rect safe = DevHudLayout.LayoutSafeArea;
            bool portrait = DevHudLayout.IsPortrait;
            float margin = settings.railEdgeMargin;
            float width = DevHudLayout.HudWidthCanvasUnits(settings, scale);
            m_HudWidthCanvas = width;

            float top = (DevHudLayout.ScreenH - (safe.y + safe.height)) / scale + margin;
            float bot = safe.y / scale + margin;
            float left = safe.x / scale + margin;
            float right = (DevHudLayout.ScreenW - (safe.x + safe.width)) / scale + margin;

            if (portrait)
            {
                float h = DevHudLayout.HudStripHeightCanvasUnits(settings, scale);
                m_Rail.anchorMin = new Vector2(0f, 1f);
                m_Rail.anchorMax = new Vector2(1f, 1f);
                m_Rail.pivot = new Vector2(0.5f, 1f);
                m_Rail.offsetMin = new Vector2(left, -top - h);
                m_Rail.offsetMax = new Vector2(-right, -top);
            }
            else
            {
                m_Rail.anchorMin = new Vector2(0f, 0f);
                m_Rail.anchorMax = new Vector2(0f, 1f);
                m_Rail.pivot = new Vector2(0f, 0.5f);
                m_Rail.offsetMin = new Vector2(left, bot);
                m_Rail.offsetMax = new Vector2(left + width, -top);
            }
        }

        // ----- structure ------------------------------------------------------------------

        static void DestroyObject(GameObject go)
        {
            if (go == null)
                return;
            go.SetActive(false); // gone from layout / Find immediately, even if Destroy is deferred
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(go);
            else
                UnityEngine.Object.DestroyImmediate(go);
        }

        static void ClearChildren(RectTransform parent)
        {
            if (parent == null)
                return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                DestroyObject(parent.GetChild(i).gameObject);
        }

        void EnsureStructure()
        {
            if (!m_NeedsRebuild && !DevHudRegistry.IsDirty && !DevHudKeyHints.IsDirty)
                return;
            RebuildStructure();
            m_NeedsRebuild = false;
            DevHudRegistry.ClearDirty();
            DevHudKeyHints.ClearDirty();
        }

        float HudFontSize(DevConsoleTheme theme, DevConsoleSettings settings) =>
            theme.hudFontSize > 0f ? theme.hudFontSize : settings.hudFontSize;

        void RebuildStructure()
        {
            if (m_Rail == null)
                return;

            DevHudRegistry.EnsureSorted();
            var settings = DevConsoleHost.Settings;
            var theme = DevConsoleHost.ActiveTheme;

            ApplyRailStyle(theme, settings);
            ClearChildren(m_PinnedStrip);
            ClearChildren(m_SectionsRoot);
            m_StripWidgets.Clear();
            m_BodyWidgets.Clear();

            bool clickable = false;
            var sections = DevHudRegistry.Sections;
            for (int i = 0; i < sections.Count; i++)
            {
                var strip = BuildSectionWidget(m_PinnedStrip, sections[i], true, theme, settings);
                if (strip != null)
                    m_StripWidgets.Add(strip);
                var body = BuildSectionWidget(m_SectionsRoot, sections[i], false, theme, settings);
                if (body != null)
                {
                    m_BodyWidgets.Add(body);
                    clickable |= body.Collapsible;
                }
            }

            WantsEventSystem = clickable;

            if (m_KeysText == null)
            {
                var rt = DevConsoleUiUtil.CreateRect("HudKeys", m_Rail);
                var le = rt.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 16f;
                le.flexibleWidth = 1f;
                m_KeysText = rt.gameObject.AddComponent<TextMeshProUGUI>();
                DevConsoleUiUtil.StyleLabel(m_KeysText, false);
                m_KeysText.alignment = TextAlignmentOptions.TopLeft;
                m_KeysText.richText = false;
                m_KeysText.raycastTarget = false;
                m_KeysText.textWrappingMode = TextWrappingModes.Normal;
                m_KeysText.overflowMode = TextOverflowModes.Overflow;
            }

            if (theme.hudFont != null)
                m_KeysText.font = theme.hudFont;
            m_KeysText.fontSize = HudFontSize(theme, settings);
            m_KeysText.color = theme.hudLabelColor;
            m_PinnedStrip.SetSiblingIndex(0);
            m_SectionsRoot.SetSiblingIndex(1);
            m_KeysText.transform.SetAsLastSibling();
            m_CachedKeys = string.Empty;

            // Deferred to Tick: this can run inside DevConsoleHost.Bootstrap.
            m_EventSystemPending = WantsEventSystem;
        }

        SectionWidget BuildSectionWidget(
            RectTransform parent,
            DevHudSection sec,
            bool strip,
            DevConsoleTheme theme,
            DevConsoleSettings settings)
        {
            bool pinnedSection = sec.Pinned;
            if (strip && !pinnedSection && !HasPinnedCell(sec))
                return null;
            if (!strip && pinnedSection)
                return null;

            var w = new SectionWidget { Section = sec };
            var rootRt = DevConsoleUiUtil.CreateRect("HudSection", parent);
            w.Root = rootRt.gameObject;
            ConfigureVertical(w.Root, 0f, 0f, 0f);

            bool showHeader = !strip || pinnedSection;
            w.Collapsible = !strip;
            if (showHeader)
                BuildHeader(rootRt, w, sec, theme, settings);

            var bodyRt = DevConsoleUiUtil.CreateRect("HudSectionBody", rootRt);
            w.Body = bodyRt.gameObject;
            ConfigureVertical(w.Body, 0f, 0f, 0f);

            for (int r = 0; r < sec.Rows.Count; r++)
            {
                var row = BuildRowWidget(bodyRt, sec.Rows[r], strip, pinnedSection, theme, settings);
                if (row != null)
                    w.Rows.Add(row);
            }

            if (!strip || pinnedSection)
            {
                for (int t = 0; t < sec.Tables.Count; t++)
                    w.Tables.Add(BuildTableWidget(bodyRt, sec.Tables[t], theme, settings));
            }

            if (w.Rows.Count == 0 && w.Tables.Count == 0)
            {
                DestroyObject(w.Root);
                return null;
            }

            return w;
        }

        static bool HasPinnedCell(DevHudSection sec)
        {
            for (int r = 0; r < sec.Rows.Count; r++)
            {
                var cells = sec.Rows[r].Cells;
                for (int c = 0; c < cells.Count; c++)
                {
                    if (cells[c].IsPinned)
                        return true;
                }
            }

            return false;
        }

        void BuildHeader(
            RectTransform sectionRoot,
            SectionWidget w,
            DevHudSection sec,
            DevConsoleTheme theme,
            DevConsoleSettings settings)
        {
            float pad = theme.hudCellPadding;
            float headerSize = Mathf.Max(8f, theme.hudHeaderFontSize);

            var rt = DevConsoleUiUtil.CreateRect("HudSectionHeader", sectionRoot);
            w.Header = rt.gameObject;
            var bg = DevConsoleUiUtil.AddImage(rt, HeaderColor(sec, theme, settings));
            bg.raycastTarget = w.Collapsible;

            var le = w.Header.AddComponent<LayoutElement>();
            le.minHeight = headerSize + pad * 2f;

            var hlg = w.Header.AddComponent<HorizontalLayoutGroup>();
            int px = Mathf.RoundToInt(pad * 2f);
            int py = Mathf.RoundToInt(pad);
            hlg.padding = new RectOffset(px, px, py, py);
            hlg.spacing = pad;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            if (sec.Icon != null)
            {
                var iconRt = DevConsoleUiUtil.CreateRect("HudSectionIcon", rt);
                var icon = iconRt.gameObject.AddComponent<Image>();
                icon.sprite = sec.Icon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var iconLe = iconRt.gameObject.AddComponent<LayoutElement>();
                iconLe.minWidth = headerSize;
                iconLe.minHeight = headerSize;
                iconLe.preferredWidth = headerSize;
                iconLe.preferredHeight = headerSize;
                iconLe.flexibleWidth = 0f;
            }

            w.Title = CreateText(rt, "HudSectionTitle", headerSize, theme.hudLabelColor, true, false, theme);
            var titleLe = w.Title.Tmp.gameObject.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;
            titleLe.minWidth = 0f;
            SetSlot(w.Title, sec.Title);

            if (w.Collapsible)
            {
                w.Chevron = CreateText(rt, "HudSectionChevron", headerSize, theme.hudLabelColor, true, false, theme);
                w.Chevron.Tmp.alignment = TextAlignmentOptions.MidlineRight;
                w.Chevron.Tmp.overflowMode = TextOverflowModes.Overflow;
                var chevLe = w.Chevron.Tmp.gameObject.AddComponent<LayoutElement>();
                chevLe.minWidth = headerSize;
                chevLe.flexibleWidth = 0f;

                var button = w.Header.AddComponent<Button>();
                button.targetGraphic = bg;
                button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                var captured = sec;
                button.onClick.AddListener(() => DevHudRegistry.ToggleCollapsed(captured));
            }
        }

        static Color HeaderColor(DevHudSection sec, DevConsoleTheme theme, DevConsoleSettings settings)
        {
            Color c = theme.hudSectionHeaderColor;
            if (sec.Tint.HasValue)
            {
                Color t = sec.Tint.Value;
                c = new Color(
                    Mathf.Lerp(c.r, t.r, 0.6f),
                    Mathf.Lerp(c.g, t.g, 0.6f),
                    Mathf.Lerp(c.b, t.b, 0.6f),
                    c.a);
            }

            return WithAlpha(c, Opacity(theme, settings));
        }

        RowWidget BuildRowWidget(
            RectTransform parent,
            DevHudRow row,
            bool strip,
            bool pinnedSection,
            DevConsoleTheme theme,
            DevConsoleSettings settings)
        {
            // Pinned cells render only in the strip; everything else renders in the section body.
            var included = new List<DevHudCell>(row.Cells.Count);
            for (int c = 0; c < row.Cells.Count; c++)
            {
                var cell = row.Cells[c];
                bool inStrip = pinnedSection || cell.IsPinned;
                if (inStrip == strip)
                    included.Add(cell);
            }

            if (included.Count == 0)
                return null;

            float pad = theme.hudCellPadding;
            var rt = DevConsoleUiUtil.CreateRect("HudRow", parent);
            var w = new RowWidget { Root = rt.gameObject };
            var hlg = w.Root.AddComponent<HorizontalLayoutGroup>();
            int px = Mathf.RoundToInt(pad);
            int py = Mathf.RoundToInt(pad * 0.5f);
            hlg.padding = new RectOffset(px, px, py, py);
            hlg.spacing = pad;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.UpperLeft;

            float size = HudFontSize(theme, settings);
            for (int c = 0; c < included.Count; c++)
            {
                var cell = included[c];
                var cellRt = DevConsoleUiUtil.CreateRect("HudCell", rt);
                var le = cellRt.gameObject.AddComponent<LayoutElement>();
                le.minWidth = 0f;
                le.preferredWidth = 0f;
                le.flexibleWidth = Mathf.Max(0.01f, cell.Weight);

                var cellHlg = cellRt.gameObject.AddComponent<HorizontalLayoutGroup>();
                cellHlg.spacing = pad;
                cellHlg.childControlWidth = true;
                cellHlg.childControlHeight = true;
                cellHlg.childForceExpandWidth = false;
                cellHlg.childForceExpandHeight = false;
                cellHlg.childAlignment = TextAnchor.UpperLeft;

                var cw = new CellWidget { Root = cellRt.gameObject, Cell = cell };
                if (!string.IsNullOrEmpty(cell.Label))
                {
                    var label = CreateText(cellRt, "HudCellLabel", size, theme.hudLabelColor, false, false, theme);
                    var labelLe = label.Tmp.gameObject.AddComponent<LayoutElement>();
                    labelLe.flexibleWidth = 0f;
                    SetSlot(label, cell.Label + ":");
                }

                cw.Value = CreateText(cellRt, "HudCellValue", size, theme.hudValueColor, false, true, theme);
                var valueLe = cw.Value.Tmp.gameObject.AddComponent<LayoutElement>();
                valueLe.flexibleWidth = 1f;
                valueLe.minWidth = 0f;
                w.Cells.Add(cw);
            }

            return w;
        }

        TableWidget BuildTableWidget(
            RectTransform parent,
            DevHudTable table,
            DevConsoleTheme theme,
            DevConsoleSettings settings)
        {
            var rt = DevConsoleUiUtil.CreateRect("HudTable", parent);
            ConfigureVertical(rt.gameObject, 0f, 0f, 0f);
            var w = new TableWidget { Root = rt.gameObject, Table = table, Body = rt };

            var header = CreateTableRow(rt, "HudTableHeader", table.Columns.Length, theme, settings, true);
            for (int c = 0; c < table.Columns.Length; c++)
                SetSlot(header.Cells[c], table.Columns[c] ?? string.Empty);
            return w;
        }

        TableRowWidget CreateTableRow(
            RectTransform parent,
            string name,
            int columns,
            DevConsoleTheme theme,
            DevConsoleSettings settings,
            bool isHeader)
        {
            float pad = theme.hudCellPadding;
            var rt = DevConsoleUiUtil.CreateRect(name, parent);
            var hlg = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            int px = Mathf.RoundToInt(pad);
            int py = Mathf.RoundToInt(pad * 0.5f);
            hlg.padding = new RectOffset(px, px, py, py);
            hlg.spacing = pad;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.UpperLeft;

            var row = new TableRowWidget { Root = rt.gameObject, Cells = new TextSlot[columns] };
            float size = HudFontSize(theme, settings);
            for (int c = 0; c < columns; c++)
            {
                var slot = CreateText(
                    rt,
                    isHeader ? "HudTableHeaderCell" : "HudTableCell",
                    size,
                    isHeader ? DevConsoleUiUtil.Muted : theme.hudValueColor,
                    isHeader,
                    true,
                    theme);
                var le = slot.Tmp.gameObject.AddComponent<LayoutElement>();
                le.minWidth = 0f;
                le.preferredWidth = 0f; // equal column widths: only flexibleWidth distributes space
                le.flexibleWidth = 1f;
                row.Cells[c] = slot;
            }

            return row;
        }

        TextSlot CreateText(
            RectTransform parent,
            string name,
            float size,
            Color color,
            bool semiBold,
            bool wrap,
            DevConsoleTheme theme)
        {
            var rt = DevConsoleUiUtil.CreateRect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            DevConsoleUiUtil.StyleLabel(tmp, semiBold);
            if (theme.hudFont != null)
                tmp.font = theme.hudFont;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.richText = false;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            return new TextSlot { Tmp = tmp };
        }

        // ----- refresh --------------------------------------------------------------------

        void ForceRefreshContent()
        {
            m_Accum = 0f;
            RefreshContent();
        }

        void SetSlot(TextSlot slot, string text)
        {
            if (slot == null || slot.Tmp == null)
                return;
            if (string.Equals(slot.Cached, text, System.StringComparison.Ordinal))
                return;
            slot.Cached = text;
            slot.Tmp.SetText(text);
            SetTextCallCount++;
        }

        static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }

        void RefreshContent()
        {
            if (!DevHud.Visible)
                return;
            if (!DevHudRegistry.HasHudContent())
                return;

            EnsureStructure();

            bool anyStrip = false;
            for (int i = 0; i < m_StripWidgets.Count; i++)
                anyStrip |= RefreshSection(m_StripWidgets[i]);
            SetActive(m_PinnedStrip != null ? m_PinnedStrip.gameObject : null, anyStrip);

            bool anyBody = false;
            for (int i = 0; i < m_BodyWidgets.Count; i++)
                anyBody |= RefreshSection(m_BodyWidgets[i]);

            bool anyVisible = anyStrip || anyBody;
            RefreshKeysSection(ref anyVisible);

            if (m_Rail != null)
                m_Rail.gameObject.SetActive(anyVisible);
        }

        bool RefreshSection(SectionWidget w)
        {
            var sec = w.Section;
            bool collapsed = w.Collapsible && sec.Collapsed;
            bool any;

            if (collapsed)
            {
                // Collapsed sections are not polled; they keep their header if they have content.
                any = w.Rows.Count > 0 || w.Tables.Count > 0;
                SetActive(w.Body, false);
            }
            else
            {
                any = false;
                for (int r = 0; r < w.Rows.Count; r++)
                    any |= RefreshRow(w.Rows[r]);
                for (int t = 0; t < w.Tables.Count; t++)
                    any |= RefreshTable(w.Tables[t]);
                SetActive(w.Body, true);
            }

            SetActive(w.Root, any);
            if (any)
            {
                SetSlot(w.Title, sec.Title);
                SetSlot(w.Chevron, collapsed ? "+" : "-");
            }

            return any;
        }

        bool RefreshRow(RowWidget row)
        {
            bool any = false;
            for (int c = 0; c < row.Cells.Count; c++)
            {
                var cw = row.Cells[c];
                string value = EvaluateCell(cw.Cell);
                bool has = !string.IsNullOrEmpty(value);
                SetActive(cw.Root, has);
                if (has)
                {
                    SetSlot(cw.Value, value);
                    any = true;
                }
            }

            SetActive(row.Root, any);
            return any;
        }

        string EvaluateCell(DevHudCell cell)
        {
            if (cell.Value == null)
                return null;
            try
            {
                ProviderInvokeCount++;
                return cell.Value.Invoke();
            }
            catch
            {
                return "err";
            }
        }

        bool RefreshTable(TableWidget w)
        {
            m_TableScratch.Clear();
            try
            {
                ProviderInvokeCount++;
                var data = w.Table.Rows.Invoke();
                if (data != null)
                {
                    foreach (var row in data)
                    {
                        if (row != null)
                            m_TableScratch.Add(row);
                    }
                }
            }
            catch
            {
                m_TableScratch.Clear();
                m_TableScratch.Add(new[] { "err" });
            }

            int cols = w.Table.Columns.Length;
            int count = m_TableScratch.Count;
            if (count > 0 && w.DataRows.Count < count)
            {
                var theme = DevConsoleHost.ActiveTheme;
                var settings = DevConsoleHost.Settings;
                while (w.DataRows.Count < count)
                    w.DataRows.Add(CreateTableRow(w.Body, "HudTableRow", cols, theme, settings, false));
            }

            for (int r = 0; r < w.DataRows.Count; r++)
            {
                var rowWidget = w.DataRows[r];
                bool active = r < count;
                SetActive(rowWidget.Root, active);
                if (!active)
                    continue;

                var values = m_TableScratch[r];
                for (int c = 0; c < cols; c++)
                    SetSlot(rowWidget.Cells[c], c < values.Length ? values[c] ?? string.Empty : string.Empty);
            }

            bool any = count > 0;
            SetActive(w.Root, any);
            return any;
        }

        void RefreshKeysSection(ref bool anyVisible)
        {
            if (m_KeysText == null)
                return;

            if (!DevHudKeyHints.TryGetActiveHints(out var hints))
            {
                if (m_KeysText.gameObject.activeSelf)
                    m_KeysText.gameObject.SetActive(false);
                m_CachedKeys = string.Empty;
                return;
            }

            m_Builder.Length = 0;
            m_Builder.Append("Keys");
            for (int i = 0; i < hints.Count; i++)
            {
                m_Builder.Append('\n');
                m_Builder.Append(DevHudKeyHints.FormatHint(hints[i]));
            }

            string built = m_Builder.ToString();
            anyVisible = true;
            if (!m_KeysText.gameObject.activeSelf)
                m_KeysText.gameObject.SetActive(true);
            if (string.Equals(built, m_CachedKeys, System.StringComparison.Ordinal))
                return;
            m_CachedKeys = built;
            m_KeysText.SetText(built);
            SetTextCallCount++;
        }

        static string CurrentControlScheme()
        {
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null)
                return "Gamepad:" + pad.deviceId;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
                return "Keyboard:" + kb.deviceId;
            return string.Empty;
        }

        internal void ResetTestCounters()
        {
            ProviderInvokeCount = 0;
            SetTextCallCount = 0;
        }
    }
}
#endif
