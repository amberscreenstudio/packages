#if AMBERSCREEN_DEVCONSOLE
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    static class DevConsoleUiUtil
    {
        static TMP_FontAsset s_Regular;
        static TMP_FontAsset s_SemiBold;
        static bool s_WarnedMissingFont;

        public static void ResetFontCache()
        {
            s_Regular = null;
            s_SemiBold = null;
            s_WarnedMissingFont = false;
        }

        public static TMP_FontAsset ResolveFont(DevConsoleSettings settings = null) =>
            ResolveRegular(settings);

        public static TMP_FontAsset Font => ResolveRegular();

        public static TMP_FontAsset FontSemiBold => ResolveSemiBold();

        public static TMP_FontAsset ResolveRegular(DevConsoleSettings settings = null)
        {
            if (s_Regular != null)
                return s_Regular;

            settings ??= DevConsoleSettings.LoadOrDefault();

            if (settings.fontOverride != null)
            {
                s_Regular = settings.fontOverride;
                return s_Regular;
            }

            if (settings.bundledRegular != null)
                s_Regular = CreateDynamicFontAsset(settings.bundledRegular);

            if (s_Regular == null)
            {
                try { s_Regular = TMP_Settings.GetFontAsset(); }
                catch { /* TMP Essential Resources may be missing */ }
            }

            if (s_Regular == null && !s_WarnedMissingFont)
            {
                s_WarnedMissingFont = true;
                Debug.LogWarning(
                    "[AmberScreen DevConsole] No font available. Assign bundled Inter on DevConsoleSettings " +
                    "or import TMP Essential Resources.");
            }

            return s_Regular;
        }

        public static TMP_FontAsset ResolveSemiBold(DevConsoleSettings settings = null)
        {
            if (s_SemiBold != null)
                return s_SemiBold;

            settings ??= DevConsoleSettings.LoadOrDefault();

            if (settings.fontOverride != null)
            {
                s_SemiBold = settings.fontOverride;
                return s_SemiBold;
            }

            if (settings.bundledSemiBold != null)
                s_SemiBold = CreateDynamicFontAsset(settings.bundledSemiBold);

            if (s_SemiBold == null)
                s_SemiBold = ResolveRegular(settings);

            return s_SemiBold;
        }

        static TMP_FontAsset CreateDynamicFontAsset(Font font)
        {
            if (font == null)
                return null;

            return TMP_FontAsset.CreateFontAsset(
                font,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic);
        }

        public static void ApplyCrisp(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;
            tmp.fontStyle = FontStyles.Normal;
            tmp.extraPadding = false;
            tmp.characterSpacing = 0f;
        }

        public static int ComputeAutoColumns(float contentWidth, float minEntryWidth, float spacing)
        {
            float denom = minEntryWidth + spacing;
            if (contentWidth <= 0f || denom <= 0f)
                return 1;
            return Mathf.Max(1, Mathf.FloorToInt(contentWidth / denom));
        }

        public static Color PanelBg => new Color(0.08f, 0.09f, 0.11f, 0.94f);
        public static Color RowBg => new Color(0.14f, 0.15f, 0.18f, 1f);
        public static Color ButtonBg => new Color(0.22f, 0.35f, 0.55f, 1f);
        public static Color ButtonDisabled => new Color(0.25f, 0.25f, 0.28f, 1f);
        public static Color TextColor => new Color(0.95f, 0.95f, 0.97f, 1f);
        public static Color Accent => new Color(0.95f, 0.75f, 0.25f, 1f);

        public static float DpToCanvas(Canvas canvas)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            return (dpi / 160f) / scale;
        }

        public static float SafeAreaWidthDp()
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            return Screen.safeArea.width * (160f / dpi);
        }

        public static float LandscapeScaleFactor(DevConsoleSettings settings)
        {
            float baseScale = Mathf.Clamp(Screen.height / 1080f, 0.9f, 2f);
            return baseScale * settings.ResolvedScaleFactor;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
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
            var font = semiBold ? FontSemiBold : Font;
            if (font != null)
                tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = TextColor;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            ApplyCrisp(tmp);
            return tmp;
        }

        public static Button AddButton(RectTransform rt, Color normal)
        {
            var img = AddImage(rt, normal);
            if (img == null)
                return null;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = normal;
            colors.highlightedColor = Color.Lerp(normal, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(normal, Color.black, 0.15f);
            colors.disabledColor = ButtonDisabled;
            colors.selectedColor = Color.Lerp(normal, Accent, 0.35f);
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
            float h = settings.buttonHeight * settings.ResolvedScaleFactor;
            if (settings.layout == DevConsoleLayout.MobilePortrait)
            {
                float dp = DpToCanvas(canvas);
                return Mathf.Max(h, 44f * dp);
            }

            return h;
        }

        public static float FontSize(DevConsoleSettings settings, Canvas canvas = null)
        {
            float size = settings.fontSize * settings.ResolvedScaleFactor;
            if (settings.layout == DevConsoleLayout.MobilePortrait)
            {
                float dp = DpToCanvas(canvas);
                return Mathf.Max(size, 15f * dp);
            }

            return size;
        }

        public static float TitleBarHeight(DevConsoleSettings settings, Canvas canvas)
        {
            if (settings.layout == DevConsoleLayout.MobilePortrait)
                return Mathf.Max(32f, MinTouch(settings, canvas) * 0.9f);
            return 32f;
        }
    }
}
#endif
