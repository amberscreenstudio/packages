using UnityEngine;

namespace AmberScreen.Dev
{
    /// <summary>
    /// Shared always-on Debug HUD rail (right edge). Games register lines; no OnGUI.
    /// When <c>AMBERSCREEN_DEVCONSOLE</c> is stripped, all members are no-ops / defaults.
    /// </summary>
    public static class DevHud
    {
#if AMBERSCREEN_DEVCONSOLE
        static bool s_Visible = true;
#endif

        public static bool Visible
        {
#if AMBERSCREEN_DEVCONSOLE
            get => s_Visible;
            set
            {
                if (s_Visible == value)
                    return;
                s_Visible = value;
                DevConsoleHost.NotifyHudVisibilityChanged();
                DevConsoleHost.PersistStateIfNeeded();
            }
#else
            get => false;
            set { }
#endif
        }

        public static void Toggle()
        {
#if AMBERSCREEN_DEVCONSOLE
            Visible = !Visible;
#endif
        }

        public static void SetLine(string section, string label, System.Func<string> value, int order = 0)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudRegistry.SetLine(section, label, value, order);
            DevConsoleHost.NotifyHudStructureChanged();
#endif
        }

        public static void RemoveLine(string section, string label)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudRegistry.RemoveLine(section, label);
            DevConsoleHost.NotifyHudStructureChanged();
#endif
        }

        public static void ClearSection(string section)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudRegistry.ClearSection(section);
            DevConsoleHost.NotifyHudStructureChanged();
#endif
        }

        public static void SetSectionOrder(params string[] sections)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudRegistry.SetSectionOrder(sections);
            DevConsoleHost.NotifyHudStructureChanged();
#endif
        }

        /// <summary>
        /// Screen-pixel rect for game content. Full safe area at 16:9 or narrower;
        /// centred 16:9 of the safe area when wider. Full screen when the define is off.
        /// </summary>
        public static Rect GameRect
        {
#if AMBERSCREEN_DEVCONSOLE
            get => DevHudLayout.ComputeGameRectPixels();
#else
            get => new Rect(0f, 0f, Screen.width, Screen.height);
#endif
        }

        /// <summary>Screen-pixel rect of the HUD rail, or zero when hidden / empty / define off.</summary>
        public static Rect RailScreenRect
        {
#if AMBERSCREEN_DEVCONSOLE
            get => DevConsoleHost.HudView != null
                ? DevConsoleHost.HudView.GetRailScreenRect()
                : Rect.zero;
#else
            get => Rect.zero;
#endif
        }

#if AMBERSCREEN_DEVCONSOLE
        internal static void ResetStatics()
        {
            s_Visible = true;
            DevHudRegistry.Reset();
        }

        internal static void ApplyRememberedVisible(bool visible) => s_Visible = visible;
#endif
    }
}
