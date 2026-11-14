#if AMBERSCREEN_SETTINGS_INPUTSYSTEM
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberScreen.Settings
{
    /// <summary>
    /// Optional key rebinding (only compiled when the Input System package is installed).
    /// Put it next to your InputActionAsset user; it loads saved overrides on enable and saves after each rebind.
    /// </summary>
    public sealed class SettingsRebinding : MonoBehaviour
    {
        public InputActionAsset actions;

        InputActionRebindingExtensions.RebindingOperation m_Operation;

        public bool IsRebinding => m_Operation != null;

        void OnEnable()
        {
            if (actions == null) return;
            var data = GameSettings.Current;
            if (data != null && !string.IsNullOrEmpty(data.bindingOverrides))
                actions.LoadBindingOverridesFromJson(data.bindingOverrides);
        }

        void OnDisable() => Cancel();

        /// <summary>Waits for the next control press and binds it. <paramref name="done"/> gets true when bound.</summary>
        public void StartRebind(InputAction action, int bindingIndex, Action<bool> done = null)
        {
            if (action == null) { done?.Invoke(false); return; }
            Cancel();
            bool wasEnabled = action.enabled;
            action.Disable();
            m_Operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op => Finish(action, wasEnabled, true, done))
                .OnCancel(op => Finish(action, wasEnabled, false, done))
                .Start();
        }

        public void Cancel()
        {
            m_Operation?.Cancel();
        }

        /// <summary>Removes every override and saves.</summary>
        public void ResetAll()
        {
            if (actions == null) return;
            actions.RemoveAllBindingOverrides();
            GameSettings.SetBindingOverrides("");
            GameSettings.Save();
        }

        void Finish(InputAction action, bool wasEnabled, bool bound, Action<bool> done)
        {
            m_Operation?.Dispose();
            m_Operation = null;
            if (wasEnabled) action.Enable();
            if (bound && actions != null)
            {
                GameSettings.SetBindingOverrides(actions.SaveBindingOverridesAsJson());
                GameSettings.Save();
            }
            done?.Invoke(bound);
        }
    }
}
#endif
