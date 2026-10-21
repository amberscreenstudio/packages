using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberScreen.Dev
{
    /// <summary>
    /// Shared always-on Debug HUD (left edge / portrait top strip). Games register lines; no OnGUI.
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
        /// Creates (or updates) a collapsible HUD section and returns a handle for adding rows /
        /// tables. Collapse state is stored in PlayerPrefs under
        /// <c>AmberScreen.DevHud.Collapsed.{id}</c> when <paramref name="rememberCollapsed"/> is true.
        /// Returns a no-op handle when <c>AMBERSCREEN_DEVCONSOLE</c> is stripped.
        /// </summary>
        public static DevHudSectionHandle AddSection(
            string id,
            string title,
            Color? tint = null,
            Sprite icon = null,
            bool startCollapsed = false,
            bool rememberCollapsed = true)
        {
#if AMBERSCREEN_DEVCONSOLE
            var section = DevHudRegistry.AddSection(id, title, tint, icon, startCollapsed, rememberCollapsed);
            DevConsoleHost.NotifyHudStructureChanged();
            return new DevHudSectionHandle(section);
#else
            return new DevHudSectionHandle(id, title);
#endif
        }

        /// <summary>Replaces the hints of <paramref name="mode"/> with none (shows nothing for the mode).</summary>
        public static void SetKeyHints(string mode)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudKeyHints.SetKeyHints(mode, System.Array.Empty<(InputAction action, string label)>());
            DevConsoleHost.NotifyHudStructureChanged();
#endif
        }

        public static void SetKeyHints(string mode, params (InputAction action, string label)[] hints)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudKeyHints.SetKeyHints(mode, hints);
            DevConsoleHost.NotifyHudStructureChanged();
#endif
        }

        /// <summary>Replaces the hints of <paramref name="mode"/> with fixed key strings (e.g. "Space").</summary>
        public static void SetKeyHints(string mode, params (string key, string label)[] hints)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudKeyHints.SetKeyHints(mode, hints);
            DevConsoleHost.NotifyHudStructureChanged();
#endif
        }

        /// <summary>Appends a fixed-string hint to <paramref name="mode"/>; mixable with InputAction hints.</summary>
        public static void AddKeyHint(string mode, string key, string label)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudKeyHints.AddKeyHint(mode, key, label);
            DevConsoleHost.NotifyHudStructureChanged();
#endif
        }

        /// <summary>Appends an InputAction hint to <paramref name="mode"/>; mixable with string hints.</summary>
        public static void AddKeyHint(string mode, InputAction action, string label)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudKeyHints.AddKeyHint(mode, action, label);
            DevConsoleHost.NotifyHudStructureChanged();
#endif
        }

        public static void SetMode(string mode)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudKeyHints.SetMode(mode);
            DevConsoleHost.NotifyHudStructureChanged();
#endif
        }

        public static void ClearKeyHints(string mode)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudKeyHints.ClearKeyHints(mode);
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
            DevHudKeyHints.Reset();
        }

        internal static void ApplyRememberedVisible(bool visible) => s_Visible = visible;
#endif
    }
}
