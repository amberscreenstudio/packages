using UnityEngine;
#if AMBERSCREEN_SETTINGS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AmberScreen.Settings
{
    /// <summary>Back/close press for whichever input handling the project uses.</summary>
    static class SettingsMenuInput
    {
        public static bool BackPressedThisFrame()
        {
#if AMBERSCREEN_SETTINGS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
            var pad = Gamepad.current;
            return pad != null && pad.buttonEast.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
    }
}
