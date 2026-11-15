using UnityEngine;

namespace AmberScreen.Settings
{
    /// <summary>
    /// Colours and font for the built-in <see cref="SettingsMenu"/>. Assign one to the menu's <c>theme</c>, call
    /// <see cref="SettingsMenu.ApplyTheme"/>, or put one in a Resources folder named <c>AmberScreenSettingsMenuTheme</c>
    /// to use it for every menu. The defaults are the original dark look.
    /// </summary>
    [CreateAssetMenu(menuName = "AmberScreen/Settings Menu Theme", fileName = ResourcesName)]
    public sealed class SettingsMenuTheme : ScriptableObject
    {
        public const string ResourcesName = "AmberScreenSettingsMenuTheme";

        [Tooltip("Leave empty to use Unity's built-in LegacyRuntime font (or the menu's own font field).")]
        public Font font;
        public string title = "Settings";

        [Header("Panel")]
        public Color dim = new Color(0f, 0f, 0f, 0.55f);
        public Color panel = new Color(0.10f, 0.10f, 0.12f, 0.97f);
        public Color header = new Color(0.16f, 0.16f, 0.19f, 1f);
        public Color text = new Color(0.92f, 0.92f, 0.92f, 1f);
        [Tooltip("Section headings, slider fill and pressed buttons.")]
        public Color accent = new Color(0.95f, 0.68f, 0.25f, 1f);

        [Header("Buttons and sliders")]
        public Color buttonNormal = new Color(0.24f, 0.24f, 0.27f, 1f);
        public Color buttonHighlighted = new Color(0.36f, 0.36f, 0.40f, 1f);
        public Color buttonSelected = new Color(0.48f, 0.40f, 0.26f, 1f);
        public Color buttonDisabled = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [Tooltip("Text on buttons (the < > arrows, Close, Reset).")]
        public Color buttonText = new Color(0.92f, 0.92f, 0.92f, 1f);
        public Color sliderBackground = new Color(0.22f, 0.22f, 0.25f, 1f);
        public Color sliderHandle = Color.white;
        public Color scrollbarBackground = new Color(0.16f, 0.16f, 0.19f, 1f);
        public Color scrollbarHandle = new Color(0.45f, 0.45f, 0.5f, 1f);

        static SettingsMenuTheme s_Builtin;

        /// <summary>The original dark look (a shared, unsaved instance).</summary>
        public static SettingsMenuTheme BuiltinDefault
        {
            get
            {
                if (s_Builtin == null)
                {
                    s_Builtin = CreateInstance<SettingsMenuTheme>();
                    s_Builtin.name = "Settings Menu Theme (built-in)";
                    s_Builtin.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Builtin;
            }
        }
    }
}
