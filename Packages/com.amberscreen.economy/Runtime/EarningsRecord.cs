using System.Collections.Generic;

namespace AmberScreen.Economy
{
    /// <summary>Generic earnings breakdown (minor units). Games fill this; no game rules here.</summary>
    public sealed class EarningsRecord
    {
        public sealed class Line
        {
            public string Label;
            public long AmountMinor;
            public EarningsLineType Type;
        }

        public readonly List<Line> Lines = new List<Line>();
        public long TotalMinor;

        public void AddLine(string label, long amountMinor, EarningsLineType type)
        {
            Lines.Add(new Line
            {
                Label = label ?? "",
                AmountMinor = amountMinor,
                Type = type
            });
            TotalMinor += amountMinor;
        }
    }
}
