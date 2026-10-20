#if AMBERSCREEN_DEVCONSOLE
using System.Collections.Generic;

namespace AmberScreen.Dev
{
    sealed class DevHudLine
    {
        public string Section;
        public string Label;
        public System.Func<string> Value;
        public int Order;
        public int RegistrationIndex;
    }

    sealed class DevHudSection
    {
        public string Name;
        public int OrderIndex = int.MaxValue;
        public int RegistrationIndex;
        public readonly List<DevHudLine> Lines = new List<DevHudLine>();
    }

    static class DevHudRegistry
    {
        static readonly List<DevHudSection> s_Sections = new List<DevHudSection>();
        static readonly Dictionary<string, DevHudSection> s_ByName =
            new Dictionary<string, DevHudSection>(System.StringComparer.Ordinal);
        static readonly List<string> s_SectionOrder = new List<string>();
        static int s_NextReg;
        static bool s_Dirty = true;

        public static bool IsDirty => s_Dirty;
        public static int SectionCount => s_Sections.Count;

        public static void ClearDirty() => s_Dirty = false;

        public static void Reset()
        {
            s_Sections.Clear();
            s_ByName.Clear();
            s_SectionOrder.Clear();
            s_NextReg = 0;
            s_Dirty = true;
        }

        public static int TotalLineCount()
        {
            int n = 0;
            for (int i = 0; i < s_Sections.Count; i++)
                n += s_Sections[i].Lines.Count;
            return n;
        }

        public static void SetLine(string section, string label, System.Func<string> value, int order)
        {
            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(label) || value == null)
                return;

            if (!s_ByName.TryGetValue(section, out var sec))
            {
                sec = new DevHudSection
                {
                    Name = section,
                    RegistrationIndex = s_NextReg++
                };
                for (int i = 0; i < s_SectionOrder.Count; i++)
                {
                    if (string.Equals(s_SectionOrder[i], section, System.StringComparison.Ordinal))
                    {
                        sec.OrderIndex = i;
                        break;
                    }
                }

                s_ByName[section] = sec;
                s_Sections.Add(sec);
                s_Dirty = true;
            }

            for (int i = 0; i < sec.Lines.Count; i++)
            {
                if (string.Equals(sec.Lines[i].Label, label, System.StringComparison.Ordinal))
                {
                    sec.Lines[i].Value = value;
                    if (sec.Lines[i].Order != order)
                    {
                        sec.Lines[i].Order = order;
                        SortLines(sec);
                        s_Dirty = true;
                    }

                    return;
                }
            }

            sec.Lines.Add(new DevHudLine
            {
                Section = section,
                Label = label,
                Value = value,
                Order = order,
                RegistrationIndex = s_NextReg++
            });
            SortLines(sec);
            s_Dirty = true;
        }

        public static void RemoveLine(string section, string label)
        {
            if (!s_ByName.TryGetValue(section, out var sec))
                return;
            for (int i = sec.Lines.Count - 1; i >= 0; i--)
            {
                if (string.Equals(sec.Lines[i].Label, label, System.StringComparison.Ordinal))
                {
                    sec.Lines.RemoveAt(i);
                    s_Dirty = true;
                    break;
                }
            }

            if (sec.Lines.Count == 0)
            {
                s_Sections.Remove(sec);
                s_ByName.Remove(section);
                s_Dirty = true;
            }
        }

        public static void ClearSection(string section)
        {
            if (!s_ByName.TryGetValue(section, out var sec))
                return;
            s_Sections.Remove(sec);
            s_ByName.Remove(section);
            s_Dirty = true;
        }

        public static void SetSectionOrder(string[] sections)
        {
            s_SectionOrder.Clear();
            if (sections != null)
            {
                for (int i = 0; i < sections.Length; i++)
                {
                    if (!string.IsNullOrEmpty(sections[i]))
                        s_SectionOrder.Add(sections[i]);
                }
            }

            for (int i = 0; i < s_Sections.Count; i++)
            {
                var sec = s_Sections[i];
                sec.OrderIndex = int.MaxValue;
                for (int j = 0; j < s_SectionOrder.Count; j++)
                {
                    if (string.Equals(s_SectionOrder[j], sec.Name, System.StringComparison.Ordinal))
                    {
                        sec.OrderIndex = j;
                        break;
                    }
                }
            }

            SortSections();
            s_Dirty = true;
        }

        public static List<DevHudSection> Sections => s_Sections;

        public static void EnsureSorted()
        {
            SortSections();
            for (int i = 0; i < s_Sections.Count; i++)
                SortLines(s_Sections[i]);
        }

        static void SortSections()
        {
            s_Sections.Sort(static (a, b) =>
            {
                int c = a.OrderIndex.CompareTo(b.OrderIndex);
                return c != 0 ? c : a.RegistrationIndex.CompareTo(b.RegistrationIndex);
            });
        }

        static void SortLines(DevHudSection sec)
        {
            sec.Lines.Sort(static (a, b) =>
            {
                int c = a.Order.CompareTo(b.Order);
                return c != 0 ? c : a.RegistrationIndex.CompareTo(b.RegistrationIndex);
            });
        }
    }
}
#endif
