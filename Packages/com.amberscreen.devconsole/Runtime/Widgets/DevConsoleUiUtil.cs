#if AMBERSCREEN_DEVCONSOLE
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    static class DevConsoleUiUtil
    {
        static TMP_FontAsset s_Font;
        static bool s_WarnedMissingFont;

        public static void ResetFontCache()
        {
            s_Font = null;
            s_WarnedMissingFont = false;
        }

        public static TMP_FontAsset ResolveFont(DevConsoleSettings settings = null)
        {
            if (s_Font != null)
                return s_Font;

            settings ??= DevConsoleSettings.LoadOrDefault();

            try
            {
                s_Font = TMP_Settings.GetFontAsset();
            }
            catch
            {
                // TMP settings may be unavailable before Essential Resources import.
            }

            if (s_Font == null && settings.fontOverride != null)
                s_Font = settings.fontOverride;

            if (s_Font == null)
            {
                s_Font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
                         ?? Resources.Load<TMP_FontAsset>("LiberationSans SDF");
            }

            if (s_Font == null && !s_WarnedMissingFont)
            {
                s_WarnedMissingFont = true;
                Debug.LogWarning(
                    "[AmberScreen DevConsole] No TMP font found. Import TMP Essential Resources " +
                    "(Window â†’ TextMeshPro â†’ Import TMP Essential Resources).");
            }

            return s_Font;
        }

        public static TMP_FontAsset Font => ResolveFont();

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

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static Image AddImage(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        public static TextMeshProUGUI AddLabel(
            RectTransform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var rt = CreateRect(name, parent);
            Stretch(rt);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = Font;
            if (font != null)
                tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = TextColor;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        public static Button AddButton(RectTransform rt, Color normal)
        {
            var img = AddImage(rt, normal);
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
                return Mathf.Max(h, 48f * dp);
            }
            return h;
        }

        public static float FontSize(DevConsoleSettings settings, Canvas canvas = null)
        {
            float size = settings.fontSize * settings.ResolvedScaleFactor;
            if (settings.layout == DevConsoleLayout.MobilePortrait)
            {
                float dp = DpToCanvas(canvas);
                return Mathf.Max(size, 16f * dp);
            }
            return size;
        }
    }
}
#endif
