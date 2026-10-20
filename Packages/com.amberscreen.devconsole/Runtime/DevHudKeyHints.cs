#if AMBERSCREEN_DEVCONSOLE
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace AmberScreen.Dev
{
    readonly struct DevHudKeyHint
    {
        public readonly InputAction Action;
        public readonly string Label;

        public DevHudKeyHint(InputAction action, string label)
        {
            Action = action;
            Label = label ?? string.Empty;
        }
    }

    static class DevHudKeyHints
    {
        static readonly Dictionary<string, List<DevHudKeyHint>> s_Modes =
            new Dictionary<string, List<DevHudKeyHint>>(StringComparer.Ordinal);
        static string s_ActiveMode = string.Empty;
        static bool s_Dirty = true;

        public static bool IsDirty => s_Dirty;
        public static string ActiveMode => s_ActiveMode;

        public static void ClearDirty() => s_Dirty = false;

        public static void Reset()
        {
            s_Modes.Clear();
            s_ActiveMode = string.Empty;
            s_Dirty = true;
        }

        public static void SetMode(string mode)
        {
            string next = mode ?? string.Empty;
            if (string.Equals(s_ActiveMode, next, StringComparison.Ordinal))
                return;
            s_ActiveMode = next;
            s_Dirty = true;
        }

        public static void SetKeyHints(string mode, (InputAction action, string label)[] hints)
        {
            if (string.IsNullOrEmpty(mode))
                return;
            var list = new List<DevHudKeyHint>();
            if (hints != null)
            {
                for (int i = 0; i < hints.Length; i++)
                {
                    if (hints[i].action == null)
                        continue;
                    list.Add(new DevHudKeyHint(hints[i].action, hints[i].label));
                }
            }

            s_Modes[mode] = list;
            s_Dirty = true;
        }

        public static void ClearKeyHints(string mode)
        {
            if (string.IsNullOrEmpty(mode))
                return;
            if (s_Modes.Remove(mode))
                s_Dirty = true;
        }

        public static bool TryGetActiveHints(out List<DevHudKeyHint> hints)
        {
            hints = null;
            if (string.IsNullOrEmpty(s_ActiveMode))
                return false;
            return s_Modes.TryGetValue(s_ActiveMode, out hints) && hints != null && hints.Count > 0;
        }

        public static string FormatHint(DevHudKeyHint hint)
        {
            string binding = string.Empty;
            try
            {
                if (hint.Action != null)
                    binding = hint.Action.GetBindingDisplayString();
            }
            catch
            {
                binding = string.Empty;
            }

            if (string.IsNullOrEmpty(binding))
                binding = hint.Action != null ? hint.Action.name : "?";
            if (string.IsNullOrEmpty(hint.Label))
                return binding;
            return binding + "  " + hint.Label;
        }
    }
}
#endif
