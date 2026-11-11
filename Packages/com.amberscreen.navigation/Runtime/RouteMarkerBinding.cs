using System;

namespace AmberScreen.Navigation
{
    /// <summary>Explicit opt-in, next-stop-only registration. Dispose when the game's binding ends.</summary>
    public sealed class RouteMarkerBinding : IDisposable
    {
        private readonly NavigationRoute route;
        private readonly MarkerRegistry registry;
        private readonly string markerId;
        private NavigationMarker owned;
        public RouteMarkerBinding(NavigationRoute route, MarkerRegistry registry, string markerId)
        {
            this.route = route ?? throw new ArgumentNullException(nameof(route));
            this.registry = registry != null ? registry : throw new ArgumentNullException(nameof(registry));
            if (string.IsNullOrWhiteSpace(markerId)) throw new ArgumentException("Marker id is required.", nameof(markerId));
            this.markerId = markerId;
            route.NextStopChanged += Refresh;
            Refresh(route.NextStop);
        }
        private void Refresh(RouteStop stop)
        {
            RemoveOwned();
            if (registry == null || stop == null) return;
            var marker = new NavigationMarker(markerId, stop.Position, "next-stop", stop.Label, true);
            if (registry.Register(marker)) owned = marker; // Never replace a game's existing registration.
        }
        private void RemoveOwned()
        {
            if (registry != null && owned != null && registry.TryGet(markerId, out NavigationMarker current) &&
                ReferenceEquals(current, owned)) registry.Unregister(markerId);
            owned = null;
        }
        public void Dispose()
        {
            route.NextStopChanged -= Refresh;
            RemoveOwned();
        }
    }
}
