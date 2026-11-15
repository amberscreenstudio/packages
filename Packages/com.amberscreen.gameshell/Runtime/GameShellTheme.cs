// Colours and font for the pause frame, version label and scene fade.
using UnityEngine;

namespace AmberScreen.GameShell
{
    [CreateAssetMenu(menuName = "AmberScreen/Game Shell Theme", fileName = "GameShellTheme")]
    public class GameShellTheme : ScriptableObject
    {
        [Tooltip("Legacy uGUI font. Empty = Unity's built-in LegacyRuntime font.")]
        public Font font;

        [Header("Panel")]
        public Color dimmer = new Color(0f, 0f, 0f, 0.55f);
        public Color panel = new Color(0.93f, 0.89f, 0.80f, 1f);
        public Color text = new Color(0.15f, 0.12f, 0.09f, 1f);
        public Color accent = new Color(0.55f, 0.40f, 0.20f, 1f);

        [Header("Buttons")]
        public Color buttonFace = new Color(0.30f, 0.24f, 0.17f, 1f);
        public Color buttonText = new Color(0.96f, 0.93f, 0.85f, 1f);
        public Color buttonPressed = new Color(0.45f, 0.32f, 0.16f, 1f);
        public Color buttonDisabled = new Color(0.25f, 0.22f, 0.18f, 0.6f);

        [Header("Scroll")]
        public Color scrollbarBackground = new Color(0.20f, 0.16f, 0.12f, 0.45f);

        [Header("Overlay")]
        public Color fade = new Color(0.04f, 0.03f, 0.025f, 1f);
        public Color versionText = new Color(0.96f, 0.93f, 0.85f, 0.55f);

        static GameShellTheme s_Builtin;

        /// <summary>The default look (warm paper panel, brass accent, dark fade).</summary>
        public static GameShellTheme BuiltinDefault
        {
            get
            {
                if (s_Builtin == null)
                {
                    s_Builtin = CreateInstance<GameShellTheme>();
                    s_Builtin.name = "GameShellTheme (built-in)";
                    s_Builtin.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Builtin;
            }
        }

        public Font ResolveFont()
        {
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
