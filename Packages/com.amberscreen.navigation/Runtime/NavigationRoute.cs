using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>
    /// Thin adaptation of LWN JobManifest's ordered snapshot/current-index/stop-change pattern.
    /// The game remains authoritative: proximity reports Reached but NEVER advances a delivery.
    /// </summary>
    public sealed class NavigationRoute
    {
        private readonly List<RouteStop> stops = new List<RouteStop>();
        private readonly IReadOnlyList<RouteStop> view;
        private int currentIndex = -1;
        private bool reached;
        private Vector3 observerPosition;
        public NavigationRoute() { view = stops.AsReadOnly(); }
        public IReadOnlyList<RouteStop> Stops => view;
        public RouteStop NextStop => currentIndex >= 0 && currentIndex < stops.Count ? stops[currentIndex] : null;
        public int CurrentIndex => currentIndex;
        public float DistanceToNextStop => NextStop == null ? float.PositiveInfinity :
            NavigationMath.HorizontalDistance(observerPosition.x, observerPosition.z, NextStop.Position.x, NextStop.Position.z);
        public event Action<RouteStop> NextStopChanged; // null means no remaining stop
        public event Action<RouteStop> Reached;
        public event Action<RouteStop> Advanced; // the stop the game advanced past

        public void SetStops(IReadOnlyList<RouteStop> orderedStops)
        {
            var copy = new List<RouteStop>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (orderedStops != null)
                for (int i = 0; i < orderedStops.Count; i++)
                {
                    RouteStop stop = orderedStops[i];
                    if (stop == null || string.IsNullOrWhiteSpace(stop.Id) || !ids.Add(stop.Id))
                        throw new ArgumentException("Stops must be non-null with unique non-empty ids.", nameof(orderedStops));
                    copy.Add(stop);
                }
            stops.Clear(); stops.AddRange(copy);
            currentIndex = stops.Count == 0 ? -1 : 0;
            reached = false;
            NextStopChanged?.Invoke(NextStop);
        }

        public void UpdatePosition(Vector3 position, float reachedRadius = 3f)
        {
            if (float.IsNaN(position.x) || float.IsInfinity(position.x) ||
                float.IsNaN(position.z) || float.IsInfinity(position.z)) return;
            observerPosition = position;
            if (NextStop == null || reached || float.IsNaN(reachedRadius) || float.IsInfinity(reachedRadius) || reachedRadius < 0f) return;
            if (DistanceToNextStop > reachedRadius) return;
            reached = true;
            Reached?.Invoke(NextStop);
        }

        public bool Advance()
        {
            RouteStop previous = NextStop;
            if (previous == null) return false;
            currentIndex++;
            reached = false;
            Advanced?.Invoke(previous);
            NextStopChanged?.Invoke(NextStop);
            return true;
        }
    }
}
