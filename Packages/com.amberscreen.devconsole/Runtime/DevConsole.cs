using System;
using UnityEngine;

namespace AmberScreen.Dev
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
        static Action<bool> s_BlocksGameChanged;
        static float s_SavedTimescale = 1f;
        static bool s_HasSavedCursor;
        static CursorLockMode s_SavedLock;
        static bool s_SavedCursorVisible;
        static bool s_ConsumedCloseThisFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_IsOpen = false;
            s_Collapsed = false;
            s_Discovered = false;
            s_OpenChanged = null;
            s_BlocksGameChanged = null;
            s_SavedTimescale = 1f;
            s_HasSavedCursor = false;
            s_SavedLock = CursorLockMode.None;
            s_SavedCursorVisible = true;
            s_ConsumedCloseThisFrame = false;
            DevConsoleRegistry.Reset();
            DevConsoleSettings.ResetRuntimeDefault();
            DevConsoleUiUtil.ResetFontCache();
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

        /// <summary>
        /// True for the remainder of the frame after the console handles Esc or gamepad B
        /// while open and expanded. Games (e.g. pause menus) can skip their own close handling.
        /// Reset at the start of each frame.
        /// </summary>
        public static bool ConsumedCloseThisFrame
        {
#if AMBERSCREEN_DEVCONSOLE
            get => s_ConsumedCloseThisFrame;
            internal set => s_ConsumedCloseThisFrame = value;
#else
            get => false;
#endif
        }

        /// <summary>Fires when <see cref="IsOpen"/> changes. Does not fire on DockRight collapse/expand.</summary>
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

        /// <summary>Fires whenever <see cref="BlocksGame"/> changes (open, close, collapse, expand).</summary>
        public static event Action<bool> BlocksGameChanged
        {
#if AMBERSCREEN_DEVCONSOLE
            add => s_BlocksGameChanged += value;
            remove => s_BlocksGameChanged -= value;
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
            bool prevBlocks = BlocksGame;
            s_Collapsed = collapsed;
            if (prevBlocks != BlocksGame)
                s_BlocksGameChanged?.Invoke(BlocksGame);
        }

        internal static void NotifyHostDestroyed()
        {
            if (!s_IsOpen)
            {
                s_Collapsed = false;
                return;
            }

            RestoreWhileOpenState();
            bool prevBlocks = BlocksGame;
            s_IsOpen = false;
            s_Collapsed = false;
            s_OpenChanged?.Invoke(false);
            if (prevBlocks)
                s_BlocksGameChanged?.Invoke(false);
        }

        static void SetOpen(bool open)
        {
            if (s_IsOpen == open)
                return;

            bool prevBlocks = BlocksGame;

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
            if (prevBlocks != BlocksGame)
                s_BlocksGameChanged?.Invoke(BlocksGame);
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

        internal static void ResetForTests()
        {
            ResetStatics();
        }
#endif
    }
}
