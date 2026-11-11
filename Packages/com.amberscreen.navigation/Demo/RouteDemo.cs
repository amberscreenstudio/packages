using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Navigation.Demo
{
    /// <summary>Inspector toggles need no input backend or player controller. Demo data only.</summary>
    public sealed class RouteDemo : MonoBehaviour
    {
        public MarkerRegistry registry;
        public Text nextStopLabel;
        public PaceNotePresenter paceNotePresenter;
        public bool coDriverPresent = true;
        public bool hudVisible = true;
        private NavigationRoute route;
        private PaceNoteFeed feed;
        private RouteMarkerBinding markers;
        private float progress;
        private bool previousHud;

        private void OnEnable()
        {
            previousHud = NavigationHud.Visible;
            route = new NavigationRoute();
            feed = new PaceNoteFeed();
            if (paceNotePresenter != null) paceNotePresenter.Feed = feed;
            if (registry != null) markers = new RouteMarkerBinding(route, registry, "route-demo/next");
            Restart();
        }
        private void Restart()
        {
            progress = 0f;
            route.SetStops(new[] {
                new RouteStop("harbour", "Harbour Road", new Vector3(0f, 0f, 30f)),
                new RouteStop("gate", "Old Gate", new Vector3(0f, 0f, 70f)),
                new RouteStop("depot", "Depot", new Vector3(0f, 0f, 110f)) });
            feed.SetNotes(new[] {
                new PaceNote("Right 3 into Harbour Road", 8f),
                new PaceNote("Long left, gate on the left", 44f),
                new PaceNote("Caution crest, depot ahead", new Vector3(0f, 0f, 90f), 8f) });
        }
        private void Update()
        {
            progress += Time.deltaTime * 8f;
            if (progress > 140f) Restart();
            var position = new Vector3(0f, 0f, progress);
            NavigationHud.Visible = hudVisible;
            feed.CoDriverPresent = coDriverPresent;
            feed.Tick(progress, position, Time.deltaTime);
            route.UpdatePosition(position, 1f);
            // Demo alone advances automatically; real games call Advance only from their own stop lifecycle.
            if (route.NextStop != null && progress >= route.NextStop.Position.z) route.Advance();
            if (nextStopLabel != null)
            {
                nextStopLabel.enabled = NavigationHud.Visible;
                nextStopLabel.text = route.NextStop == null ? "Route complete — restarting shortly" :
                    "Next stop: " + route.NextStop.Label + " — " + route.DistanceToNextStop.ToString("0") + " m";
            }
        }
        private void OnDisable()
        {
            markers?.Dispose(); markers = null;
            if (paceNotePresenter != null) { paceNotePresenter.Feed = null; paceNotePresenter.RenderNow(); }
            if (nextStopLabel != null) nextStopLabel.text = "";
            NavigationHud.Visible = previousHud;
        }
    }
}
