using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>No route finding or distance integration. The game supplies progress and position each tick.</summary>
    public sealed class PaceNoteFeed
    {
        private readonly List<PaceNote> notes = new List<PaceNote>();
        private bool[] consumed = Array.Empty<bool>();
        private bool coDriverPresent;
        public bool CoDriverPresent
        {
            get => coDriverPresent;
            set { coDriverPresent = value; if (!value) ClearCurrent(); }
        }
        public float DisplaySeconds { get; set; } = 4f;
        public PaceNote CurrentNote { get; private set; }
        public float RemainingSeconds { get; private set; }
        public event Action<PaceNote> NoteFired;

        public void SetNotes(IReadOnlyList<PaceNote> authoredNotes)
        {
            var copy = new List<PaceNote>();
            if (authoredNotes != null)
                for (int i = 0; i < authoredNotes.Count; i++)
                {
                    if (authoredNotes[i] == null) throw new ArgumentException("Notes must be non-null.", nameof(authoredNotes));
                    copy.Add(authoredNotes[i]);
                }
            notes.Clear(); notes.AddRange(copy);
            consumed = new bool[notes.Count];
            ClearCurrent();
        }

        // Keep calling while alone: crossed triggers are consumed silently, never replayed on Mags's return.
        // Progress is metres along an authored route, NOT straight-line distance to the next stop.
        public void Tick(float routeProgress, Vector3 position, float deltaSeconds)
        {
            if (!float.IsNaN(deltaSeconds) && !float.IsInfinity(deltaSeconds) && deltaSeconds > 0f)
            {
                RemainingSeconds = Math.Max(0f, RemainingSeconds - deltaSeconds);
                if (RemainingSeconds == 0f) ClearCurrent();
            }
            for (int i = 0; i < notes.Count; i++)
            {
                if (consumed[i]) continue;
                PaceNote note = notes[i];
                bool triggered = note.UsesWorldPosition ?
                    NavigationMath.HorizontalDistance(position.x, position.z, note.Position.x, note.Position.z) <= note.Radius :
                    !float.IsNaN(routeProgress) && !float.IsInfinity(routeProgress) && routeProgress >= note.RouteDistance;
                if (!triggered) continue;
                consumed[i] = true;
                if (!CoDriverPresent) continue;
                CurrentNote = note;
                RemainingSeconds = !float.IsNaN(DisplaySeconds) && !float.IsInfinity(DisplaySeconds) ?
                    Math.Max(0f, DisplaySeconds) : 4f;
                NoteFired?.Invoke(note);
                if (RemainingSeconds == 0f) ClearCurrent();
            }
        }
        private void ClearCurrent() { CurrentNote = null; RemainingSeconds = 0f; }
    }
}
