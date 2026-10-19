#if AMBERSCREEN_DEVCONSOLE
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace AmberScreen.DevConsole
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

        DevConsoleView m_View;
        DevConsoleSettings m_Settings;
        GameObject m_OwnedEventSystem;

        float m_CornerHoldTime;
        bool m_GamepadChordLatched;

        // 3-finger tap: begin within 0.25s, all up within 0.5s, move <= 30 dp.
        bool m_TapActive;
        float m_TapFirstBegin;
        float m_TapMaxTravelPx;
        int m_TapBeganCount;
        readonly Vector2[] m_TapOrigin = new Vector2[10];
        readonly int[] m_TapFinger = new int[10];
        int m_TapTracked;

        public static DevConsoleHost Instance => s_Instance;

        internal static bool IsRegistryDirty => s_RegistryDirty;

        public static void ResetStatics()
        {
            s_Instance = null;
            s_RegistryDirty = false;
            s_NotesDirty = false;
        }

        public static void EnsureExists()
        {
            if (s_Instance == null)
                s_Instance = Object.FindFirstObjectByType<DevConsoleHost>();
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
            s_Instance.EnsureEventSystemForOpen();
            s_Instance.m_View?.SetVisible(true);
            s_Instance.m_View?.Rebuild();
        }

        public static void OnClosed()
        {
            if (s_Instance == null)
                return;
            s_Instance.m_View?.SetVisible(false);
            s_Instance.m_View?.SetCollapsed(false);
        }

        void Bootstrap()
        {
            m_Settings = DevConsoleSettings.LoadOrDefault();
            m_View = new DevConsoleView(transform);
            m_View.SetVisible(false);
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            m_View?.Dispose();
            m_View = null;
            if (m_OwnedEventSystem != null)
            {
                Destroy(m_OwnedEventSystem);
                m_OwnedEventSystem = null;
            }

            if (s_Instance == this)
            {
                s_Instance = null;
                DevConsole.NotifyHostDestroyed();
            }
        }

        void OnSceneChanged(Scene a, Scene b)
        {
            if (DevConsole.IsOpen)
                m_View?.RefreshTitle();
        }

        void Update()
        {
            DevConsole.ConsumedCloseThisFrame = false;

            if (m_Settings == null)
                m_Settings = DevConsoleSettings.LoadOrDefault();

            PollOpenCloseInput();
            SyncOwnedEventSystem();

            if (!DevConsole.IsOpen)
            {
                m_View?.TickClosed();
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

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[m_Settings.openKey].wasPressedThisFrame)
            {
                DevConsole.Toggle();
                return;
            }

            if (m_Settings.enableGamepadChord)
                PollGamepadChord();

            if (m_Settings.enableThreeFingerTap)
                PollThreeFingerTap();

            if (m_Settings.enableCornerHold)
                PollCornerHold();
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

            // Fire on release: all fingers up, at least 3 began in the begin window.
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

        void PollCornerHold()
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
            else if (m_Settings.cornerHoldWithMouseInEditor &&
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

        void EnsureEventSystemForOpen()
        {
            if (EventSystem.current != null)
            {
                if (m_OwnedEventSystem != null &&
                    EventSystem.current.gameObject != m_OwnedEventSystem)
                {
                    Destroy(m_OwnedEventSystem);
                    m_OwnedEventSystem = null;
                }
                return;
            }

            if (m_OwnedEventSystem != null)
                return;

            var go = new GameObject("DevConsoleEventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            m_OwnedEventSystem = go;
        }

        void SyncOwnedEventSystem()
        {
            if (!DevConsole.IsOpen)
                return;

            if (EventSystem.current == null)
            {
                EnsureEventSystemForOpen();
                return;
            }

            if (m_OwnedEventSystem != null &&
                EventSystem.current.gameObject != m_OwnedEventSystem)
            {
                Destroy(m_OwnedEventSystem);
                m_OwnedEventSystem = null;
            }
        }
    }
}
#endif
