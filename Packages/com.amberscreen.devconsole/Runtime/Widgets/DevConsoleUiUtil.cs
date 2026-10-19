#if AMBERSCREEN_DEVCONSOLE
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.DevConsole
{
    static class DevConsoleUiUtil
    {
        static TMP_FontAsset s_Font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (s_Font == null)
                    s_Font = TMP_Settings.defaultFontAsset;
                return s_Font;
            }
        }

        public static Color PanelBg => new Color(0.08f, 0.09f, 0.11f, 0.94f);
        public static Color RowBg => new Color(0.14f, 0.15f, 0.18f, 1f);
        public static Color ButtonBg => new Color(0.22f, 0.35f, 0.55f, 1f);
        public static Color ButtonDisabled => new Color(0.25f, 0.25f, 0.28f, 1f);
        public static Color TextColor => new Color(0.95f, 0.95f, 0.97f, 1f);
        public static Color Accent => new Color(0.95f, 0.75f, 0.25f, 1f);

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
            tmp.font = Font;
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

        public static float MinTouch(DevConsoleSettings settings)
        {
            float h = settings.buttonHeight * settings.ResolvedScaleFactor;
            if (settings.layout == DevConsoleLayout.MobilePortrait)
                return Mathf.Max(h, 48f);
            return h;
        }

        public static float FontSize(DevConsoleSettings settings) =>
            settings.fontSize * settings.ResolvedScaleFactor;
    }
}
#endif
