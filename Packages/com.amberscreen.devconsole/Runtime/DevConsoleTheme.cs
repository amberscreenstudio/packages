using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev
{
    [Serializable]
    public sealed class DevConsoleThemeSurface
    {
        public Color color = Color.white;
        public Sprite sprite;
        public bool sliced;
    }

    /// <summary>
    /// Skin for the Dev Console and Debug HUD. Null on settings uses <see cref="BuiltinDefault"/>.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AmberScreenDevConsoleTheme",
        menuName = "Amber Screen/Dev Console Theme")]
    public sealed class DevConsoleTheme : ScriptableObject
    {
        static DevConsoleTheme s_Builtin;

        [Header("Surfaces")]
        public DevConsoleThemeSurface panelBackground = new DevConsoleThemeSurface
        {
            color = new Color(0.08f, 0.09f, 0.11f, 0.94f)
        };

        public DevConsoleThemeSurface railBackground = new DevConsoleThemeSurface
        {
            color = new Color(0.08f, 0.09f, 0.11f, 0.88f)
        };

        public DevConsoleThemeSurface hudBackground = new DevConsoleThemeSurface
        {
            color = new Color(0.05f, 0.06f, 0.08f, 1f)
        };

        public DevConsoleThemeSurface windowBackground = new DevConsoleThemeSurface
        {
            color = new Color(0.08f, 0.09f, 0.11f, 0.94f)
        };

        public DevConsoleThemeSurface rowBackground = new DevConsoleThemeSurface
        {
            color = new Color(0.14f, 0.15f, 0.18f, 1f)
        };

        [Header("Border")]
        public Color borderColor = new Color(1f, 1f, 1f, 0.08f);
        [Min(0f)] public float borderWidth = 0f;

        [Header("Fonts")]
        public TMP_FontAsset fontRegular;
        public TMP_FontAsset fontBold;

        [Header("Text colours")]
        public Color labelColor = new Color(0.95f, 0.95f, 0.97f, 1f);
        public Color valueColor = new Color(0.95f, 0.95f, 0.97f, 1f);
        public Color warningColor = new Color(0.95f, 0.75f, 0.25f, 1f);
        public Color errorColor = new Color(0.85f, 0.25f, 0.25f, 1f);
        public Color accentColor = new Color(0.95f, 0.75f, 0.25f, 1f);
        public Color mutedColor = new Color(0.75f, 0.78f, 0.82f, 1f);

        [Header("Font sizes")]
        [Min(10f)] public float titleFontSize = 16f;
        [Min(10f)] public float bodyFontSize = 16f;
        [Min(10f)] public float hudFontSize = 14f;

        [Header("Layout")]
        [Min(0f)] public float padding = 6f;
        [Min(0f)] public float spacing = 4f;
        [Min(16f)] public float rowHeight = 30f;
        [Min(0f)] public float cornerRadius = 0f;

        [Header("Buttons")]
        public Color buttonNormal = new Color(0.22f, 0.35f, 0.55f, 1f);
        // Match prior Color.Lerp(normal, white/black/accent) so the builtin look is unchanged.
        public Color buttonHover = new Color(0.337f, 0.4475f, 0.6175f, 1f);
        public Color buttonPressed = new Color(0.187f, 0.2975f, 0.4675f, 1f);
        public Color buttonDisabled = new Color(0.25f, 0.25f, 0.28f, 1f);
        public Color buttonSelected = new Color(0.4755f, 0.49f, 0.445f, 1f);

        [Header("Chrome")]
        public Color chromeButtonBg = new Color(1f, 1f, 1f, 0.06f);
        public Color chromeButtonHighlight = new Color(1f, 1f, 1f, 0.14f);
        public Color chromeCloseHover = new Color(0.85f, 0.25f, 0.25f, 1f);
        public Color confirmButton = new Color(0.55f, 0.2f, 0.2f, 1f);
        public Color dropdownBg = new Color(0.12f, 0.13f, 0.16f, 1f);
        public Color dropdownTemplateBg = new Color(0.1f, 0.1f, 0.12f, 0.98f);
        public Color dropdownItemBg = new Color(0.18f, 0.2f, 0.24f, 1f);
        public Color dropdownCheck = new Color(0.3f, 0.45f, 0.7f, 0.5f);
        public Color toggleBox = new Color(0.1f, 0.1f, 0.12f, 1f);
        public Color sectionHeader = new Color(0.16f, 0.18f, 0.22f, 1f);
        public Color scrollWell = new Color(0, 0, 0, 0.15f);
        public Color historyWell = new Color(0, 0, 0, 0.25f);
        public Color filterWell = new Color(0, 0, 0, 0.35f);
        public Color statusWell = new Color(0, 0, 0, 0.2f);
        public Color placeholderColor = new Color(1, 1, 1, 0.35f);

        [Header("Debug HUD")]
        [Tooltip("HUD panel fill. Defaults match hudBackground.color. The sprite still comes from hudBackground.")]
        public Color hudPanelColor = new Color(0.05f, 0.06f, 0.08f, 1f);
        [Tooltip("Section header bar. Defaults match sectionHeader. A section tint is blended on top.")]
        public Color hudSectionHeaderColor = new Color(0.16f, 0.18f, 0.22f, 1f);
        public Color hudLabelColor = new Color(0.95f, 0.95f, 0.97f, 1f);
        public Color hudValueColor = new Color(0.95f, 0.95f, 0.97f, 1f);
        [Tooltip("Always-visible pinned strip at the top of the HUD (slightly brighter than the panel).")]
        public Color hudPinnedStripColor = new Color(0.09f, 0.10f, 0.13f, 1f);
        [Tooltip("Multiplied with DevConsoleSettings.hudOpacity.")]
        [Range(0f, 1f)] public float hudOpacity = 1f;
        [Tooltip("Null uses fontRegular.")]
        public TMP_FontAsset hudFont;
        [Min(8f)] public float hudHeaderFontSize = 14f;
        [Min(0f)] public float hudCellPadding = 4f;

        /// <summary>Built-in theme matching the 0.4.0 look exactly.</summary>
        public static DevConsoleTheme BuiltinDefault
        {
            get
            {
                if (s_Builtin == null)
                {
                    s_Builtin = CreateInstance<DevConsoleTheme>();
                    s_Builtin.name = "DevConsoleTheme.BuiltinDefault";
                    s_Builtin.hideFlags = HideFlags.HideAndDontSave;
                }

                return s_Builtin;
            }
        }

        public static void ApplySurface(Image image, DevConsoleThemeSurface surface, float alphaMul = 1f)
        {
            if (image == null || surface == null)
                return;
            var c = surface.color;
            c.a *= alphaMul;
            image.color = c;
            if (surface.sprite != null)
            {
                image.sprite = surface.sprite;
                image.type = surface.sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
        }

#if UNITY_EDITOR
        void Reset()
        {
            // Keep inspector defaults aligned with BuiltinDefault field initializers.
            hudPanelColor = hudBackground.color;
            hudSectionHeaderColor = sectionHeader;
            hudLabelColor = labelColor;
            hudValueColor = valueColor;
        }
#endif
    }
}
