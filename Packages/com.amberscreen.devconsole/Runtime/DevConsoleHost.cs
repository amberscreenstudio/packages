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
        static DevConsoleHost s_Instance;
        static bool s_RegistryDirty;
        static bool s_NotesDirty;

        DevConsoleView m_View;
        DevConsoleSettings m_Settings;
        float m_CornerHoldTime;
        bool m_GamepadChordLatched;
        bool m_ThreeFingerLatched;

        public static DevConsoleHost Instance => s_Instance;

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
            if (s_Instance != null && DevConsole.IsOpen)
                s_Instance.m_View?.Rebuild();
        }

        public static void NotifyNotesChanged()
        {
            s_NotesDirty = true;
            if (s_Instance != null && DevConsole.IsOpen)
                s_Instance.m_View?.RefreshNotes();
        }

        public static void OnOpened()
        {
            EnsureExists();
            s_Instance.m_View?.SetVisible(true);
            s_Instance.m_View?.Rebuild();
            s_Instance.m_View?.SelectFirst();
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
            EnsureEventSystem();
            m_View = new DevConsoleView(transform);
            m_View.SetVisible(false);
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            m_View?.Dispose();
            m_View = null;
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
            // Closed path: poll open bindings only — no string/list allocations.
            PollOpenCloseInput();

            if (!DevConsole.IsOpen)
                return;

            if (s_RegistryDirty)
            {
                s_RegistryDirty = false;
                m_View?.Rebuild();
            }

            if (s_NotesDirty)
            {
                s_NotesDirty = false;
                m_View?.RefreshNotes();
            }

            m_View?.TickOpen(Time.unscaledDeltaTime);
            PollOpenNavigation();
        }

        void PollOpenCloseInput()
        {
            if (!DevConsole.CanActivate())
                return;

            if (m_Settings == null)
                m_Settings = DevConsoleSettings.LoadOrDefault();

            var keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard[m_Settings.openKey].wasPressedThisFrame)
                {
                    DevConsole.Toggle();
                    return;
                }

                if (DevConsole.IsOpen && keyboard.escapeKey.wasPressedThisFrame)
                {
                    DevConsole.Close();
                    return;
                }
            }

            if (m_Settings.enableGamepadChord)
                PollGamepadChord();

            if (m_Settings.enableThreeFingerTap)
                PollThreeFingerTap();

            if (m_Settings.enableCornerHold)
                PollCornerHold();
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
                m_ThreeFingerLatched = false;
                return;
            }

            int pressed = 0;
            var touches = touch.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].press.isPressed)
                    pressed++;
            }

            if (pressed >= 3)
            {
                if (!m_ThreeFingerLatched)
                {
                    m_ThreeFingerLatched = true;
                    DevConsole.Toggle();
                }
            }
            else
            {
                m_ThreeFingerLatched = false;
            }
        }

        void PollCornerHold()
        {
            bool inCorner = false;
            Vector2 pos = default;

            if (Touchscreen.current != null)
            {
                var primary = Touchscreen.current.primaryTouch;
                if (primary.press.isPressed)
                {
                    pos = primary.position.ReadValue();
                    inCorner = true;
                }
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                pos = Mouse.current.position.ReadValue();
                inCorner = true;
            }

            if (!inCorner)
            {
                m_CornerHoldTime = 0f;
                return;
            }

            float w = Screen.width;
            float h = Screen.height;
            bool topLeft = pos.x <= w * 0.15f && pos.y >= h * (1f - 0.15f);
            if (!topLeft)
            {
                m_CornerHoldTime = 0f;
                return;
            }

            m_CornerHoldTime += Time.unscaledDeltaTime;
            if (m_CornerHoldTime >= 2f)
            {
                m_CornerHoldTime = 0f;
                DevConsole.Toggle();
            }
        }

        void PollOpenNavigation()
        {
            var pad = Gamepad.current;
            if (pad == null || !DevConsole.IsOpen)
                return;

            if (pad.buttonEast.wasPressedThisFrame)
            {
                if (m_View != null && m_View.TryCloseDropdown())
                    return;
                DevConsole.Close();
            }
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                if (EventSystem.current.GetComponent<InputSystemUIInputModule>() == null &&
                    EventSystem.current.GetComponent<BaseInputModule>() == null)
                {
                    EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                return;
            }

            var go = new GameObject("EventSystem");
            DontDestroyOnLoad(go);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
#endif
