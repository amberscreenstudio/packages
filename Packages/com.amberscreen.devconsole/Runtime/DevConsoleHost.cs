#if AMBERSCREEN_DEVCONSOLE
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

        static DevConsoleHost s_Instance;
        static bool s_RegistryDirty;
        static bool s_NotesDirty;
        static DevConsoleSettings s_CachedSettings;

        DevConsoleView m_View;
        GameObject m_OwnedEventSystem;

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
                s_Instance = Object.FindAnyObjectByType<DevConsoleHost>();
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
            m_View = new DevConsoleView(transform);
            m_View.SetVisible(false);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.activeSceneChanged -= OnSceneChanged;
            m_View?.Dispose();
            m_View = null;
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

            if (settings.enableGamepadChord)
                PollGamepadChord();

            if (settings.enableThreeFingerTap)
                PollThreeFingerTap();

            if (settings.enableCornerHold)
                PollCornerHold(settings);
        }

        void PollCloseWhileOpen()
        {
            if (!DevConsole.IsOpen || !DevConsole.BlocksGame)
                return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (m_View != null && m_View.TryCloseDropdown())
                {
                    DevConsole.ConsumedCloseThisFrame = true;
                    return;
                }

                DevConsole.ConsumedCloseThisFrame = true;
                DevConsole.Close();
                return;
            }

            var pad = Gamepad.current;
            if (pad != null && pad.buttonEast.wasPressedThisFrame)
            {
                if (m_View != null && m_View.TryCloseDropdown())
                {
                    DevConsole.ConsumedCloseThisFrame = true;
                    return;
                }

                DevConsole.ConsumedCloseThisFrame = true;
                DevConsole.Close();
            }
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

            if (HasForeignEventSystem())
                return;

            if (m_OwnedEventSystem != null)
                return;

            var go = new GameObject("DevConsoleEventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            m_OwnedEventSystem = go;
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
            var systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
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
