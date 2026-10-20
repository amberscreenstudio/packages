#if AMBERSCREEN_DEVCONSOLE
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace AmberScreen.Dev
{
    [DefaultExecutionOrder(-1000)]
    sealed class DevConsoleHost : MonoBehaviour
    {
        const float ThreeFingerBeginWindow = 0.25f;
        const float ThreeFingerEndWindow = 0.5f;
        const float ThreeFingerMaxMoveDp = 30f;
        const string StatePrefsKey = "AmberScreen.DevConsole.State";

        internal const string TmpEssentialsMissingMessage =
            "[DevConsole] TextMeshPro Essential Resources are missing. " +
            "Import them via Window > TextMeshPro > Import TMP Essential Resources, then re-enter Play mode.";

        /// <summary>Edit-mode tests override this to simulate missing TMP Essentials.</summary>
        internal static Func<bool> TmpReady = DefaultTmpReady;

#if UNITY_EDITOR
        /// <summary>Editor assembly sets this to show a one-shot import dialog.</summary>
        internal static Action TmpEssentialsMissingEditorHook;
#endif

        static DevConsoleHost s_Instance;
        static bool s_RegistryDirty;
        static bool s_NotesDirty;
        static DevConsoleSettings s_CachedSettings;
        static string s_RememberedSection;
        static bool s_RememberedOpen;
        static bool s_RememberedCollapsed;
        static bool s_RememberedHudVisible = true;
        static bool s_StateLoaded;
        static bool s_LoggedTmpMissing;
        static bool s_HudHadLines;

        DevConsoleView m_View;
        DevHudView m_HudView;
        GameObject m_OwnedEventSystem;
        static bool s_WarnedWrongSceneModule;

        float m_CornerHoldTime;
        bool m_GamepadChordLatched;

        bool m_TapActive;
        float m_TapFirstBegin;
        float m_TapMaxTravelPx;
        int m_TapBeganCount;
        readonly Vector2[] m_TapOrigin = new Vector2[10];
        readonly int[] m_TapFinger = new int[10];
        int m_TapTracked;

        public static DevConsoleHost Instance => s_Instance;

        internal static bool IsRegistryDirty => s_RegistryDirty;

        internal static DevConsoleSettings Settings =>
            s_CachedSettings != null ? s_CachedSettings : (s_CachedSettings = DevConsoleSettings.LoadOrDefault());

        public static void ResetStatics()
        {
            s_Instance = null;
            s_RegistryDirty = false;
            s_NotesDirty = false;
            s_CachedSettings = null;
            s_WarnedWrongSceneModule = false;
            s_RememberedSection = null;
            s_RememberedOpen = false;
            s_RememberedCollapsed = false;
            s_RememberedHudVisible = true;
            s_StateLoaded = false;
            s_LoggedTmpMissing = false;
            s_HudHadLines = false;
            TmpReady = DefaultTmpReady;
        }

        internal static DevHudView HudView => s_Instance != null ? s_Instance.m_HudView : null;

        internal static void NotifyHudVisibilityChanged()
        {
            if (s_Instance == null)
                return;
            s_Instance.m_HudView?.OnSettingsOrVisibilityChanged();
            s_Instance.m_View?.ApplyLayoutPublic();
        }

        internal static void NotifyHudStructureChanged()
        {
            // Structure dirty is handled inside DevHudView.Tick; only re-run console layout
            // when the empty/non-empty reserve boundary flips.
            bool hasLines = DevHudRegistry.TotalLineCount() > 0;
            if (hasLines == s_HudHadLines)
                return;
            s_HudHadLines = hasLines;
            if (s_Instance?.m_View != null)
                s_Instance.m_View.ApplyLayoutPublic();
        }

        static bool DefaultTmpReady() => TMP_Settings.instance != null;

        internal static bool IsTmpReady() =>
            TmpReady == null ? DefaultTmpReady() : TmpReady.Invoke();

        internal static void ReportTmpEssentialsMissing()
        {
            if (!s_LoggedTmpMissing)
            {
                s_LoggedTmpMissing = true;
                Debug.LogError(TmpEssentialsMissingMessage);
            }
#if UNITY_EDITOR
            // Only prompt when TMP is actually missing (not when tests stub TmpReady).
            if (TMP_Settings.instance == null)
                TmpEssentialsMissingEditorHook?.Invoke();
#endif
        }

        internal void NotifySectionWindowChanged()
        {
            m_View?.OnSectionWindowChanged();
        }

        internal static bool IsPointerOverConsoleUi()
        {
            return s_Instance != null && s_Instance.m_View != null &&
                   s_Instance.m_View.IsPointerOverConsole();
        }

        internal static void TryRestoreRememberedOpen()
        {
            LoadState();
            if (!Settings.rememberState || !s_RememberedOpen)
                return;
            if (!DevConsole.CanActivate())
                return;
            DevConsole.Open();
            if (s_RememberedCollapsed)
                DevConsole.SetCollapsed(true);
        }

        internal static void ApplyRememberedSectionOnOpen()
        {
            LoadState();
            if (!Settings.rememberState || string.IsNullOrEmpty(s_RememberedSection))
                return;
            DevConsole.SetOpenSection(s_RememberedSection);
        }

        internal static void PersistStateIfNeeded()
        {
            if (!Settings.rememberState || !DevConsole.CanActivate())
                return;

            int rail = 0;
            if (DevConsole.IsOpen)
                rail = DevConsole.IsCollapsed ? 2 : 1;
            string section = DevConsole.OpenSectionName ?? string.Empty;
            int hud = DevHud.Visible ? 1 : 0;
            string json =
                $"{{\"rail\":{rail},\"section\":\"{EscapeJson(section)}\",\"hud\":{hud}}}";
            PlayerPrefs.SetString(StatePrefsKey, json);
            PlayerPrefs.Save();
            s_RememberedOpen = rail != 0;
            s_RememberedCollapsed = rail == 2;
            s_RememberedSection = section;
            s_RememberedHudVisible = hud != 0;
            s_StateLoaded = true;
        }

        static void LoadState()
        {
            if (s_StateLoaded)
                return;
            s_StateLoaded = true;
            s_RememberedHudVisible = Settings.hudShowOnStart;
            if (!PlayerPrefs.HasKey(StatePrefsKey))
                return;
            string json = PlayerPrefs.GetString(StatePrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            int rail = ExtractInt(json, "rail");
            s_RememberedOpen = rail == 1 || rail == 2;
            s_RememberedCollapsed = rail == 2;
            s_RememberedSection = ExtractString(json, "section");
            if (json.IndexOf("\"hud\":", System.StringComparison.Ordinal) >= 0)
                s_RememberedHudVisible = ExtractInt(json, "hud") != 0;
        }

        static int ExtractInt(string json, string key)
        {
            string token = "\"" + key + "\":";
            int i = json.IndexOf(token, System.StringComparison.Ordinal);
            if (i < 0)
                return 0;
            i += token.Length;
            int end = i;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
                end++;
            if (int.TryParse(json.Substring(i, end - i), out int v))
                return v;
            return 0;
        }

        static string ExtractString(string json, string key)
        {
            string token = "\"" + key + "\":\"";
            int i = json.IndexOf(token, System.StringComparison.Ordinal);
            if (i < 0)
                return string.Empty;
            i += token.Length;
            int end = i;
            while (end < json.Length && json[end] != '"')
                end++;
            return json.Substring(i, end - i);
        }

        static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        public static void ReloadCachedSettings(DevConsoleSettings settings = null)
        {
            s_CachedSettings = settings != null ? settings : DevConsoleSettings.LoadOrDefault();
            if (s_Instance != null)
                s_Instance.m_View?.OnSettingsReloaded();
        }

        public static void EnsureExists()
        {
            if (s_Instance == null)
                s_Instance = UnityEngine.Object.FindAnyObjectByType<DevConsoleHost>();
            if (s_Instance != null)
                return;

            var go = new GameObject("[AmberScreen DevConsole]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<DevConsoleHost>();
            s_Instance.Bootstrap();
        }

        public static void NotifyRegistryChanged()
        {
            s_RegistryDirty = true;
        }

        public static void NotifyNotesChanged()
        {
            s_NotesDirty = true;
        }

        public static void ClearRegistryDirty()
        {
            s_RegistryDirty = false;
        }

        public static void OnOpened()
        {
            EnsureExists();
            s_Instance.EnsureEventSystem();
            s_Instance.m_View?.SetVisible(true);
            s_Instance.m_View?.Rebuild();
        }

        public static void OnClosed()
        {
            if (s_Instance == null)
                return;
            s_Instance.m_View?.SetVisible(false);
            s_Instance.m_View?.SetCollapsed(false);
            s_Instance.CleanupEventSystemIfUnused();
        }

        internal static void NotifyDevButtonShown(bool shown)
        {
            EnsureExists();
            if (shown)
                s_Instance.EnsureEventSystem();
            else
                s_Instance.CleanupEventSystemIfUnused();
        }

        void Bootstrap()
        {
            s_CachedSettings = DevConsoleSettings.LoadOrDefault();

            if (!IsTmpReady())
            {
                ReportTmpEssentialsMissing();
                m_View = null;
                m_HudView = null;
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.activeSceneChanged += OnSceneChanged;
                return;
            }

            LoadState();
            DevHud.ApplyRememberedVisible(
                Settings.rememberState ? s_RememberedHudVisible : Settings.hudShowOnStart);

            try
            {
                m_View = new DevConsoleView(transform);
                m_View.SetVisible(false);
                m_HudView = new DevHudView(transform);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                m_View = null;
                m_HudView?.Dispose();
                m_HudView = null;
                DestroyHalfBuiltCanvases();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        void DestroyHalfBuiltCanvases()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null && child.GetComponent<Canvas>() != null)
                    Destroy(child.gameObject);
            }
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.activeSceneChanged -= OnSceneChanged;
            m_View?.Dispose();
            m_View = null;
            m_HudView?.Dispose();
            m_HudView = null;
            DestroyOwnedEventSystem();

            if (s_Instance == this)
            {
                s_Instance = null;
                DevConsole.NotifyHostDestroyed();
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ReconcileOwnedEventSystem();
            if (DevConsole.IsOpen)
                m_View?.RefreshTitle();
        }

        void OnSceneChanged(Scene a, Scene b)
        {
            if (DevConsole.IsOpen)
                m_View?.RefreshTitle();
        }

        void Update()
        {
            DevConsole.ConsumedCloseThisFrame = false;

            // HUD ticks even when the console is closed.
            m_HudView?.Tick(Time.unscaledDeltaTime);

            // Closed path: input only (+ cheap DEV-button safe-area compare, no allocations).
            PollOpenCloseInput();

            if (!DevConsole.IsOpen)
            {
                m_View?.MaybeRepositionDevButton();
                return;
            }

            if (s_RegistryDirty)
                m_View?.Rebuild();

            if (s_NotesDirty)
            {
                s_NotesDirty = false;
                m_View?.RefreshNotes();
            }

            m_View?.TickOpen(Time.unscaledDeltaTime);
            PollCloseWhileOpen();
        }

        void PollOpenCloseInput()
        {
            if (!DevConsole.CanActivate())
                return;

            var settings = Settings;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[settings.openKey].wasPressedThisFrame)
            {
                DevConsole.Toggle();
                return;
            }

            if (keyboard != null &&
                settings.hudToggleKey != settings.openKey &&
                keyboard[settings.hudToggleKey].wasPressedThisFrame)
            {
                DevHud.Toggle();
            }

            if (settings.enableGamepadChord)
                PollGamepadChord();

            if (settings.enableThreeFingerTap)
                PollThreeFingerTap();

            if (settings.enableCornerHold)
                PollCornerHold(settings);
        }

        void PollCloseWhileOpen()
        {
            if (!DevConsole.IsOpen)
                return;

            var keyboard = Keyboard.current;
            bool esc = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            var pad = Gamepad.current;
            bool cancel = pad != null && pad.buttonEast.wasPressedThisFrame;
            if (!esc && !cancel)
                return;

            if (m_View != null && m_View.TryCloseDropdown())
            {
                DevConsole.ConsumedCloseThisFrame = true;
                return;
            }

            if (!string.IsNullOrEmpty(DevConsole.OpenSectionName))
            {
                DevConsole.ConsumedCloseThisFrame = true;
                DevConsole.CloseSection();
                return;
            }

            if (DevConsole.IsRailLayout())
            {
                DevConsole.ConsumedCloseThisFrame = true;
                DevConsole.Close();
                return;
            }

            if (!DevConsole.BlocksGame)
                return;

            DevConsole.ConsumedCloseThisFrame = true;
            DevConsole.Close();
        }

        void PollGamepadChord()
        {
            var pad = Gamepad.current;
            if (pad == null)
            {
                m_GamepadChordLatched = false;
                return;
            }

            bool held = pad.selectButton.isPressed && pad.startButton.isPressed;
            if (held)
            {
                if (!m_GamepadChordLatched)
                {
                    m_GamepadChordLatched = true;
                    DevConsole.Toggle();
                }
            }
            else
            {
                m_GamepadChordLatched = false;
            }
        }

        void PollThreeFingerTap()
        {
            var touch = Touchscreen.current;
            if (touch == null)
            {
                ResetThreeFingerTap();
                return;
            }

            float maxMovePx = ThreeFingerMaxMoveDp * ((Screen.dpi > 0f ? Screen.dpi : 160f) / 160f);
            var touches = touch.touches;
            int touchCount = touches.Count;
            int pressed = 0;

            for (int i = 0; i < touchCount; i++)
            {
                var t = touches[i];
                if (t.press.wasPressedThisFrame)
                {
                    if (m_TapTracked < m_TapFinger.Length)
                    {
                        if (!m_TapActive)
                        {
                            m_TapActive = true;
                            m_TapFirstBegin = Time.unscaledTime;
                            m_TapMaxTravelPx = 0f;
                            m_TapBeganCount = 0;
                            m_TapTracked = 0;
                        }

                        if (Time.unscaledTime - m_TapFirstBegin <= ThreeFingerBeginWindow)
                        {
                            m_TapFinger[m_TapTracked] = t.touchId.ReadValue();
                            m_TapOrigin[m_TapTracked] = t.position.ReadValue();
                            m_TapTracked++;
                            m_TapBeganCount++;
                        }
                    }
                }

                if (t.press.isPressed)
                {
                    pressed++;
                    if (m_TapActive)
                    {
                        int id = t.touchId.ReadValue();
                        Vector2 pos = t.position.ReadValue();
                        for (int s = 0; s < m_TapTracked; s++)
                        {
                            if (m_TapFinger[s] != id)
                                continue;
                            m_TapMaxTravelPx = Mathf.Max(
                                m_TapMaxTravelPx, Vector2.Distance(pos, m_TapOrigin[s]));
                            break;
                        }
                    }
                }
            }

            if (!m_TapActive)
                return;

            float elapsed = Time.unscaledTime - m_TapFirstBegin;
            if (m_TapMaxTravelPx > maxMovePx || elapsed > ThreeFingerEndWindow)
            {
                ResetThreeFingerTap();
                return;
            }

            if (pressed == 0)
            {
                if (m_TapBeganCount >= 3 &&
                    elapsed <= ThreeFingerEndWindow &&
                    m_TapMaxTravelPx <= maxMovePx)
                {
                    DevConsole.Toggle();
                }
                ResetThreeFingerTap();
            }
        }

        void ResetThreeFingerTap()
        {
            m_TapActive = false;
            m_TapBeganCount = 0;
            m_TapTracked = 0;
            m_TapMaxTravelPx = 0f;
        }

        void PollCornerHold(DevConsoleSettings settings)
        {
            bool inCorner = false;
            Vector2 pos = default;

            var touch = Touchscreen.current;
            if (touch != null)
            {
                var touches = touch.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (!touches[i].press.isPressed)
                        continue;
                    pos = touches[i].position.ReadValue();
                    inCorner = true;
                    break;
                }
            }
#if UNITY_EDITOR
            else if (settings.cornerHoldWithMouseInEditor &&
                     Mouse.current != null &&
                     Mouse.current.leftButton.isPressed)
            {
                pos = Mouse.current.position.ReadValue();
                inCorner = true;
            }
#endif

            if (!inCorner)
            {
                m_CornerHoldTime = 0f;
                return;
            }

            Rect safe = Screen.safeArea;
            float cornerW = safe.width * 0.15f;
            float cornerH = safe.height * 0.15f;
            bool topLeft =
                pos.x >= safe.x && pos.x <= safe.x + cornerW &&
                pos.y >= safe.y + safe.height - cornerH && pos.y <= safe.y + safe.height;

            if (!topLeft)
            {
                m_CornerHoldTime = 0f;
                return;
            }

            m_CornerHoldTime += Time.unscaledDeltaTime;
            if (m_CornerHoldTime >= 2f)
            {
                m_CornerHoldTime = 0f;
                if (!DevConsole.IsOpen)
                    DevConsole.Open();
            }
        }

        internal void EnsureEventSystem()
        {
            ReconcileOwnedEventSystem();

            if (TryConfigureForeignEventSystem())
                return;

            if (m_OwnedEventSystem != null)
            {
                EnsureModuleActions(m_OwnedEventSystem.GetComponent<InputSystemUIInputModule>());
                return;
            }

            var go = new GameObject("DevConsoleEventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            m_OwnedEventSystem = go;
        }

        bool TryConfigureForeignEventSystem()
        {
            var systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            EventSystem foreign = null;
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null)
                    continue;
                if (m_OwnedEventSystem != null && systems[i].gameObject == m_OwnedEventSystem)
                    continue;
                foreign = systems[i];
                break;
            }

            if (foreign == null)
                return false;

            var module = foreign.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                if (!s_WarnedWrongSceneModule)
                {
                    s_WarnedWrongSceneModule = true;
                    var other = foreign.GetComponent<BaseInputModule>();
                    string typeName = other != null ? other.GetType().Name : "none";
                    Debug.LogWarning(
                        $"[DevConsole] Scene EventSystem uses {typeName}; console UI needs InputSystemUIInputModule");
                }

                return true;
            }

            EnsureModuleActions(module);
            return true;
        }

        static void EnsureModuleActions(InputSystemUIInputModule module)
        {
            if (module == null)
                return;
            if (module.actionsAsset == null)
                module.AssignDefaultActions();
        }

        void CleanupEventSystemIfUnused()
        {
            ReconcileOwnedEventSystem();

            bool needEs = DevConsole.IsOpen ||
                          (Settings.showOnScreenDevButton && !DevConsole.IsOpen &&
                           m_View != null && m_View.IsDevButtonVisible);
            if (!needEs)
                DestroyOwnedEventSystem();
        }

        void ReconcileOwnedEventSystem()
        {
            if (m_OwnedEventSystem == null)
                return;

            if (HasForeignEventSystem())
                DestroyOwnedEventSystem();
        }

        bool HasForeignEventSystem()
        {
            var systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null)
                    continue;
                if (m_OwnedEventSystem != null && systems[i].gameObject == m_OwnedEventSystem)
                    continue;
                return true;
            }
            return false;
        }

        void DestroyOwnedEventSystem()
        {
            if (m_OwnedEventSystem == null)
                return;
            Destroy(m_OwnedEventSystem);
            m_OwnedEventSystem = null;
        }
    }
}
#endif
