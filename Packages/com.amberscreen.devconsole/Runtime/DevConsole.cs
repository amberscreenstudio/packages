using System;
using UnityEngine;

namespace AmberScreen.DevConsole
{
    /// <summary>
    /// Public static API for the Amber Screen Dev Console.
    /// When <c>AMBERSCREEN_DEVCONSOLE</c> is stripped, Register calls are no-ops and <see cref="IsOpen"/> is always false.
    /// </summary>
    public static class DevConsole
    {
#if AMBERSCREEN_DEVCONSOLE
        static bool s_IsOpen;
        static bool s_Collapsed;
        static bool s_Discovered;
        static Action<bool> s_OpenChanged;
        static float s_SavedTimescale = 1f;
        static bool s_HasSavedCursor;
        static CursorLockMode s_SavedLock;
        static bool s_SavedCursorVisible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_IsOpen = false;
            s_Collapsed = false;
            s_Discovered = false;
            s_OpenChanged = null;
            s_SavedTimescale = 1f;
            s_HasSavedCursor = false;
            s_SavedLock = CursorLockMode.None;
            s_SavedCursorVisible = true;
            DevConsoleRegistry.Reset();
            DevConsoleSettings.ResetRuntimeDefault();
            DevConsoleHost.ResetStatics();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void BootstrapHost()
        {
            if (!CanActivate())
                return;
            DevConsoleHost.EnsureExists();
        }
#endif

        public static bool IsOpen
        {
#if AMBERSCREEN_DEVCONSOLE
            get => s_IsOpen;
#else
            get => false;
#endif
        }

        /// <summary>True when open and not collapsed (DockRight collapse).</summary>
        public static bool BlocksGame
        {
#if AMBERSCREEN_DEVCONSOLE
            get => s_IsOpen && !s_Collapsed;
#else
            get => false;
#endif
        }

        public static event Action<bool> OpenChanged
        {
#if AMBERSCREEN_DEVCONSOLE
            add => s_OpenChanged += value;
            remove => s_OpenChanged -= value;
#else
            add { }
            remove { }
#endif
        }

        public static void Register(
            string section,
            string label,
            Action action,
            bool visible = true,
            Func<bool> available = null)
        {
#if AMBERSCREEN_DEVCONSOLE
            var entry = DevConsoleRegistry.Upsert(section, label, DevConsoleEntryKind.Button, visible, available);
            entry.Action = action;
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void SetButtonCaption(string section, string label, string caption)
        {
#if AMBERSCREEN_DEVCONSOLE
            if (DevConsoleRegistry.SetButtonCaption(section, label, caption))
                DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void RegisterToggle(
            string section,
            string label,
            Func<bool> get,
            Action<bool> set)
        {
#if AMBERSCREEN_DEVCONSOLE
            var entry = DevConsoleRegistry.Upsert(section, label, DevConsoleEntryKind.Toggle, true, null);
            entry.ToggleGet = get;
            entry.ToggleSet = set;
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void RegisterDropdown(
            string section,
            string label,
            Func<string[]> options,
            Action<int> onGo)
        {
#if AMBERSCREEN_DEVCONSOLE
            var entry = DevConsoleRegistry.Upsert(section, label, DevConsoleEntryKind.Dropdown, true, null);
            entry.DropdownOptions = options;
            entry.DropdownGo = onGo;
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void RegisterConfirm(string section, string label, Action action)
        {
#if AMBERSCREEN_DEVCONSOLE
            var entry = DevConsoleRegistry.Upsert(section, label, DevConsoleEntryKind.Confirm, true, null);
            entry.Action = action;
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void RegisterReadout(
            string section,
            string label,
            Func<string> value,
            float refreshSeconds = 0.5f)
        {
#if AMBERSCREEN_DEVCONSOLE
            var entry = DevConsoleRegistry.Upsert(section, label, DevConsoleEntryKind.Readout, true, null);
            entry.ReadoutValue = value;
            entry.ReadoutRefreshSeconds = refreshSeconds > 0f ? refreshSeconds : 0.5f;
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void Note(string message)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleRegistry.Note(message);
            DevConsoleHost.NotifyNotesChanged();
#endif
        }

        public static void SetSectionOrder(params string[] sections)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleRegistry.SetSectionOrder(sections);
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void Unregister(string section, string label)
        {
#if AMBERSCREEN_DEVCONSOLE
            if (DevConsoleRegistry.Unregister(section, label))
                DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void ClearSection(string section)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleRegistry.ClearSection(section);
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void Open()
        {
#if AMBERSCREEN_DEVCONSOLE
            if (!CanActivate())
                return;
            EnsureDiscovered();
            DevConsoleHost.EnsureExists();
            SetOpen(true);
#endif
        }

        public static void Close()
        {
#if AMBERSCREEN_DEVCONSOLE
            SetOpen(false);
#endif
        }

        public static void Toggle()
        {
#if AMBERSCREEN_DEVCONSOLE
            if (s_IsOpen)
                Close();
            else
                Open();
#endif
        }

#if AMBERSCREEN_DEVCONSOLE
        internal static bool CanActivate() => Debug.isDebugBuild;

        internal static void SetCollapsed(bool collapsed)
        {
            if (s_Collapsed == collapsed)
                return;
            s_Collapsed = collapsed;
            // BlocksGame is derived; notify open-changed consumers so UI labels refresh.
            s_OpenChanged?.Invoke(s_IsOpen);
        }

        internal static void NotifyHostDestroyed()
        {
            if (!s_IsOpen)
            {
                s_Collapsed = false;
                return;
            }

            RestoreWhileOpenState();
            s_IsOpen = false;
            s_Collapsed = false;
            s_OpenChanged?.Invoke(false);
        }

        static void SetOpen(bool open)
        {
            if (s_IsOpen == open)
                return;

            if (open)
            {
                ApplyWhileOpenState();
                s_IsOpen = true;
                s_Collapsed = false;
                DevConsoleHost.EnsureExists();
                DevConsoleHost.OnOpened();
            }
            else
            {
                RestoreWhileOpenState();
                s_IsOpen = false;
                s_Collapsed = false;
                DevConsoleHost.OnClosed();
            }

            s_OpenChanged?.Invoke(s_IsOpen);
        }

        static void ApplyWhileOpenState()
        {
            var settings = DevConsoleSettings.LoadOrDefault();
            if (settings.pauseTimescaleWhileOpen)
            {
                s_SavedTimescale = Time.timeScale;
                Time.timeScale = 0f;
            }

            if (settings.showCursorWhileOpen)
            {
                s_SavedLock = Cursor.lockState;
                s_SavedCursorVisible = Cursor.visible;
                s_HasSavedCursor = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        static void RestoreWhileOpenState()
        {
            var settings = DevConsoleSettings.LoadOrDefault();
            if (settings.pauseTimescaleWhileOpen)
                Time.timeScale = s_SavedTimescale;

            if (s_HasSavedCursor && settings.showCursorWhileOpen)
            {
                Cursor.lockState = s_SavedLock;
                Cursor.visible = s_SavedCursorVisible;
                s_HasSavedCursor = false;
            }
        }

        static void EnsureDiscovered()
        {
            if (s_Discovered)
                return;
            s_Discovered = true;

            var settings = DevConsoleSettings.LoadOrDefault();
            if (settings.enableAttributeDiscovery)
                DevConsoleDiscovery.DiscoverAttributes();
            if (settings.enableProviderDiscovery)
                DevConsoleDiscovery.DiscoverProviders();
        }

        // Exposed for tests / host
        internal static void ResetForTests()
        {
            ResetStatics();
        }
#endif
    }
}
