using System;

namespace AmberScreen.Dev
{
    /// <summary>
    /// One label/value pair on a Debug HUD row. Cells in a row share the row width by
    /// <see cref="Weight"/>. Cells whose <see cref="Value"/> returns null or empty are hidden.
    /// Compiles (and does nothing useful) when <c>AMBERSCREEN_DEVCONSOLE</c> is stripped.
    /// </summary>
    public class DevHudCell
    {
        float m_Weight = 1f;

        /// <summary>Text drawn before the value (a trailing colon is added when drawn). Empty = value only.</summary>
        public string Label { get; set; }

        /// <summary>Polled at <c>hudUpdateHz</c>. Null / empty result hides the cell.</summary>
        public Func<string> Value { get; set; }

        /// <summary>Relative width inside the row (maps to LayoutElement.flexibleWidth). Default 1.</summary>
        public float Weight
        {
            get => m_Weight;
            set => m_Weight = value;
        }

        /// <summary>True once <see cref="Pin"/> was called (and not undone by <see cref="Unpin"/>).</summary>
        public bool IsPinned { get; private set; }

        public DevHudCell()
        {
        }

        public DevHudCell(string label, Func<string> value, float weight = 1f)
        {
            Label = label;
            Value = value;
            m_Weight = weight;
        }

        public static DevHudCell Create(string label, Func<string> value, float weight = 1f) =>
            new DevHudCell(label, value, weight);

        /// <summary>
        /// Moves this cell into the always-visible pinned strip at the top of the HUD.
        /// Returns this cell for chaining.
        /// </summary>
        public DevHudCell Pin()
        {
            if (!IsPinned)
            {
                IsPinned = true;
#if AMBERSCREEN_DEVCONSOLE
                DevHudRegistry.MarkDirty();
#endif
            }

            return this;
        }

        /// <summary>Returns this cell to its normal row inside its section.</summary>
        public DevHudCell Unpin()
        {
            if (IsPinned)
            {
                IsPinned = false;
#if AMBERSCREEN_DEVCONSOLE
                DevHudRegistry.MarkDirty();
#endif
            }

            return this;
        }
    }
}
