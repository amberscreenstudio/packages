#if AMBERSCREEN_DEVCONSOLE
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Dev
{
    /// <summary>
    /// One HUD row. Rows created by <c>SetLine</c> are "legacy" rows: <see cref="Label"/> is the
    /// lookup key and they hold exactly one cell. Rows created by AddRow have a null Label.
    /// </summary>
    sealed class DevHudRow
    {
        public string Label;
        public int Order;
        public int RegistrationIndex;
        public readonly List<DevHudCell> Cells = new List<DevHudCell>();

        public bool IsLegacy => Label != null;
    }

    sealed class DevHudTable
    {
        public string[] Columns;
        public Func<IEnumerable<string[]>> Rows;
        public int RegistrationIndex;
    }

    sealed class DevHudSection
    {
        public string Id;
        public string Title;
        public Color? Tint;
        public Sprite Icon;
        public bool Pinned;
        public bool Collapsed;
        public bool RememberCollapsed = true;

        /// <summary>True once created or upgraded through AddSection (survives RemoveLine emptying it).</summary>
        public bool Explicit;

        public int OrderIndex = int.MaxValue;
        public int RegistrationIndex;
        public readonly List<DevHudRow> Rows = new List<DevHudRow>();
        public readonly List<DevHudTable> Tables = new List<DevHudTable>();

        /// <summary>Legacy alias for <see cref="Id"/> (SetLine's section name is the id).</summary>
        public string Name => Id;

        /// <summary>Legacy alias for <see cref="Rows"/>.</summary>
        public List<DevHudRow> Lines => Rows;

        public int ItemCount => Rows.Count + Tables.Count;
    }

    static class DevHudRegistry
    {
        public const string CollapsedKeyPrefix = "AmberScreen.DevHud.Collapsed.";

        static readonly List<DevHudSection> s_Sections = new List<DevHudSection>();
        static readonly Dictionary<string, DevHudSection> s_ById =
            new Dictionary<string, DevHudSection>(StringComparer.Ordinal);
        static readonly List<string> s_SectionOrder = new List<string>();
        static int s_NextReg;
        static bool s_Dirty = true;

        public static bool IsDirty => s_Dirty;
        public static int SectionCount => s_Sections.Count;
        public static List<DevHudSection> Sections => s_Sections;

        public static string CollapsedKey(string id) => CollapsedKeyPrefix + id;

        public static void ClearDirty() => s_Dirty = false;
        public static void MarkDirty() => s_Dirty = true;

        public static void Reset()
        {
            s_Sections.Clear();
            s_ById.Clear();
            s_SectionOrder.Clear();
            s_NextReg = 0;
            s_Dirty = true;
        }

        /// <summary>Rows + tables across all sections (pinned cells live inside rows, so are included).</summary>
        public static int TotalLineCount()
        {
            int n = 0;
            for (int i = 0; i < s_Sections.Count; i++)
                n += s_Sections[i].ItemCount;
            return n;
        }

        /// <summary>Number of cells that render in the pinned strip (pinned cells + all cells of pinned sections).</summary>
        public static int PinnedCellCount()
        {
            int n = 0;
            for (int i = 0; i < s_Sections.Count; i++)
            {
                var sec = s_Sections[i];
                for (int r = 0; r < sec.Rows.Count; r++)
                {
                    var cells = sec.Rows[r].Cells;
                    for (int c = 0; c < cells.Count; c++)
                    {
                        if (sec.Pinned || cells[c].IsPinned)
                            n++;
                    }
                }
            }

            return n;
        }

        /// <summary>Anything to draw (rows, tables, legacy lines, pins) or an active key-hint list.</summary>
        public static bool HasHudContent() =>
            TotalLineCount() > 0 || PinnedCellCount() > 0 || DevHudKeyHints.TryGetActiveHints(out _);

        // ----- sections -------------------------------------------------------------------

        public static DevHudSection AddSection(
            string id,
            string title,
            Color? tint,
            Sprite icon,
            bool startCollapsed,
            bool rememberCollapsed)
        {
            if (string.IsNullOrEmpty(id))
            {
                // Detached section so the handle is still safe to use; it is never registered.
                return new DevHudSection { Id = string.Empty, Title = title ?? string.Empty };
            }

            bool existed = s_ById.TryGetValue(id, out var sec);
            if (!existed)
                sec = Insert(new DevHudSection { Id = id });

            bool firstExplicit = !sec.Explicit;
            sec.Explicit = true;
            sec.Title = string.IsNullOrEmpty(title) ? id : title;
            sec.Tint = tint;
            sec.Icon = icon;
            sec.RememberCollapsed = rememberCollapsed;

            if (firstExplicit)
            {
                bool collapsed = startCollapsed;
                if (rememberCollapsed)
                {
                    string key = CollapsedKey(id);
                    if (PlayerPrefs.HasKey(key))
                        collapsed = PlayerPrefs.GetInt(key, startCollapsed ? 1 : 0) != 0;
                }

                sec.Collapsed = collapsed;
            }

            s_Dirty = true;
            return sec;
        }

        /// <summary>Returns the live section for a (possibly stale) handle target, re-inserting it if needed.</summary>
        public static DevHudSection Resolve(DevHudSection section)
        {
            if (section == null || string.IsNullOrEmpty(section.Id))
                return section;
            if (s_ById.TryGetValue(section.Id, out var live))
                return live;
            Insert(section);
            s_Dirty = true;
            return section;
        }

        static DevHudSection Insert(DevHudSection sec)
        {
            sec.RegistrationIndex = s_NextReg++;
            sec.OrderIndex = int.MaxValue;
            for (int i = 0; i < s_SectionOrder.Count; i++)
            {
                if (string.Equals(s_SectionOrder[i], sec.Id, StringComparison.Ordinal))
                {
                    sec.OrderIndex = i;
                    break;
                }
            }

            if (string.IsNullOrEmpty(sec.Title))
                sec.Title = sec.Id;
            s_ById[sec.Id] = sec;
            s_Sections.Add(sec);
            s_Dirty = true;
            return sec;
        }

        public static void AddRow(DevHudSection sec, DevHudCell[] cells)
        {
            if (sec == null || cells == null || string.IsNullOrEmpty(sec.Id))
                return;

            var row = new DevHudRow { RegistrationIndex = s_NextReg++ };
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                    row.Cells.Add(cells[i]);
            }

            if (row.Cells.Count == 0)
                return;

            sec.Rows.Add(row);
            SortRows(sec);
            s_Dirty = true;
        }

        public static void AddTable(
            DevHudSection sec,
            string[] columns,
            Func<IEnumerable<string[]>> rows)
        {
            if (sec == null || string.IsNullOrEmpty(sec.Id) ||
                columns == null || columns.Length == 0 || rows == null)
                return;

            sec.Tables.Add(new DevHudTable
            {
                Columns = (string[])columns.Clone(),
                Rows = rows,
                RegistrationIndex = s_NextReg++
            });
            s_Dirty = true;
        }

        public static void ClearContent(DevHudSection sec)
        {
            if (sec == null)
                return;
            sec.Rows.Clear();
            sec.Tables.Clear();
            s_Dirty = true;
        }

        public static void SetPinned(DevHudSection sec, bool pinned)
        {
            if (sec == null || sec.Pinned == pinned)
                return;
            sec.Pinned = pinned;
            s_Dirty = true;
        }

        public static void SetCollapsed(DevHudSection sec, bool collapsed)
        {
            if (sec == null || sec.Collapsed == collapsed)
                return;
            sec.Collapsed = collapsed;
            s_Dirty = true;
            if (sec.RememberCollapsed && !string.IsNullOrEmpty(sec.Id))
            {
                PlayerPrefs.SetInt(CollapsedKey(sec.Id), collapsed ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void ToggleCollapsed(DevHudSection sec)
        {
            if (sec != null)
                SetCollapsed(sec, !sec.Collapsed);
        }

        // ----- legacy lines ---------------------------------------------------------------

        public static void SetLine(string section, string label, Func<string> value, int order)
        {
            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(label) || value == null)
                return;

            if (!s_ById.TryGetValue(section, out var sec))
                sec = Insert(new DevHudSection { Id = section, Title = section });

            for (int i = 0; i < sec.Rows.Count; i++)
            {
                var row = sec.Rows[i];
                if (!row.IsLegacy || !string.Equals(row.Label, label, StringComparison.Ordinal))
                    continue;

                row.Cells[0].Value = value;
                if (row.Order != order)
                {
                    row.Order = order;
                    SortRows(sec);
                    s_Dirty = true;
                }

                return;
            }

            var legacy = new DevHudRow
            {
                Label = label,
                Order = order,
                RegistrationIndex = s_NextReg++
            };
            legacy.Cells.Add(new DevHudCell(label, value));
            sec.Rows.Add(legacy);
            SortRows(sec);
            s_Dirty = true;
        }

        public static void RemoveLine(string section, string label)
        {
            if (string.IsNullOrEmpty(section) || !s_ById.TryGetValue(section, out var sec))
                return;

            for (int i = sec.Rows.Count - 1; i >= 0; i--)
            {
                var row = sec.Rows[i];
                if (row.IsLegacy && string.Equals(row.Label, label, StringComparison.Ordinal))
                {
                    sec.Rows.RemoveAt(i);
                    s_Dirty = true;
                    break;
                }
            }

            // Implicit (SetLine-created) sections vanish when empty; AddSection ones keep their options.
            if (!sec.Explicit && sec.ItemCount == 0)
            {
                s_Sections.Remove(sec);
                s_ById.Remove(section);
                s_Dirty = true;
            }
        }

        public static void ClearSection(string section)
        {
            if (string.IsNullOrEmpty(section) || !s_ById.TryGetValue(section, out var sec))
                return;
            s_Sections.Remove(sec);
            s_ById.Remove(section);
            s_Dirty = true;
        }

        // ----- ordering -------------------------------------------------------------------

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
                    if (string.Equals(s_SectionOrder[j], sec.Id, StringComparison.Ordinal))
                    {
                        sec.OrderIndex = j;
                        break;
                    }
                }
            }

            SortSections();
            s_Dirty = true;
        }

        public static void EnsureSorted()
        {
            SortSections();
            for (int i = 0; i < s_Sections.Count; i++)
                SortRows(s_Sections[i]);
        }

        static void SortSections()
        {
            s_Sections.Sort(static (a, b) =>
            {
                int c = a.OrderIndex.CompareTo(b.OrderIndex);
                return c != 0 ? c : a.RegistrationIndex.CompareTo(b.RegistrationIndex);
            });
        }

        static void SortRows(DevHudSection sec)
        {
            sec.Rows.Sort(static (a, b) =>
            {
                int c = a.Order.CompareTo(b.Order);
                return c != 0 ? c : a.RegistrationIndex.CompareTo(b.RegistrationIndex);
            });
        }
    }
}
#endif
