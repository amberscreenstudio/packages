using System;
using System.Collections.Generic;

namespace AmberScreen.Dev
{
    /// <summary>
    /// Handle returned by <see cref="DevHud.AddSection"/>. All members are no-ops when
    /// <c>AMBERSCREEN_DEVCONSOLE</c> is stripped.
    /// </summary>
    public sealed class DevHudSectionHandle
    {
#if AMBERSCREEN_DEVCONSOLE
        DevHudSection m_Section;

        internal DevHudSectionHandle(DevHudSection section)
        {
            m_Section = section;
        }

        public string Id => m_Section.Id;
        public string Title => m_Section.Title;
        public bool IsPinned => m_Section.Pinned;
        public bool IsCollapsed => m_Section.Collapsed;

        // Re-attaches the section if ClearSection removed it, so stale handles keep working.
        DevHudSection Resolve()
        {
            m_Section = DevHudRegistry.Resolve(m_Section);
            return m_Section;
        }
#else
        internal DevHudSectionHandle(string id, string title)
        {
            Id = id;
            Title = string.IsNullOrEmpty(title) ? id : title;
        }

        public string Id { get; }
        public string Title { get; }
        public bool IsPinned => false;
        public bool IsCollapsed => false;
#endif

        /// <summary>Adds one row. Cells share the width by weight; empty cells are hidden.</summary>
        public DevHudSectionHandle AddRow(params DevHudCell[] cells)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudRegistry.AddRow(Resolve(), cells);
            DevConsoleHost.NotifyHudStructureChanged();
#endif
            return this;
        }

        /// <summary>
        /// Adds a table: a header row of <paramref name="columns"/> and one data row per entry
        /// returned by <paramref name="rows"/> (polled at <c>hudUpdateHz</c>). Equal column widths.
        /// </summary>
        public DevHudSectionHandle AddTable(string[] columns, Func<IEnumerable<string[]>> rows)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudRegistry.AddTable(Resolve(), columns, rows);
            DevConsoleHost.NotifyHudStructureChanged();
#endif
            return this;
        }

        /// <summary>Pins the whole section into the always-visible strip at the top of the HUD.</summary>
        public DevHudSectionHandle SetPinned(bool pinned)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudRegistry.SetPinned(Resolve(), pinned);
#endif
            return this;
        }

        /// <summary>Collapses / expands the section from code (persisted when rememberCollapsed).</summary>
        public DevHudSectionHandle SetCollapsed(bool collapsed)
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudRegistry.SetCollapsed(Resolve(), collapsed);
#endif
            return this;
        }

        /// <summary>Removes all rows and tables, keeping the section options (tint, icon, collapse).</summary>
        public DevHudSectionHandle Clear()
        {
#if AMBERSCREEN_DEVCONSOLE
            DevHudRegistry.ClearContent(Resolve());
            DevConsoleHost.NotifyHudStructureChanged();
#endif
            return this;
        }
    }
}
