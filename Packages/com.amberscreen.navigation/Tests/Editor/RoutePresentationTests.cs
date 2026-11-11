using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Navigation.Tests
{
    public sealed class RoutePresentationTests
    {
        private GameObject host;
        [SetUp] public void SetUp() { host = new GameObject("Route test"); NavigationHud.Visible = true; }
        [TearDown] public void TearDown() { Object.DestroyImmediate(host); NavigationHud.Visible = true; }
        private static NavigationRoute Route()
        {
            var route = new NavigationRoute();
            route.SetStops(new[] { new RouteStop("a", "First", Vector3.zero), new RouteStop("b", "Second", Vector3.forward) });
            return route;
        }
        [Test] public void MarkersAreOptInFollowNextAndDisposeCleanly()
        {
            var registry = host.AddComponent<MarkerRegistry>(); var route = Route();
            Assert.That(registry.Count, Is.Zero);
            using (var binding = new RouteMarkerBinding(route, registry, "route/next"))
            {
                Assert.That(registry.Count, Is.EqualTo(1)); route.Advance();
                Assert.That(registry.TryGet("route/next", out var marker), Is.True);
                Assert.That(marker.Label, Is.EqualTo("Second"));
            }
            Assert.That(registry.Count, Is.Zero); route.SetStops(null); Assert.That(registry.Count, Is.Zero);
        }
        [Test] public void BindingNeverReplacesOrRemovesSomeoneElsesMarker()
        {
            var registry = host.AddComponent<MarkerRegistry>(); var route = Route();
            var existing = new NavigationMarker("same", Vector3.zero);
            registry.Register(existing);
            using (var binding = new RouteMarkerBinding(route, registry, "same")) route.Advance();
            Assert.That(registry.TryGet("same", out var actual), Is.True); Assert.That(actual, Is.SameAs(existing));
        }
        [Test] public void BindingDoesNotRemoveExternallyReplacedEntry()
        {
            var registry = host.AddComponent<MarkerRegistry>(); var route = Route();
            var binding = new RouteMarkerBinding(route, registry, "next");
            var replacement = new NavigationMarker("next", Vector3.forward);
            registry.UpdateEntry(replacement); binding.Dispose();
            Assert.That(registry.TryGet("next", out var actual), Is.True); Assert.That(actual, Is.SameAs(replacement));
        }
        [Test] public void CompletionRemovesOnlyTheRouteMarker()
        {
            var registry = host.AddComponent<MarkerRegistry>(); var route = Route();
            registry.Register(new NavigationMarker("other", Vector3.zero));
            using (var binding = new RouteMarkerBinding(route, registry, "next"))
            { route.Advance(); route.Advance(); Assert.That(registry.Count, Is.EqualTo(1)); }
        }
        [Test] public void PresenterUsesOneHudSwitchAndCoDriverGate()
        {
            var textGo = new GameObject("Label", typeof(RectTransform)); textGo.transform.SetParent(host.transform, false);
            var text = textGo.AddComponent<Text>(); var presenter = textGo.AddComponent<PaceNotePresenter>();
            var feed = new PaceNoteFeed { CoDriverPresent = true };
            presenter.label = text; presenter.Feed = feed;
            feed.SetNotes(new[] { new PaceNote("Right 3", 0f) }); feed.Tick(0, Vector3.zero, 0);
            presenter.RenderNow(); Assert.That(text.text, Is.EqualTo("Right 3"));
            NavigationHud.Visible = false; presenter.RenderNow(); Assert.That(text.enabled, Is.False);
            Assert.That(feed.CurrentNote, Is.Not.Null); // UI visibility must not alter navigation data.
            NavigationHud.Visible = true; presenter.RenderNow(); Assert.That(text.enabled, Is.True);
            feed.CoDriverPresent = false; presenter.RenderNow(); Assert.That(text.text, Is.Empty);
            Assert.That(text.enabled, Is.False);
        }
        [Test] public void PresenterClearsOnDisableAndExpiry()
        {
            var textGo = new GameObject("Label", typeof(RectTransform)); textGo.transform.SetParent(host.transform, false);
            var text = textGo.AddComponent<Text>(); var presenter = textGo.AddComponent<PaceNotePresenter>();
            var feed = new PaceNoteFeed { CoDriverPresent = true }; presenter.label = text; presenter.Feed = feed;
            feed.SetNotes(new[] { new PaceNote("Crest", 0f) }); feed.Tick(0, Vector3.zero, 0);
            presenter.RenderNow(); presenter.enabled = false;
            // EditMode does not call OnDisable on plain components; LateUpdate/RenderNow covers it in Play.
            presenter.RenderNow(); Assert.That(text.text, Is.Empty);
            presenter.enabled = true; feed.Tick(0, Vector3.zero, 4); presenter.RenderNow(); Assert.That(text.enabled, Is.False);
        }
    }
}
