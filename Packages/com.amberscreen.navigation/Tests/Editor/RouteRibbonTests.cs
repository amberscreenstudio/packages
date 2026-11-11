using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Navigation.Tests
{
    public sealed class RouteRibbonTests
    {
        private GameObject host;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Ribbon test");
            NavigationHud.Visible = true;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            NavigationHud.Visible = true;
        }

        private static RoutePath Straight()
        {
            var path = new RoutePath();
            path.SetPoints(new[] { Vector3.zero, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f) });
            return path;
        }

        private RouteRibbonUI MakeUi(RoutePath path)
        {
            var go = new GameObject("UI Ribbon", typeof(RectTransform));
            go.transform.SetParent(host.transform, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(200f, 200f);
            var ui = go.AddComponent<RouteRibbonUI>();
            ui.WorldMin = new Vector2(-2f, -2f);
            ui.WorldMax = new Vector2(12f, 12f);
            ui.Width = 8f;
            ui.color = Color.white;
            ui.Path = path;
            return ui;
        }

        [Test]
        public void UiMeshHasTwoVerticesPerPoint()
        {
            var ui = MakeUi(Straight());
            var vh = new VertexHelper();
            ui.FillMesh(vh);
            // 3 points, shared join verts: 6 vertices, 2 segments * 2 triangles.
            Assert.That(vh.currentVertCount, Is.EqualTo(6));
            Assert.That(vh.currentIndexCount, Is.EqualTo(12));
        }

        [Test]
        public void RemainingOnlyDropsTravelledPortion()
        {
            var path = Straight();
            var ui = MakeUi(path);
            ui.ShowRemainingOnly = true;
            ui.ProgressDistance = path.Length;
            var vh = new VertexHelper();
            ui.FillMesh(vh);
            Assert.That(vh.currentVertCount, Is.EqualTo(0));

            ui.ProgressDistance = 15f; // mid second segment: start + last point
            vh = new VertexHelper();
            ui.FillMesh(vh);
            Assert.That(vh.currentVertCount, Is.EqualTo(4));
            Assert.That(vh.currentIndexCount, Is.EqualTo(6));
        }

        [Test]
        public void UiHidesWhenHudIsOffOrDisabled()
        {
            var ui = MakeUi(Straight());
            var vh = new VertexHelper();
            NavigationHud.Visible = false;
            ui.FillMesh(vh);
            Assert.That(vh.currentVertCount, Is.EqualTo(0));
            NavigationHud.Visible = true;
            ui.enabled = false;
            // EditMode does not call OnDisable on plain components; FillMesh reads isActiveAndEnabled.
            vh = new VertexHelper();
            ui.FillMesh(vh);
            Assert.That(vh.currentVertCount, Is.EqualTo(0));
            ui.enabled = true;
            vh = new VertexHelper();
            ui.FillMesh(vh);
            Assert.That(vh.currentVertCount, Is.EqualTo(6));
        }

        [Test]
        public void WorldLineWritesOffsetPointsAndRespectsHud()
        {
            var path = Straight();
            var world = host.AddComponent<RouteRibbonWorld>();
            world.Path = path;
            world.HeightOffset = 0.5f;
            world.ShowRemainingOnly = false;
            world.RenderNow();
            var line = world.Line;
            Assert.That(line, Is.Not.Null);
            Assert.That(line.enabled, Is.True);
            Assert.That(line.positionCount, Is.EqualTo(3));
            Assert.That(line.GetPosition(0).y, Is.EqualTo(0.5f).Within(1e-4f));

            world.ShowRemainingOnly = true;
            world.ProgressDistance = path.Length;
            world.RenderNow();
            Assert.That(line.positionCount, Is.EqualTo(0));
            Assert.That(line.enabled, Is.False);

            world.ShowRemainingOnly = false;
            world.RenderNow();
            NavigationHud.Visible = false;
            world.RenderNow();
            Assert.That(line.positionCount, Is.EqualTo(0));
            Assert.That(line.enabled, Is.False);

            NavigationHud.Visible = true;
            world.enabled = false;
            world.RenderNow();
            Assert.That(line.positionCount, Is.EqualTo(0));
        }

        [Test]
        public void EmptyPathProducesNoMeshOrLine()
        {
            var empty = new RoutePath();
            var ui = MakeUi(empty);
            var vh = new VertexHelper();
            ui.FillMesh(vh);
            Assert.That(vh.currentVertCount, Is.EqualTo(0));

            var world = host.AddComponent<RouteRibbonWorld>();
            world.Path = empty;
            world.RenderNow();
            Assert.That(world.Line.positionCount, Is.EqualTo(0));
            Assert.That(world.Line.enabled, Is.False);
        }
    }
}
