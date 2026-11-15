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
        static Func<bool> s_OpenBlocked;

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
            s_OpenBlocked = null;
            DevConsoleRegistry.Reset();
            DevConsoleSettings.ResetRuntimeDefault();
            DevConsoleUiUtil.ResetFontCache();
            DevHud.ResetStatics();
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

        /// <summary>
        /// Same as <see cref="Register(string,string,Action,bool,Func{bool})"/> with a live visibility
        /// predicate. Hidden entries take no layout space. Re-evaluated on open and each refresh.
        /// </summary>
        public static void Register(
            string section,
            string label,
            Action action,
            bool visible,
            Func<bool> available,
            Func<bool> visiblePredicate)
        {
#if AMBERSCREEN_DEVCONSOLE
            var entry = DevConsoleRegistry.Upsert(
                section, label, DevConsoleEntryKind.Button, visible, available, visiblePredicate);
            entry.Action = action;
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        /// <summary>Inserts a non-interactive sub-header inside <paramref name="section"/>.</summary>
        public static void RegisterSubHeader(string section, string title)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleRegistry.Upsert(section, title, DevConsoleEntryKind.SubHeader, true, null);
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        /// <summary>
        /// Shows or hides an entry by id (<c>section/label</c>). Returns false if not found.
        /// </summary>
        public static bool SetVisible(string entryId, bool visible)
        {
#if AMBERSCREEN_DEVCONSOLE
            bool ok = DevConsoleRegistry.SetVisible(entryId, visible);
            if (ok)
                DevConsoleHost.NotifyRegistryChanged();
            return ok;
#else
            return false;
#endif
        }

        /// <summary>
        /// Turns the rail command-line input on or off at runtime, overriding the
        /// <c>commandLine</c> setting until the host is destroyed (or tests reset).
        /// </summary>
        public static void SetCommandLineEnabled(bool enabled)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleHost.SetCommandLineEnabled(enabled);
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

        /// <summary>
        /// Return true while the console must not open (game paused, loading, a cutscene...).
        /// Checked by the open key, gestures, <see cref="Open"/>, <see cref="Toggle"/> and <see cref="OpenSection"/>.
        /// Closing is never blocked, and an already open console stays open. Null (default) = never blocked.
        /// </summary>
        public static Func<bool> OpenBlocked
        {
#if AMBERSCREEN_DEVCONSOLE
            get => s_OpenBlocked;
            set => s_OpenBlocked = value;
#else
            get => null;
            set { }
#endif
        }

        /// <summary>True when <see cref="OpenBlocked"/> says no right now.</summary>
        public static bool IsOpenBlocked
        {
#if AMBERSCREEN_DEVCONSOLE
            get
            {
                if (s_OpenBlocked == null)
                    return false;
                try { return s_OpenBlocked(); }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return false;
                }
            }
#else
            get => false;
#endif
        }

        /// <summary>Key that opens/closes the console, for this session (overrides the settings asset; Key.None = no key).</summary>
        public static void SetOpenKey(UnityEngine.InputSystem.Key key)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleHost.SetOpenKeyOverride(key);
#endif
        }

        /// <summary>Key that shows/hides the HUD overlay, for this session (overrides the settings asset; Key.None = no key).</summary>
        public static void SetHudToggleKey(UnityEngine.InputSystem.Key key)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleHost.SetHudKeyOverride(key);
#endif
        }

        /// <summary>Canvas sorting order for this session (HUD draws one below, DEV button one above).</summary>
        public static void SetSortingOrder(int order)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleHost.SetSortingOrderOverride(order);
#endif
        }

        /// <summary>Drops the SetOpenKey / SetHudToggleKey / SetSortingOrder overrides (back to the settings asset).</summary>
        public static void ClearOverrides()
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleHost.SetOpenKeyOverride(null);
            DevConsoleHost.SetHudKeyOverride(null);
            DevConsoleHost.SetSortingOrderOverride(null);
#endif
        }

        public static UnityEngine.InputSystem.Key OpenKey
        {
#if AMBERSCREEN_DEVCONSOLE
            get => DevConsoleHost.OpenKey;
#else
            get => UnityEngine.InputSystem.Key.None;
#endif
        }

        public static UnityEngine.InputSystem.Key HudToggleKey
        {
#if AMBERSCREEN_DEVCONSOLE
            get => DevConsoleHost.HudToggleKey;
#else
            get => UnityEngine.InputSystem.Key.None;
#endif
        }

        public static int SortingOrder
        {
#if AMBERSCREEN_DEVCONSOLE
            get => DevConsoleHost.SortingOrder;
#else
            get => 0;
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
            if (!s_IsOpen && IsOpenBlocked)
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

        /// <summary>
        /// A dropdown that remembers the pick (no Go button). <paramref name="onChanged"/> runs when the
        /// player picks another item. Read the pick any time with <see cref="GetChoice"/>.
        /// </summary>
        public static void RegisterChoice(
            string section,
            string label,
            Func<string[]> options,
            Action<int> onChanged = null,
            int defaultIndex = 0)
        {
#if AMBERSCREEN_DEVCONSOLE
            bool isNew = DevConsoleRegistry.Find(section, label) == null;
            var entry = DevConsoleRegistry.Upsert(section, label, DevConsoleEntryKind.Dropdown, true, null);
            entry.DropdownOptions = options;
            entry.DropdownChanged = onChanged;
            entry.DropdownIsChoice = true;
            if (isNew)
                entry.DropdownSelected = Math.Max(0, defaultIndex);
            DevConsoleHost.NotifyRegistryChanged();
#endif
        }

        /// <summary>Last picked index of a dropdown or choice (-1 when there is no such entry).</summary>
        public static int GetChoice(string section, string label)
        {
#if AMBERSCREEN_DEVCONSOLE
            var entry = DevConsoleRegistry.Find(section, label);
            return entry != null && entry.Kind == DevConsoleEntryKind.Dropdown ? entry.DropdownSelected : -1;
#else
            return -1;
#endif
        }

        /// <summary>Sets the pick of a dropdown or choice (does not call its callback). False if not found.</summary>
        public static bool SetChoice(string section, string label, int index)
        {
#if AMBERSCREEN_DEVCONSOLE
            var entry = DevConsoleRegistry.Find(section, label);
            if (entry == null || entry.Kind != DevConsoleEntryKind.Dropdown)
                return false;
            entry.DropdownSelected = Math.Max(0, index);
            DevConsoleHost.NotifyRegistryChanged();
            return true;
#else
            return false;
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

        /// <summary>
        /// A readout that can span several lines (value may contain '\n'). The row grows with the text,
        /// up to <paramref name="maxLines"/> lines; extra lines are cut with "...". Pinned, it shows on one line.
        /// </summary>
        public static void RegisterMultilineReadout(
            string section,
            string label,
            Func<string> value,
            float refreshSeconds = 0.5f,
            int maxLines = 12)
        {
#if AMBERSCREEN_DEVCONSOLE
            var entry = DevConsoleRegistry.Upsert(section, label, DevConsoleEntryKind.Readout, true, null);
            entry.ReadoutValue = value;
            entry.ReadoutRefreshSeconds = refreshSeconds > 0f ? refreshSeconds : 0.5f;
            entry.ReadoutMultiline = true;
            entry.ReadoutMaxLines = Math.Max(1, maxLines);
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

        public static void RegisterCommand(string name, string help, Action<string[]> run)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleCommands.Register(name, help, run);
#endif
        }

        public static void ApplyTheme(DevConsoleTheme theme)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleHost.ApplyTheme(theme);
#endif
        }

        public static void SetSectionIcon(string section, Sprite icon)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevConsoleRegistry.SetSectionIcon(section, icon);
            DevConsoleHost.NotifyRegistryChanged();
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
            if (!s_IsOpen && IsOpenBlocked)
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
            if (!IsRailLayout())
                return false;
            int w = DevConsoleView.s_LayoutScreenWidth > 0
                ? DevConsoleView.s_LayoutScreenWidth
                : Screen.width;
            int h = DevConsoleView.s_LayoutScreenHeight > 0
                ? DevConsoleView.s_LayoutScreenHeight
                : Screen.height;
            return h > w;
        }

        static bool ComputeBlocksGame()
        {
            if (!s_IsOpen)
                return false;

            if (IsRailLayout())
            {
                // Section / log window always blocks. Expanded portrait sheet still blocks;
                // the collapsed icon strip does not.
                if (!string.IsNullOrEmpty(s_OpenSectionName))
                    return true;
                if (IsPortraitRail() && !s_Collapsed)
                    return true;
                return false;
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

        /// <summary>
        /// Re-unlock the cursor while the console stays open (commands that start gameplay often
        /// re-lock it). Does not overwrite the saved pre-open cursor state.
        /// </summary>
        internal static void ReapplyCursorWhileOpen()
        {
            if (!s_IsOpen)
                return;
            var settings = DevConsoleSettings.LoadOrDefault();
            if (!settings.showCursorWhileOpen)
                return;
            if (!s_HasSavedCursor)
            {
                s_SavedLock = Cursor.lockState;
                s_SavedCursorVisible = Cursor.visible;
                s_HasSavedCursor = true;
            }

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
            DevConsoleView.ClearLayoutScreenOverride();
            ResetStatics();
        }

        internal static bool IsCollapsed => s_Collapsed;
#endif
    }
}
