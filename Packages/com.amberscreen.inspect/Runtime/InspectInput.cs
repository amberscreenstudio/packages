using UnityEngine;
#if AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AmberScreen.Inspect
{
    /// <summary>
    /// Reads mouse, keyboard and gamepad through the Input System when it is installed and active,
    /// otherwise through the old Input Manager. Gamepad needs the Input System.
    /// </summary>
    public static class InspectInput
    {
        static int s_Frame = -1;
#if !(AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM) && ENABLE_LEGACY_INPUT_MANAGER
        static bool s_HasLast;
        static Vector2 s_Last;
#endif
        static Vector2 s_Delta;

        /// <summary>Call once per frame (the viewer does) so mouse movement is measured correctly.</summary>
        public static void Poll()
        {
            if (s_Frame == Time.frameCount) return;
            s_Frame = Time.frameCount;
#if AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            s_Delta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            Vector2 p = Input.mousePosition;
            s_Delta = s_HasLast ? p - s_Last : Vector2.zero;
            s_Last = p;
            s_HasLast = true;
#endif
        }

        public static bool TryGetPointer(out Vector2 position)
        {
#if AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            position = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            return mouse != null;
#elif ENABLE_LEGACY_INPUT_MANAGER
            position = Input.mousePosition;
            return Input.mousePresent;
#else
            position = Vector2.zero;
            return false;
#endif
        }

        public static Vector2 PointerDelta
        {
            get
            {
                Poll();
                return s_Delta;
            }
        }

        public static bool PrimaryPressedThisFrame
        {
            get
            {
#if AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonDown(0);
#else
                return false;
#endif
            }
        }

        public static bool PrimaryHeld
        {
            get
            {
#if AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButton(0);
#else
                return false;
#endif
            }
        }

        /// <summary>-1, 0 or +1 per scroll notch (positive = wheel forward).</summary>
        public static float ScrollSteps
        {
            get
            {
#if AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                float y = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER
                float y = Input.mouseScrollDelta.y;
#else
                float y = 0f;
#endif
                return Mathf.Approximately(y, 0f) ? 0f : Mathf.Sign(y);
            }
        }

        /// <summary>Right stick (Input System only).</summary>
        public static Vector2 Stick
        {
            get
            {
#if AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                return Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
#else
                return Vector2.zero;
#endif
            }
        }

        /// <summary>Right trigger minus left trigger (Input System only). Positive = zoom in.</summary>
        public static float ZoomAxis
        {
            get
            {
#if AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                var pad = Gamepad.current;
                return pad != null ? pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue() : 0f;
#else
                return 0f;
#endif
            }
        }

        /// <summary>Escape, right mouse button or gamepad B.</summary>
        public static bool ExitPressed
        {
            get
            {
#if AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
                if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) return true;
                return Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1);
#else
                return false;
#endif
            }
        }

        /// <summary>Tab or gamepad right shoulder.</summary>
        public static bool NextPressed
        {
            get
            {
#if AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && kb.tabKey.wasPressedThisFrame && !kb.shiftKey.isPressed) return true;
                return Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Tab) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift);
#else
                return false;
#endif
            }
        }

        /// <summary>Shift+Tab or gamepad left shoulder.</summary>
        public static bool PreviousPressed
        {
            get
            {
#if AMBERSCREEN_INSPECT_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && kb.tabKey.wasPressedThisFrame && kb.shiftKey.isPressed) return true;
                return Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Tab) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
#else
                return false;
#endif
            }
        }
    }
}
