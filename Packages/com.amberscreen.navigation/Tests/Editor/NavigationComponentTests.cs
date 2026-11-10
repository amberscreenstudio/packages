using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Navigation.Tests
{
    public sealed class TestBearingSource : CustomBearingSource
    {
        public float bearing;
        public override bool TryGetBearing(out float value) { value = bearing; return true; }
    }

    public sealed class NavigationComponentTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private MarkerTypes types;
        private HeadingSource heading;
        private MarkerRegistry registry;
        private CompassStripPresenter presenter;
        private Transform body, cameraLook;

        private GameObject NewObject(string name, params System.Type[] components)
        {
            var go = new GameObject(name, components); objects.Add(go); return go;
        }
        [SetUp] public void SetUp()
        {
            NavigationHud.Visible = true;
            body = NewObject("body").transform;
            cameraLook = NewObject("camera look").transform;
            cameraLook.rotation = Quaternion.Euler(0, 90, 0);
            heading = NewObject("heading").AddComponent<HeadingSource>();
            heading.ReferenceTransform = body;
            registry = NewObject("registry").AddComponent<MarkerRegistry>();
            var canvas = NewObject("canvas", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var strip = NewObject("strip", typeof(RectTransform));
            strip.transform.SetParent(canvas.transform, false);
            ((RectTransform)strip.transform).sizeDelta = new Vector2(640, 110);
            presenter = strip.AddComponent<CompassStripPresenter>();
            presenter.headingSource = heading; presenter.registry = registry;
        }
        [TearDown] public void TearDown()
        {
            // No static hosts and no DontDestroyOnLoad. Always restore the sole global value.
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            if (types != null) Object.DestroyImmediate(types);
            NavigationHud.Visible = true;
        }
        private void AddMarker(bool? distance = null) =>
            registry.Register(new NavigationMarker("well", new Vector3(0, 0, 10), "well", "Well", distance));

        [Test] public void ConfiguredReferenceIsNotImplicitCamera()
        {
            Assert.That(heading.RawHeading, Is.EqualTo(0f).Within(0.001f));
            heading.ReferenceTransform = cameraLook;
            Assert.That(heading.RawHeading, Is.EqualTo(90f).Within(0.001f));
        }
        [Test] public void UnassignedReferenceUsesOwnTransform()
        {
            heading.ReferenceTransform = null; heading.transform.rotation = Quaternion.Euler(0, 180, 0);
            Assert.That(heading.RawHeading, Is.EqualTo(180f).Within(0.001f));
        }
        [TestCase(100f)] [TestCase(-100f)] [TestCase(0f)]
        public void VerticalOrCoincidentTargetHasDefinedBearing(float y)
        {
            body.rotation = Quaternion.Euler(0, 37, 0);
            Assert.That(heading.BearingTo(new Vector3(0, y, 0)), Is.EqualTo(37f).Within(0.001f));
            registry.Register(new NavigationMarker("vertical", new Vector3(0, y, 0), label: "Vertical"));
            presenter.RenderNow();
            Assert.That(presenter.VisibleItems[0].Text, Is.EqualTo("Vertical\n0 m"));
        }
        [Test] public void DiscoveryWithoutRegistrationHasNoAuthority()
        {
            var discovered = new HashSet<string> { "well" }; // Game data, intentionally never passed to package.
            Assert.That(discovered.Contains("well"), Is.True);
            presenter.RenderNow(); Assert.That(presenter.VisibleItems.Count, Is.Zero);
            AddMarker(); presenter.RenderNow(); Assert.That(presenter.VisibleItems.Count, Is.EqualTo(1));
            registry.Unregister("well"); presenter.RenderNow(); Assert.That(presenter.VisibleItems.Count, Is.Zero);
            foreach (Text text in presenter.GetComponentsInChildren<Text>())
                Assert.That(text.text, Does.Not.Contain("Well")); // No active marker widget remains.
        }
        [Test] public void PerMarkerDistanceCanBeHidden()
        {
            AddMarker(false); presenter.RenderNow();
            Assert.That(presenter.VisibleItems[0].Text, Is.EqualTo("Well"));
        }
        [Test] public void GlobalDistanceCanBeHidden()
        {
            AddMarker(true); presenter.showDistances = false; presenter.RenderNow();
            Assert.That(presenter.VisibleItems[0].Text, Is.EqualTo("Well"));
        }
        [Test] public void TypeDefaultCanBeOverriddenPerMarker()
        {
            types = ScriptableObject.CreateInstance<MarkerTypes>();
            types.types.Add(new MarkerType { typeId = "well", defaultShowDistance = false });
            presenter.markerTypes = types;
            AddMarker(); presenter.RenderNow();
            Assert.That(presenter.VisibleItems[0].Text, Is.EqualTo("Well"));
            registry.UpdateEntry(new NavigationMarker("well", new Vector3(0, 0, 10), "well", "Well", true));
            presenter.RenderNow();
            Assert.That(presenter.VisibleItems[0].Text, Is.EqualTo("Well\n10 m"));
        }
        [Test] public void HudOffHidesUiButLeavesQueriesAndRegistryAlive()
        {
            AddMarker(); presenter.RenderNow();
            Assert.That(presenter.IsHudVisible, Is.True);
            NavigationHud.Visible = false; presenter.RenderNow();
            Assert.That(presenter.IsHudVisible, Is.False);
            Assert.That(presenter.VisibleItems.Count, Is.Zero);
            Assert.That(heading.RawHeading, Is.EqualTo(0f));
            Assert.That(heading.BearingTo(Vector3.right), Is.EqualTo(90f));
            Assert.That(registry.Count, Is.EqualTo(1));
            NavigationHud.Visible = true; presenter.RenderNow();
            Assert.That(presenter.VisibleItems.Count, Is.EqualTo(1));
        }
        [Test] public void CustomBearingIsOptInAndNeverHasDistance()
        {
            var source = NewObject("sun").AddComponent<TestBearingSource>(); source.label = "Sun";
            presenter.RenderNow(); Assert.That(presenter.VisibleItems.Count, Is.Zero);
            presenter.customSources.Add(source); presenter.RenderNow();
            Assert.That(presenter.VisibleItems.Count, Is.EqualTo(1));
            Assert.That(presenter.VisibleItems[0].Text, Is.EqualTo("Sun"));
            source.enabled = false; presenter.RenderNow(); Assert.That(presenter.VisibleItems.Count, Is.Zero);
        }
        private sealed class FixedDistance : INavigationDistanceProvider
        {
            public float value;
            public float GetDistance(Vector3 origin, NavigationMarker marker) => value;
        }
        [Test] public void OptionalDistanceProviderReplacesHorizontalDistance()
        {
            AddMarker(); presenter.DistanceProvider = new FixedDistance { value = 42 };
            presenter.RenderNow(); Assert.That(presenter.VisibleItems[0].Text, Is.EqualTo("Well\n42 m"));
        }
        [Test] public void InvalidDistanceIsHidden()
        {
            AddMarker(); presenter.DistanceProvider = new FixedDistance { value = float.NaN };
            presenter.RenderNow(); Assert.That(presenter.VisibleItems[0].Text, Is.EqualTo("Well"));
        }
        [Test] public void DuplicateRegistrationDoesNotOverwriteAndUpdateIsExplicit()
        {
            AddMarker(); Assert.That(registry.Register(new NavigationMarker("well", Vector3.back)), Is.False);
            registry.UpdateEntry(new NavigationMarker("well", Vector3.back)); presenter.RenderNow();
            Assert.That(presenter.VisibleItems.Count, Is.Zero); Assert.That(registry.Count, Is.EqualTo(1));
        }
        [Test] public void TransformMarkersFollowLivePositionAndDestroyedTargetsAreSkipped()
        {
            Transform target = NewObject("target").transform; target.position = Vector3.forward * 10;
            registry.Register(new NavigationMarker("moving", target)); presenter.RenderNow();
            Assert.That(presenter.VisibleItems.Count, Is.EqualTo(1));
            target.position = Vector3.back; presenter.RenderNow();
            Assert.That(presenter.VisibleItems.Count, Is.Zero);
            Object.DestroyImmediate(target.gameObject); presenter.RenderNow();
            Assert.That(presenter.VisibleItems.Count, Is.Zero);
        }
        [Test] public void RawPresenterCanBypassTheSharedInstrumentSpring()
        {
            body.rotation = Quaternion.Euler(0, 70, 0); heading.SetHeading(20f);
            presenter.useRawHeading = true; presenter.RenderNow();
            Assert.That(presenter.DisplayedHeading, Is.EqualTo(70f).Within(0.001f));
            presenter.useRawHeading = false; presenter.RenderNow();
            Assert.That(presenter.DisplayedHeading, Is.EqualTo(20f).Within(0.001f));
        }
        [Test] public void ConfigurableNorthIsSharedByHeadingAndMarkerBearing()
        {
            heading.NorthAxis = Vector3.right; body.rotation = Quaternion.Euler(0, 90, 0);
            Assert.That(heading.RawHeading, Is.EqualTo(0f).Within(0.001f));
            Assert.That(heading.BearingTo(Vector3.back), Is.EqualTo(90f).Within(0.001f));
        }
    }
}
