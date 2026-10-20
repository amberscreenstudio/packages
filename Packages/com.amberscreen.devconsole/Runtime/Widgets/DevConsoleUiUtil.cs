#if AMBERSCREEN_DEVCONSOLE
using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    static class DevConsoleUiUtil
    {
        const string InterRegularResource = "AmberScreenDevConsole/Inter-Regular";
        const string InterSemiBoldResource = "AmberScreenDevConsole/Inter-SemiBold";

        static TMP_FontAsset s_Regular;
        static TMP_FontAsset s_SemiBold;
        static bool s_WarnedMissingFont;
        static bool s_LoggedFont;
        static string s_FontLogLabel = "TMP default (Inter not found)";

        public static void ResetFontCache()
        {
            s_Regular = null;
            s_SemiBold = null;
            s_WarnedMissingFont = false;
            s_LoggedFont = false;
            s_FontLogLabel = "TMP default (Inter not found)";
        }

        public static TMP_FontAsset ResolveFont(DevConsoleSettings settings = null) =>
            ResolveRegular(settings);

        public static TMP_FontAsset Font => ResolveRegular();

        public static TMP_FontAsset FontSemiBold => ResolveSemiBold();

        public static DevConsoleTheme Theme => DevConsoleHost.ActiveTheme;

        public static TMP_FontAsset ResolveRegular(DevConsoleSettings settings = null)
        {
            if (s_Regular != null)
                return s_Regular;

            settings ??= DevConsoleSettings.LoadOrDefault();
            var theme = Theme;
            if (theme != null && theme.fontRegular != null)
            {
                s_Regular = theme.fontRegular;
                s_FontLogLabel = "theme " + theme.fontRegular.name;
                return s_Regular;
            }

            if (settings.fontOverride != null)
            {
                s_Regular = settings.fontOverride;
                s_FontLogLabel = "override " + settings.fontOverride.name;
                return s_Regular;
            }

            var bundled = Resources.Load<Font>(InterRegularResource);
            if (bundled != null)
            {
                s_Regular = CreateDynamicFontAsset(bundled);
                if (s_Regular != null)
                    s_FontLogLabel = "Inter (bundled)";
            }

            if (s_Regular == null)
            {
                try { s_Regular = TMP_Settings.GetFontAsset(); }
                catch { /* TMP Essential Resources may be missing */ }
                if (s_Regular != null)
                    s_FontLogLabel = "TMP default (Inter not found)";
            }

            if (s_Regular == null && !s_WarnedMissingFont)
            {
                s_WarnedMissingFont = true;
                Debug.LogWarning(
                    "[AmberScreen DevConsole] No font available. Ensure Inter is under " +
                    "Runtime/Resources/AmberScreenDevConsole/ or import TMP Essential Resources.");
            }

            return s_Regular;
        }

        public static TMP_FontAsset ResolveSemiBold(DevConsoleSettings settings = null)
        {
            if (s_SemiBold != null)
                return s_SemiBold;

            settings ??= DevConsoleSettings.LoadOrDefault();
            var theme = Theme;
            if (theme != null && theme.fontBold != null)
            {
                s_SemiBold = theme.fontBold;
                return s_SemiBold;
            }

            if (settings.fontOverride != null)
            {
                s_SemiBold = settings.fontOverride;
                return s_SemiBold;
            }

            var bundled = Resources.Load<Font>(InterSemiBoldResource);
            if (bundled != null)
                s_SemiBold = CreateDynamicFontAsset(bundled);

            if (s_SemiBold == null)
                s_SemiBold = ResolveRegular(settings);

            return s_SemiBold;
        }

        public static void LogFontOnce()
        {
            if (s_LoggedFont)
                return;
            ResolveRegular();
            s_LoggedFont = true;
            Debug.Log("[DevConsole] Font: " + s_FontLogLabel);
        }

        static TMP_FontAsset CreateDynamicFontAsset(Font font)
        {
            if (font == null)
                return null;

            try
            {
                if (TMP_Settings.instance == null)
                {
                    DevConsoleHost.ReportTmpEssentialsMissing();
                    return null;
                }

                var asset = TMP_FontAsset.CreateFontAsset(
                    font,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic);
                if (asset == null)
                    DevConsoleHost.ReportTmpEssentialsMissing();
                return asset;
            }
            catch (Exception)
            {
                DevConsoleHost.ReportTmpEssentialsMissing();
                return null;
            }
        }

        public static void ApplyCrisp(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;
            tmp.fontStyle = FontStyles.Normal;
            tmp.extraPadding = false;
            tmp.characterSpacing = 0f;
        }

        public static void StyleLabel(TextMeshProUGUI tmp, bool semiBold = false)
        {
            if (tmp == null)
                return;
            var font = semiBold ? FontSemiBold : Font;
            if (font != null)
                tmp.font = font;
            ApplyCrisp(tmp);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
        }

        public static int ComputeAutoColumns(float contentWidth, float minEntryWidth, float spacing)
        {
            float denom = minEntryWidth + spacing;
            if (contentWidth <= 0f || denom <= 0f)
                return 1;
            return Mathf.Max(1, Mathf.FloorToInt(contentWidth / denom));
        }

        public static Color PanelBg => Theme.panelBackground.color;
        public static Color RailBg => Theme.railBackground.color;
        public static Color WindowBg => Theme.windowBackground.color;
        public static Color RowBg => Theme.rowBackground.color;
        public static Color ButtonBg => Theme.buttonNormal;
        public static Color ButtonDisabled => Theme.buttonDisabled;
        public static Color TextColor => Theme.labelColor;
        public static Color ValueColor => Theme.valueColor;
        public static Color Accent => Theme.accentColor;
        public static Color Muted => Theme.mutedColor;
        public static Color ChromeButtonBg => Theme.chromeButtonBg;
        public static Color ChromeButtonHighlight => Theme.chromeButtonHighlight;
        public static Color ChromeCloseHover => Theme.chromeCloseHover;

        public static float DpToCanvas(Canvas canvas)
        {
            float dpi = ResolvedDpi();
            if (dpi <= 0f)
                dpi = 160f;
            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            return (dpi / 160f) / scale;
        }

        public static float SafeAreaWidthDp()
        {
            float dpi = ResolvedDpi();
            if (dpi <= 0f)
                dpi = 160f;
            float width = DevConsoleView.s_LayoutScreenWidth > 0
                ? DevConsoleView.s_LayoutScreenWidth
                : Screen.safeArea.width;
            return width * (160f / dpi);
        }

        public static float ResolvedDpi()
        {
            float dpi = DevConsoleView.LayoutDpi;
            return dpi > 0f ? dpi : Screen.dpi;
        }

        public static bool IsPortraitScaledLayout(DevConsoleSettings settings)
        {
            if (settings == null)
                return false;
            if (settings.layout == DevConsoleLayout.MobilePortrait)
                return true;
            if (settings.layout == DevConsoleLayout.Rail)
                return DevConsoleView.LayoutScreenHeight > DevConsoleView.LayoutScreenWidth;
            return false;
        }

        public static float LandscapeScaleFactor(DevConsoleSettings settings)
        {
            float height = DevConsoleView.LayoutScreenHeight;
            float baseScale = Mathf.Clamp(height / 1080f, 0.9f, 2f);
            return baseScale * settings.ResolvedScaleFactor;
        }

        /// <summary>
        /// Dpi-based ConstantPixelSize scale for portrait Rail and MobilePortrait.
        /// Clamped to 1.5..4 after applying <see cref="DevConsoleSettings.portraitUiScale"/>.
        /// </summary>
        public static float PortraitScaleFactor(DevConsoleSettings settings)
        {
            float dpi = ResolvedDpi();
            float height = DevConsoleView.LayoutScreenHeight;
            float density = dpi > 0f ? dpi / 160f : height / 960f;
            float ui = settings != null ? Mathf.Clamp(settings.portraitUiScale, 0.75f, 2f) : 1f;
            float resolved = settings != null ? settings.ResolvedScaleFactor : 1f;
            float factor = Mathf.Max(density, 1.5f) * resolved * ui;
            return Mathf.Clamp(factor, 1.5f, 4f);
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>
        /// After setting top-stretch anchors (min=(0,1), max=(1,1)), clear the default
        /// sizeDelta (100,100) so width matches the parent instead of parent+100.
        /// </summary>
        public static void FitTopStretchContent(RectTransform rt, float height = 0f)
        {
            if (rt == null)
                return;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
            var omin = rt.offsetMin;
            var omax = rt.offsetMax;
            rt.offsetMin = new Vector2(0f, omin.y);
            rt.offsetMax = new Vector2(0f, omax.y);
        }

        public static Image AddImage(RectTransform rt, Color color)
        {
            var img = rt.GetComponent<Image>();
            if (img == null)
            {
                var existing = rt.GetComponent<Graphic>();
                if (existing != null)
                {
                    Debug.LogError(
                        $"[DevConsole] Cannot add Image to '{rt.gameObject.name}': " +
                        $"already has {existing.GetType().Name}.");
                    return null;
                }

                img = rt.gameObject.AddComponent<Image>();
            }

            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        public static TextMeshProUGUI AddLabel(
            RectTransform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
            bool semiBold = false)
        {
            var rt = CreateRect(name, parent);
            Stretch(rt);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            StyleLabel(tmp, semiBold);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = TextColor;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button AddButton(RectTransform rt, Color normal)
        {
            var img = AddImage(rt, normal);
            if (img == null)
                return null;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ApplyButtonColors(btn, normal);
            btn.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            return btn;
        }

        public static void ApplyButtonColors(Button btn, Color normal)
        {
            if (btn == null)
                return;
            var theme = Theme;
            var colors = btn.colors;
            colors.normalColor = normal;
            colors.highlightedColor = theme.buttonHover.a > 0.01f &&
                                      AlmostEqual(normal, theme.buttonNormal)
                ? theme.buttonHover
                : Color.Lerp(normal, Color.white, 0.15f);
            colors.pressedColor = AlmostEqual(normal, theme.buttonNormal)
                ? theme.buttonPressed
                : Color.Lerp(normal, Color.black, 0.15f);
            colors.disabledColor = theme.buttonDisabled;
            colors.selectedColor = AlmostEqual(normal, theme.buttonNormal)
                ? theme.buttonSelected
                : Color.Lerp(normal, theme.accentColor, 0.35f);
            btn.colors = colors;
        }

        static bool AlmostEqual(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.02f &&
            Mathf.Abs(a.g - b.g) < 0.02f &&
            Mathf.Abs(a.b - b.b) < 0.02f &&
            Mathf.Abs(a.a - b.a) < 0.02f;

        public static Button AddChromeButton(
            RectTransform rt,
            Color normal,
            Color highlight,
            Color? selectedOverride = null)
        {
            var img = AddImage(rt, normal);
            if (img == null)
                return null;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlight;
            colors.pressedColor = Color.Lerp(highlight, Color.black, 0.2f);
            colors.disabledColor = ButtonDisabled;
            colors.selectedColor = selectedOverride ?? highlight;
            btn.colors = colors;
            btn.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            return btn;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void SetHeight(RectTransform rt, float height)
        {
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        public static float MinTouch(DevConsoleSettings settings, Canvas canvas = null)
        {
            float row = Theme != null ? Theme.rowHeight : settings.buttonHeight;
            float h = row * settings.ResolvedScaleFactor;
            if (!IsPortraitScaledLayout(settings))
                return h;

            float scale = canvas != null && canvas.scaleFactor > 0f
                ? canvas.scaleFactor
                : PortraitScaleFactor(settings);
            float dpi = ResolvedDpi();
            float density = dpi > 0f
                ? dpi / 160f
                : DevConsoleView.LayoutScreenHeight / 960f;
            // 44dp in canvas units so height * scaleFactor >= 44 * density pixels.
            float minCanvas = 44f * density / Mathf.Max(scale, 0.01f);
            return Mathf.Max(h, minCanvas);
        }

        public static float FontSize(DevConsoleSettings settings, Canvas canvas = null)
        {
            float body = Theme != null ? Theme.bodyFontSize : settings.fontSize;
            float size = body * settings.ResolvedScaleFactor;
            if (!IsPortraitScaledLayout(settings))
                return size;

            float dp = DpToCanvas(canvas);
            return Mathf.Max(size, 15f * dp);
        }

        public static float TitleBarHeight(DevConsoleSettings settings, Canvas canvas)
        {
            if (IsPortraitScaledLayout(settings))
                return Mathf.Max(32f, MinTouch(settings, canvas) * 0.9f);
            return 32f;
        }
    }
}
#endif
