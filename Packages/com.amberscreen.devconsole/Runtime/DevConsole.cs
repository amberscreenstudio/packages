using System;
using UnityEngine;

namespace AmberScreen.Dev
{
    /// <summary>
    /// Public static API for the Amber Screen Dev Console.
    /// When <c>AMBERSCREEN_DEVCONSOLE</c> is stripped, Register calls are no-ops and <see cref="IsOpen"/> is always false.
    /// Gate gameplay input on <see cref="BlocksGame"/> (not <see cref="IsOpen"/>). Skip world clicks when
    /// <see cref="IsPointerOverConsole"/> is true.
    /// </summary>
    public static class DevConsole
    {
#if AMBERSCREEN_DEVCONSOLE
        static bool s_IsOpen;
        static bool s_Collapsed;
        static string s_OpenSectionName;
        static bool s_Discovered;
        static Action<bool> s_OpenChanged;
        static Action<bool> s_BlocksGameChanged;
        static float s_SavedTimescale = 1f;
        static bool s_HasSavedCursor;
        static bool s_TimescalePaused;
        static CursorLockMode s_SavedLock;
        static bool s_SavedCursorVisible;
        static bool s_ConsumedCloseThisFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_IsOpen = false;
            s_Collapsed = false;
            s_OpenSectionName = null;
            s_Discovered = false;
            s_OpenChanged = null;
            s_BlocksGameChanged = null;
            s_SavedTimescale = 1f;
            s_HasSavedCursor = false;
            s_TimescalePaused = false;
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
            DevConsoleHost.TryRestoreRememberedOpen();
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

        /// <summary>
        /// True when gameplay should be blocked. For Rail (landscape): only while a section window is open.
        /// For Rail (portrait sheet): while the rail is open and expanded. For other layouts: open and not collapsed.
        /// </summary>
        public static bool BlocksGame
        {
#if AMBERSCREEN_DEVCONSOLE
            get => ComputeBlocksGame();
#else
            get => false;
#endif
        }

        /// <summary>
        /// True for the remainder of the frame after the console handles Esc or gamepad B.
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

        public static string OpenSectionName
        {
#if AMBERSCREEN_DEVCONSOLE
            get => s_OpenSectionName;
#else
            get => null;
#endif
        }

        public static bool IsRailOpen
        {
#if AMBERSCREEN_DEVCONSOLE
            get => s_IsOpen && IsRailLayout();
#else
            get => false;
#endif
        }

        public static bool IsPointerOverConsole
        {
#if AMBERSCREEN_DEVCONSOLE
            get => DevConsoleHost.Instance != null && DevConsoleHost.IsPointerOverConsoleUi();
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

        public static void RegisterQuick(string label, Action action, Func<bool> available = null)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleRegistry.UpsertQuick(label, action, available);
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void UnregisterQuick(string label)
        {
#if AMBERSCREEN_DEVCONSOLE
            if (DevConsoleRegistry.UnregisterQuick(label))
                DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void PinReadout(string section, string label)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleRegistry.PinReadout(section, label);
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        public static void OpenSection(string section)
        {
#if AMBERSCREEN_DEVCONSOLE
            if (!CanActivate() || string.IsNullOrEmpty(section))
                return;
            EnsureDiscovered();
            DevConsoleHost.EnsureExists();
            if (!s_IsOpen)
                SetOpen(true);
            SetOpenSection(section);
#endif
        }

        public static void CloseSection()
        {
#if AMBERSCREEN_DEVCONSOLE
            SetOpenSection(null);
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
            SetOpenSection(null);
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

        internal static bool IsRailLayout()
        {
            var settings = DevConsoleHost.Settings;
            return settings != null && settings.layout == DevConsoleLayout.Rail;
        }

        internal static bool IsPortraitRail()
        {
            return IsRailLayout() && Screen.height > Screen.width;
        }

        static bool ComputeBlocksGame()
        {
            if (!s_IsOpen)
                return false;

            if (IsRailLayout())
            {
                if (IsPortraitRail())
                    return !s_Collapsed;
                return !string.IsNullOrEmpty(s_OpenSectionName);
            }

            return !s_Collapsed;
        }

        internal static void SetCollapsed(bool collapsed)
        {
            if (s_Collapsed == collapsed)
                return;
            bool prevBlocks = BlocksGame;
            s_Collapsed = collapsed;
            if (IsRailLayout() && collapsed)
                SetOpenSection(null);
            NotifyBlocksChanged(prevBlocks);
            DevConsoleHost.PersistStateIfNeeded();
        }

        internal static void SetOpenSection(string section)
        {
            string next = string.IsNullOrEmpty(section) ? null : section;
            if (string.Equals(s_OpenSectionName, next, StringComparison.Ordinal))
            {
                DevConsoleHost.Instance?.NotifySectionWindowChanged();
                return;
            }

            bool prevBlocks = BlocksGame;
            s_OpenSectionName = next;
            SyncPauseForBlocks();
            NotifyBlocksChanged(prevBlocks);
            DevConsoleHost.Instance?.NotifySectionWindowChanged();
            DevConsoleHost.PersistStateIfNeeded();
        }

        internal static void NotifyHostDestroyed()
        {
            if (!s_IsOpen)
            {
                s_Collapsed = false;
                s_OpenSectionName = null;
                return;
            }

            RestoreWhileOpenState();
            bool prevBlocks = BlocksGame;
            s_IsOpen = false;
            s_Collapsed = false;
            s_OpenSectionName = null;
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
                ApplyCursorWhileOpen();
                s_IsOpen = true;
                s_Collapsed = false;
                DevConsoleHost.EnsureExists();
                if (DevConsoleHost.Settings.rememberState)
                    DevConsoleHost.ApplyRememberedSectionOnOpen();
                DevConsoleHost.OnOpened();
                SyncPauseForBlocks();
            }
            else
            {
                RestoreWhileOpenState();
                s_IsOpen = false;
                s_Collapsed = false;
                s_OpenSectionName = null;
                DevConsoleHost.OnClosed();
            }

            s_OpenChanged?.Invoke(s_IsOpen);
            NotifyBlocksChanged(prevBlocks);
            DevConsoleHost.PersistStateIfNeeded();
        }

        static void NotifyBlocksChanged(bool prevBlocks)
        {
            if (prevBlocks != BlocksGame)
                s_BlocksGameChanged?.Invoke(BlocksGame);
        }

        static void ApplyCursorWhileOpen()
        {
            var settings = DevConsoleSettings.LoadOrDefault();
            if (!settings.showCursorWhileOpen)
                return;

            s_SavedLock = Cursor.lockState;
            s_SavedCursorVisible = Cursor.visible;
            s_HasSavedCursor = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        static void SyncPauseForBlocks()
        {
            var settings = DevConsoleSettings.LoadOrDefault();
            if (!settings.pauseTimescaleWhileOpen)
                return;

            bool shouldPause = IsRailLayout()
                ? !string.IsNullOrEmpty(s_OpenSectionName)
                : BlocksGame;

            if (shouldPause && !s_TimescalePaused)
            {
                s_SavedTimescale = Time.timeScale;
                Time.timeScale = 0f;
                s_TimescalePaused = true;
            }
            else if (!shouldPause && s_TimescalePaused)
            {
                Time.timeScale = s_SavedTimescale;
                s_TimescalePaused = false;
            }
        }

        static void RestoreWhileOpenState()
        {
            var settings = DevConsoleSettings.LoadOrDefault();
            if (s_TimescalePaused && settings.pauseTimescaleWhileOpen)
            {
                Time.timeScale = s_SavedTimescale;
                s_TimescalePaused = false;
            }

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

        internal static bool IsCollapsed => s_Collapsed;
#endif
    }
}
