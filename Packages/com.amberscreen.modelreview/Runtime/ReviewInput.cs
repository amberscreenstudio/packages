using UnityEngine;
#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace AmberScreen.ModelReview
{
    /// <summary>Input wrapper that compiles with either Input System or legacy Input.</summary>
    public static class ReviewInput
    {
        public static bool GetKeyDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return key switch
            {
                KeyCode.Alpha1 => kb.digit1Key.wasPressedThisFrame,
                KeyCode.Alpha2 => kb.digit2Key.wasPressedThisFrame,
                KeyCode.Alpha3 => kb.digit3Key.wasPressedThisFrame,
                KeyCode.F => kb.fKey.wasPressedThisFrame,
                KeyCode.I => kb.iKey.wasPressedThisFrame,
                KeyCode.R => kb.rKey.wasPressedThisFrame,
                KeyCode.G => kb.gKey.wasPressedThisFrame,
                KeyCode.C => kb.cKey.wasPressedThisFrame,
                KeyCode.T => kb.tKey.wasPressedThisFrame,
                KeyCode.H => kb.hKey.wasPressedThisFrame,
                KeyCode.W => kb.wKey.wasPressedThisFrame,
                KeyCode.A => kb.aKey.wasPressedThisFrame,
                KeyCode.S => kb.sKey.wasPressedThisFrame,
                KeyCode.D => kb.dKey.wasPressedThisFrame,
                KeyCode.Q => kb.qKey.wasPressedThisFrame,
                KeyCode.E => kb.eKey.wasPressedThisFrame,
                KeyCode.LeftShift => kb.leftShiftKey.wasPressedThisFrame,
                KeyCode.RightShift => kb.rightShiftKey.wasPressedThisFrame,
                _ => false
            };
#else
            return Input.GetKeyDown(key);
#endif
        }

        public static bool GetKey(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return key switch
            {
                KeyCode.W => kb.wKey.isPressed,
                KeyCode.A => kb.aKey.isPressed,
                KeyCode.S => kb.sKey.isPressed,
                KeyCode.D => kb.dKey.isPressed,
                KeyCode.Q => kb.qKey.isPressed,
                KeyCode.E => kb.eKey.isPressed,
                KeyCode.LeftShift => kb.leftShiftKey.isPressed,
                KeyCode.RightShift => kb.rightShiftKey.isPressed,
                _ => false
            };
#else
            return Input.GetKey(key);
#endif
        }

        public static bool GetMouseButton(int button)
        {
#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
            var mouse = Mouse.current;
            if (mouse == null) return false;
            return button switch
            {
                0 => mouse.leftButton.isPressed,
                1 => mouse.rightButton.isPressed,
                2 => mouse.middleButton.isPressed,
                _ => false
            };
#else
            return Input.GetMouseButton(button);
#endif
        }

        public static Vector2 MouseDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
                var mouse = Mouse.current;
                return mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
#else
                return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
#endif
            }
        }

        public static float ScrollY
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
                var mouse = Mouse.current;
                return mouse != null ? mouse.scroll.ReadValue().y : 0f;
#else
                return Input.mouseScrollDelta.y;
#endif
            }
        }

        public static bool ShiftHeld => GetKey(KeyCode.LeftShift) || GetKey(KeyCode.RightShift);
    }
}
