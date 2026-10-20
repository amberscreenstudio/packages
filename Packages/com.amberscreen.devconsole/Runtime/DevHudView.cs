#if AMBERSCREEN_DEVCONSOLE
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    sealed class DevHudView
    {
        readonly Transform m_Host;
        Canvas m_Canvas;
        CanvasScaler m_Scaler;
        RectTransform m_Root;
        RectTransform m_Rail;
        Image m_RailBg;
        readonly List<TextMeshProUGUI> m_SectionTexts = new List<TextMeshProUGUI>();
        readonly List<string> m_CachedTexts = new List<string>();
        readonly StringBuilder m_Builder = new StringBuilder(256);
        float m_Accum;
        float m_HudWidthCanvas;
        Vector2Int m_LastScreen;
        Rect m_LastSafeArea;

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
            m_SectionTexts.Clear();
            m_CachedTexts.Clear();
        }

        public Canvas Canvas => m_Canvas;
        public RectTransform Rail => m_Rail;
        public float HudWidthCanvas => m_HudWidthCanvas;

        public void Tick(float unscaledDt)
        {
            if (m_Canvas == null)
                return;

            var settings = DevConsoleHost.Settings;
            bool show = DevHud.Visible && DevHudRegistry.TotalLineCount() > 0;
            if (!show)
            {
                if (m_Rail != null && m_Rail.gameObject.activeSelf)
                    m_Rail.gameObject.SetActive(false);
                return;
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

            if (DevHudRegistry.IsDirty)
            {
                RebuildStructure();
                DevHudRegistry.ClearDirty();
                ForceRefreshContent();
                ApplyLayout();
                return;
            }

            float hz = Mathf.Max(1f, settings.hudUpdateHz);
            float interval = 1f / hz;
            m_Accum += unscaledDt;
            if (m_Accum < interval)
                return;
            m_Accum = 0f;
            RefreshContent();
        }

        public void OnSettingsOrVisibilityChanged()
        {
            SyncScaler();
            ApplyVisibilityAndLayout();
            if (DevHud.Visible && DevHudRegistry.TotalLineCount() > 0)
                ForceRefreshContent();
        }

        public Rect GetRailScreenRect()
        {
            if (m_Rail == null || !DevHud.Visible || DevHudRegistry.TotalLineCount() == 0)
                return Rect.zero;

            var settings = DevConsoleHost.Settings;
            float scale = Mathf.Max(
                m_Canvas != null ? m_Canvas.scaleFactor : DevHudLayout.SharedUiScaleFactor(settings),
                0.01f);
            Rect safe = DevHudLayout.LayoutSafeArea;
            float widthCu = DevHudLayout.HudWidthCanvasUnits(settings, scale);
            float widthPx = widthCu * scale;
            float marginPx = settings.railEdgeMargin * scale;
            float rightInsetPx = (DevHudLayout.ScreenW - (safe.x + safe.width)) + marginPx;
            float x = DevHudLayout.ScreenW - rightInsetPx - widthPx;
            bool portrait = DevHudLayout.ScreenH > DevHudLayout.ScreenW;
            if (portrait)
            {
                float hPx = safe.height * Mathf.Clamp01(settings.hudPortraitMaxHeightFraction);
                float topInsetPx = (DevHudLayout.ScreenH - (safe.y + safe.height)) + marginPx;
                float y = DevHudLayout.ScreenH - topInsetPx - hPx;
                return new Rect(x, y, widthPx, hPx);
            }

            float bot = safe.y + marginPx;
            float top = DevHudLayout.ScreenH -
                        ((DevHudLayout.ScreenH - (safe.y + safe.height)) + marginPx);
            return new Rect(x, bot, widthPx, Mathf.Max(0f, top - bot));
        }

        void Build()
        {
            var settings = DevConsoleHost.Settings;
            var go = new GameObject("DevHudCanvas", typeof(RectTransform));
            go.transform.SetParent(m_Host, false);
            m_Canvas = go.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = settings.sortingOrder - 1;
            m_Scaler = go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>().enabled = false;
            m_Root = go.GetComponent<RectTransform>();
            DevConsoleUiUtil.Stretch(m_Root);

            m_Rail = DevConsoleUiUtil.CreateRect("DevHudRail", m_Root);
            m_RailBg = DevConsoleUiUtil.AddImage(m_Rail, new Color(0.05f, 0.06f, 0.08f, settings.hudOpacity));
            m_RailBg.raycastTarget = false;
            var vlg = m_Rail.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 4f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;

            SyncScaler();
            ApplyLayout();
            m_Rail.gameObject.SetActive(false);
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
            bool show = DevHud.Visible && DevHudRegistry.TotalLineCount() > 0;
            if (m_Rail != null)
                m_Rail.gameObject.SetActive(show);
            if (m_RailBg != null)
            {
                var c = m_RailBg.color;
                c.a = DevConsoleHost.Settings.hudOpacity;
                m_RailBg.color = c;
            }
        }

        void ApplyLayout()
        {
            if (m_Rail == null)
                return;
            var settings = DevConsoleHost.Settings;
            float scale = Mathf.Max(m_Canvas != null ? m_Canvas.scaleFactor : 1f, 0.01f);
            Rect safe = DevHudLayout.LayoutSafeArea;
            bool portrait = DevHudLayout.ScreenH > DevHudLayout.ScreenW;
            float margin = settings.railEdgeMargin;
            float width = DevHudLayout.HudWidthCanvasUnits(settings, scale);
            m_HudWidthCanvas = width;

            float top = (DevHudLayout.ScreenH - (safe.y + safe.height)) / scale + margin;
            float bot = safe.y / scale + margin;
            float right = (DevHudLayout.ScreenW - (safe.x + safe.width)) / scale + margin;

            m_Rail.anchorMin = new Vector2(1f, 1f);
            m_Rail.anchorMax = new Vector2(1f, 1f);
            m_Rail.pivot = new Vector2(1f, 1f);

            if (portrait)
            {
                float canvasH = safe.height / scale;
                float h = canvasH * Mathf.Clamp01(settings.hudPortraitMaxHeightFraction);
                m_Rail.offsetMin = new Vector2(-width - right, -top - h);
                m_Rail.offsetMax = new Vector2(-right, -top);
            }
            else
            {
                m_Rail.anchorMin = new Vector2(1f, 0f);
                m_Rail.anchorMax = new Vector2(1f, 1f);
                m_Rail.pivot = new Vector2(1f, 0.5f);
                m_Rail.offsetMin = new Vector2(-width - right, bot);
                m_Rail.offsetMax = new Vector2(-right, -top);
            }
        }

        void RebuildStructure()
        {
            DevHudRegistry.EnsureSorted();
            var sections = DevHudRegistry.Sections;
            var settings = DevConsoleHost.Settings;

            while (m_SectionTexts.Count > sections.Count)
            {
                int last = m_SectionTexts.Count - 1;
                if (m_SectionTexts[last] != null)
                    UnityEngine.Object.Destroy(m_SectionTexts[last].gameObject);
                m_SectionTexts.RemoveAt(last);
                m_CachedTexts.RemoveAt(last);
            }

            while (m_SectionTexts.Count < sections.Count)
            {
                var rt = DevConsoleUiUtil.CreateRect("HudSection", m_Rail);
                var le = rt.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 16f;
                le.flexibleWidth = 1f;
                var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
                DevConsoleUiUtil.StyleLabel(tmp, false);
                tmp.fontSize = settings.hudFontSize;
                tmp.color = DevConsoleUiUtil.TextColor;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.richText = false;
                tmp.raycastTarget = false;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.overflowMode = TextOverflowModes.Overflow;
                m_SectionTexts.Add(tmp);
                m_CachedTexts.Add(string.Empty);
            }

            for (int i = 0; i < m_SectionTexts.Count; i++)
            {
                m_SectionTexts[i].fontSize = settings.hudFontSize;
                m_CachedTexts[i] = string.Empty;
            }
        }

        void ForceRefreshContent()
        {
            m_Accum = 0f;
            RefreshContent();
        }

        void RefreshContent()
        {
            if (!DevHud.Visible || DevHudRegistry.TotalLineCount() == 0)
                return;

            var sections = DevHudRegistry.Sections;
            if (m_SectionTexts.Count != sections.Count)
                RebuildStructure();

            for (int s = 0; s < sections.Count; s++)
            {
                var sec = sections[s];
                m_Builder.Length = 0;
                m_Builder.Append(sec.Name);
                var lines = sec.Lines;
                for (int i = 0; i < lines.Count; i++)
                {
                    m_Builder.Append('\n');
                    m_Builder.Append(lines[i].Label);
                    m_Builder.Append(": ");
                    string value;
                    try
                    {
                        ProviderInvokeCount++;
                        value = lines[i].Value?.Invoke() ?? "-";
                    }
                    catch
                    {
                        value = "err";
                    }

                    m_Builder.Append(value);
                }

                string cached = s < m_CachedTexts.Count ? m_CachedTexts[s] : null;
                if (BuilderEquals(m_Builder, cached))
                    continue;

                string built = m_Builder.ToString();
                if (s < m_CachedTexts.Count)
                    m_CachedTexts[s] = built;
                if (s < m_SectionTexts.Count && m_SectionTexts[s] != null)
                {
                    m_SectionTexts[s].SetText(built);
                    SetTextCallCount++;
                }
            }

            if (m_Rail != null && !m_Rail.gameObject.activeSelf)
                m_Rail.gameObject.SetActive(true);
        }

        static bool BuilderEquals(StringBuilder sb, string s)
        {
            if (sb == null)
                return s == null;
            if (s == null)
                return false;
            if (sb.Length != s.Length)
                return false;
            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] != s[i])
                    return false;
            }

            return true;
        }

        internal void ResetTestCounters()
        {
            ProviderInvokeCount = 0;
            SetTextCallCount = 0;
        }
    }
}
#endif
