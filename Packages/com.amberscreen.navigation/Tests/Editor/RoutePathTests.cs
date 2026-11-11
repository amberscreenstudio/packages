using NUnit.Framework;
using UnityEngine;

namespace AmberScreen.Navigation.Tests
{
    public sealed class RoutePathTests
    {
        private static RoutePath Path(params Vector3[] pts)
        {
            var path = new RoutePath();
            path.SetPoints(pts);
            return path;
        }

        [Test]
        public void LengthAndCumulativeMatchPolyline()
        {
            var path = Path(Vector3.zero, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            Assert.That(path.Length, Is.EqualTo(20f).Within(1e-4f));
            Assert.That(path.Points.Count, Is.EqualTo(3));
            Assert.That(path.CumulativeLengths[0], Is.EqualTo(0f));
            Assert.That(path.CumulativeLengths[1], Is.EqualTo(10f).Within(1e-4f));
            Assert.That(path.CumulativeLengths[2], Is.EqualTo(20f).Within(1e-4f));
        }

        [Test]
        public void SampleAtDistanceReturnsPositionAndDirection()
        {
            var path = Path(Vector3.zero, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            Assert.That(path.SampleAtDistance(5f, out var mid, out var dir), Is.True);
            Assert.That(mid.x, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(dir.x, Is.EqualTo(1f).Within(1e-4f));
            path.SampleAtDistance(15f, out var mid2, out var dir2);
            Assert.That(mid2.z, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(dir2.z, Is.EqualTo(1f).Within(1e-4f));
            path.SampleAtDistance(-3f, out var start, out _);
            Assert.That(start, Is.EqualTo(Vector3.zero));
            path.SampleAtDistance(99f, out var end, out var endDir);
            Assert.That(end, Is.EqualTo(new Vector3(10f, 0f, 10f)));
            Assert.That(endDir.z, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void ClosestDistanceProjectsOntoSegments()
        {
            var path = Path(Vector3.zero, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f));
            Assert.That(path.ClosestDistance(new Vector3(5f, 2f, 1f)), Is.EqualTo(5f).Within(1e-3f));
            Assert.That(path.ClosestDistance(new Vector3(12f, 0f, 5f)), Is.EqualTo(15f).Within(1e-3f));
            Assert.That(path.ClosestDistance(new Vector3(-4f, 0f, 0f)), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void EmptySingleAndDegeneratePathsAreSafe()
        {
            var empty = new RoutePath();
            Assert.That(empty.Length, Is.EqualTo(0f));
            Assert.That(empty.SampleAtDistance(1f, out var p, out var d), Is.False);
            Assert.That(p, Is.EqualTo(Vector3.zero));
            Assert.That(d, Is.EqualTo(Vector3.forward));
            Assert.That(empty.ClosestDistance(Vector3.one), Is.EqualTo(0f));

            var one = Path(new Vector3(3f, 1f, 2f));
            Assert.That(one.Length, Is.EqualTo(0f));
            Assert.That(one.SampleAtDistance(4f, out var p1, out _), Is.True);
            Assert.That(p1, Is.EqualTo(new Vector3(3f, 1f, 2f)));

            var dup = Path(Vector3.zero, Vector3.zero, new Vector3(4f, 0f, 0f), new Vector3(4f, 0f, 0f));
            Assert.That(dup.Points.Count, Is.EqualTo(2));
            Assert.That(dup.Length, Is.EqualTo(4f).Within(1e-4f));
        }

        [Test]
        public void NonFinitePointsAreDroppedAndEventFires()
        {
            var path = new RoutePath();
            int count = 0;
            path.Changed += () => count++;
            path.SetPoints(new[] { Vector3.zero, new Vector3(float.NaN, 0f, 0f), new Vector3(2f, 0f, 0f) });
            Assert.That(path.Points.Count, Is.EqualTo(2));
            Assert.That(path.Length, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(count, Is.EqualTo(1));
            path.SetPoints(null);
            Assert.That(path.Points.Count, Is.EqualTo(0));
            Assert.That(count, Is.EqualTo(2));
        }
    }
}
