#if AMBERSCREEN_DEVCONSOLE
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberScreen.Dev
{
    /// <summary>
    /// A key hint: either bound to an <see cref="InputAction"/> (display string follows the live
    /// binding) or a fixed key string such as "Space" / "LMB".
    /// </summary>
    readonly struct DevHudKeyHint
    {
        public readonly InputAction Action;
        public readonly string KeyText;
        public readonly string Label;

        DevHudKeyHint(InputAction action, string keyText, string label)
        {
            Action = action;
            KeyText = keyText ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public static DevHudKeyHint FromAction(InputAction action, string label) =>
            new DevHudKeyHint(action, null, label);

        public static DevHudKeyHint FromKey(string key, string label) =>
            new DevHudKeyHint(null, key, label);
    }

    static class DevHudKeyHints
    {
        static readonly Dictionary<string, List<DevHudKeyHint>> s_Modes =
            new Dictionary<string, List<DevHudKeyHint>>(StringComparer.Ordinal);
        static string s_ActiveMode = string.Empty;
        static bool s_Dirty = true;
        static bool s_WarnedNullAction;

        public static bool IsDirty => s_Dirty;
        public static string ActiveMode => s_ActiveMode;

        public static void ClearDirty() => s_Dirty = false;

        public static void Reset()
        {
            s_Modes.Clear();
            s_ActiveMode = string.Empty;
            s_Dirty = true;
            s_WarnedNullAction = false;
        }

        public static void SetMode(string mode)
        {
            string next = mode ?? string.Empty;
            if (string.Equals(s_ActiveMode, next, StringComparison.Ordinal))
                return;
            s_ActiveMode = next;
            s_Dirty = true;
        }

        /// <summary>Replaces the hint list for <paramref name="mode"/> with InputAction-based hints.</summary>
        public static void SetKeyHints(string mode, (InputAction action, string label)[] hints)
        {
            if (string.IsNullOrEmpty(mode))
                return;
            var list = new List<DevHudKeyHint>();
            if (hints != null)
            {
                for (int i = 0; i < hints.Length; i++)
                    TryAppend(list, hints[i].action, hints[i].label);
            }

            s_Modes[mode] = list;
            s_Dirty = true;
        }

        /// <summary>Replaces the hint list for <paramref name="mode"/> with fixed-string hints.</summary>
        public static void SetKeyHints(string mode, (string key, string label)[] hints)
        {
            if (string.IsNullOrEmpty(mode))
                return;
            var list = new List<DevHudKeyHint>();
            if (hints != null)
            {
                for (int i = 0; i < hints.Length; i++)
                    TryAppend(list, hints[i].key, hints[i].label);
            }

            s_Modes[mode] = list;
            s_Dirty = true;
        }

        /// <summary>Appends one InputAction hint (mixable with string hints in the same mode).</summary>
        public static void AddKeyHint(string mode, InputAction action, string label)
        {
            if (string.IsNullOrEmpty(mode))
                return;
            var list = GetOrCreate(mode);
            if (TryAppend(list, action, label))
                s_Dirty = true;
        }

        /// <summary>Appends one fixed-string hint (mixable with InputAction hints in the same mode).</summary>
        public static void AddKeyHint(string mode, string key, string label)
        {
            if (string.IsNullOrEmpty(mode))
                return;
            var list = GetOrCreate(mode);
            if (TryAppend(list, key, label))
                s_Dirty = true;
        }

        static List<DevHudKeyHint> GetOrCreate(string mode)
        {
            if (!s_Modes.TryGetValue(mode, out var list) || list == null)
            {
                list = new List<DevHudKeyHint>();
                s_Modes[mode] = list;
            }

            return list;
        }

        static bool TryAppend(List<DevHudKeyHint> list, InputAction action, string label)
        {
            if (action == null)
            {
                if (!s_WarnedNullAction)
                {
                    s_WarnedNullAction = true;
                    Debug.LogWarning(
                        "[DevHud] Key hint skipped: InputAction is null. " +
                        "(Logged once; further null actions are skipped silently.)");
                }

                return false;
            }

            list.Add(DevHudKeyHint.FromAction(action, label));
            return true;
        }

        static bool TryAppend(List<DevHudKeyHint> list, string key, string label)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            list.Add(DevHudKeyHint.FromKey(key, label));
            return true;
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
            string binding = hint.KeyText;
            if (hint.Action != null)
            {
                try
                {
                    binding = hint.Action.GetBindingDisplayString();
                }
                catch
                {
                    binding = string.Empty;
                }

                if (string.IsNullOrEmpty(binding))
                    binding = hint.Action.name;
            }

            if (string.IsNullOrEmpty(binding))
                binding = "?";
            if (string.IsNullOrEmpty(hint.Label))
                return binding;
            return binding + "  " + hint.Label;
        }
    }
}
#endif
