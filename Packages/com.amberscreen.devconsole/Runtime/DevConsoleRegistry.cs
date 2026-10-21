#if AMBERSCREEN_DEVCONSOLE
using System;
using System.Collections.Generic;

namespace AmberScreen.Dev
{
    enum DevConsoleEntryKind
    {
        Button,
        Toggle,
        Dropdown,
        Confirm,
        Readout
    }

    sealed class DevConsoleEntry
    {
        public string Section;
        public string Label;
        public string Caption;
        public DevConsoleEntryKind Kind;
        public bool Visible = true;
        public Func<bool> Available;
        public Action Action;
        public Func<bool> ToggleGet;
        public Action<bool> ToggleSet;
        public Func<string[]> DropdownOptions;
        public Action<int> DropdownGo;
        public Func<string> ReadoutValue;
        public float ReadoutRefreshSeconds = 0.5f;
        public int RegistrationOrder;
    }

    sealed class DevConsoleSection
    {
        public string Name;
        public int FirstRegistrationOrder;
        public readonly List<DevConsoleEntry> Entries = new List<DevConsoleEntry>();
    }

    sealed class DevConsoleQuickEntry
    {
        public string Label;
        public Action Action;
        public Func<bool> Available;
        public int Order;
    }

    sealed class DevConsolePinnedReadout
    {
        public string Section;
        public string Label;
    }

    static class DevConsoleRegistry
    {
        static readonly Dictionary<string, DevConsoleSection> s_Sections =
            new Dictionary<string, DevConsoleSection>(StringComparer.Ordinal);
        static readonly List<string> s_SectionOrderOverride = new List<string>();
        static readonly List<string> s_Notes = new List<string>(50);
        static readonly List<DevConsoleQuickEntry> s_Quicks = new List<DevConsoleQuickEntry>();
        static readonly List<DevConsolePinnedReadout> s_Pins = new List<DevConsolePinnedReadout>();
        static readonly Dictionary<string, UnityEngine.Sprite> s_SectionIcons =
            new Dictionary<string, UnityEngine.Sprite>(StringComparer.Ordinal);
        static string s_Status = string.Empty;
        static int s_NextOrder;

        public static string Status => s_Status;
        public static IReadOnlyList<string> Notes => s_Notes;
        public static IReadOnlyList<DevConsoleQuickEntry> Quicks => s_Quicks;
        public static IReadOnlyList<DevConsolePinnedReadout> Pins => s_Pins;

        public static void Reset()
        {
            s_Sections.Clear();
            s_SectionOrderOverride.Clear();
            s_Notes.Clear();
            s_Quicks.Clear();
            s_Pins.Clear();
            s_SectionIcons.Clear();
            s_Status = string.Empty;
            s_NextOrder = 0;
        }

        public static void SetSectionIcon(string section, UnityEngine.Sprite icon)
        {
            if (string.IsNullOrEmpty(section))
                return;
            if (icon == null)
                s_SectionIcons.Remove(section);
            else
                s_SectionIcons[section] = icon;
        }

        public static UnityEngine.Sprite GetSectionIcon(string section)
        {
            if (string.IsNullOrEmpty(section))
                return null;
            return s_SectionIcons.TryGetValue(section, out var s) ? s : null;
        }

        public static void SetSectionOrder(string[] sections)
        {
            s_SectionOrderOverride.Clear();
            if (sections == null)
                return;
            for (int i = 0; i < sections.Length; i++)
            {
                if (!string.IsNullOrEmpty(sections[i]) && !s_SectionOrderOverride.Contains(sections[i]))
                    s_SectionOrderOverride.Add(sections[i]);
            }
        }

        public static void Note(string message)
        {
            s_Status = message ?? string.Empty;
            if (string.IsNullOrEmpty(message))
                return;
            s_Notes.Add(message);
            while (s_Notes.Count > 50)
                s_Notes.RemoveAt(0);
        }

        public static void UpsertQuick(string label, Action action, Func<bool> available)
        {
            label = label ?? string.Empty;
            for (int i = 0; i < s_Quicks.Count; i++)
            {
                if (string.Equals(s_Quicks[i].Label, label, StringComparison.Ordinal))
                {
                    s_Quicks[i].Action = action;
                    s_Quicks[i].Available = available;
                    return;
                }
            }

            s_Quicks.Add(new DevConsoleQuickEntry
            {
                Label = label,
                Action = action,
                Available = available,
                Order = s_NextOrder++
            });
        }

        public static bool UnregisterQuick(string label)
        {
            label = label ?? string.Empty;
            for (int i = 0; i < s_Quicks.Count; i++)
            {
                if (string.Equals(s_Quicks[i].Label, label, StringComparison.Ordinal))
                {
                    s_Quicks.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public static void PinReadout(string section, string label)
        {
            section = section ?? string.Empty;
            label = label ?? string.Empty;
            for (int i = 0; i < s_Pins.Count; i++)
            {
                if (string.Equals(s_Pins[i].Section, section, StringComparison.Ordinal) &&
                    string.Equals(s_Pins[i].Label, label, StringComparison.Ordinal))
                    return;
            }

            s_Pins.Add(new DevConsolePinnedReadout { Section = section, Label = label });
        }

        public static DevConsoleEntry Upsert(
            string section,
            string label,
            DevConsoleEntryKind kind,
            bool visible,
            Func<bool> available)
        {
            section = section ?? string.Empty;
            label = label ?? string.Empty;

            if (!s_Sections.TryGetValue(section, out var sec))
            {
                sec = new DevConsoleSection
                {
                    Name = section,
                    FirstRegistrationOrder = s_NextOrder++
                };
                s_Sections[section] = sec;
            }

            for (int i = 0; i < sec.Entries.Count; i++)
            {
                if (string.Equals(sec.Entries[i].Label, label, StringComparison.Ordinal))
                {
                    var existing = sec.Entries[i];
                    existing.Kind = kind;
                    existing.Visible = visible;
                    existing.Available = available;
                    existing.Caption = label;
                    ClearKindFields(existing);
                    return existing;
                }
            }

            var entry = new DevConsoleEntry
            {
                Section = section,
                Label = label,
                Caption = label,
                Kind = kind,
                Visible = visible,
                Available = available,
                RegistrationOrder = s_NextOrder++
            };
            sec.Entries.Add(entry);
            return entry;
        }

        static void ClearKindFields(DevConsoleEntry e)
        {
            e.Action = null;
            e.ToggleGet = null;
            e.ToggleSet = null;
            e.DropdownOptions = null;
            e.DropdownGo = null;
            e.ReadoutValue = null;
            e.ReadoutRefreshSeconds = 0.5f;
        }

        public static bool SetButtonCaption(string section, string label, string caption)
        {
            var entry = Find(section, label);
            if (entry == null)
                return false;
            entry.Caption = caption ?? string.Empty;
            return true;
        }

        public static bool Unregister(string section, string label)
        {
            if (!s_Sections.TryGetValue(section ?? string.Empty, out var sec))
                return false;
            for (int i = 0; i < sec.Entries.Count; i++)
            {
                if (string.Equals(sec.Entries[i].Label, label, StringComparison.Ordinal))
                {
                    sec.Entries.RemoveAt(i);
                    if (sec.Entries.Count == 0)
                        s_Sections.Remove(sec.Name);
                    return true;
                }
            }

            return false;
        }

        public static void ClearSection(string section)
        {
            s_Sections.Remove(section ?? string.Empty);
        }

        public static DevConsoleEntry Find(string section, string label)
        {
            if (!s_Sections.TryGetValue(section ?? string.Empty, out var sec))
                return null;
            for (int i = 0; i < sec.Entries.Count; i++)
            {
                if (string.Equals(sec.Entries[i].Label, label, StringComparison.Ordinal))
                    return sec.Entries[i];
            }

            return null;
        }

        public static List<DevConsoleSection> GetOrderedSections()
        {
            var result = new List<DevConsoleSection>(s_Sections.Count);
            var used = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < s_SectionOrderOverride.Count; i++)
            {
                if (s_Sections.TryGetValue(s_SectionOrderOverride[i], out var sec))
                {
                    result.Add(sec);
                    used.Add(sec.Name);
                }
            }

            var rest = new List<DevConsoleSection>();
            foreach (var kv in s_Sections)
            {
                if (!used.Contains(kv.Key))
                    rest.Add(kv.Value);
            }

            rest.Sort((a, b) => a.FirstRegistrationOrder.CompareTo(b.FirstRegistrationOrder));
            result.AddRange(rest);
            return result;
        }

        public static int CountVisibleEntries(DevConsoleSection section)
        {
            if (section == null)
                return 0;
            int n = 0;
            for (int i = 0; i < section.Entries.Count; i++)
            {
                if (section.Entries[i].Visible)
                    n++;
            }

            return n;
        }

        public static bool IsAvailable(DevConsoleEntry entry)
        {
            if (entry?.Available == null)
                return true;
            try { return entry.Available(); }
            catch { return false; }
        }

        public static bool IsQuickAvailable(DevConsoleQuickEntry entry)
        {
            if (entry?.Available == null)
                return true;
            try { return entry.Available(); }
            catch { return false; }
        }
    }
}
#endif
