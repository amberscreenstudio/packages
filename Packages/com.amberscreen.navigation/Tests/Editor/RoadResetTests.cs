using NUnit.Framework;
using UnityEngine;

namespace AmberScreen.Navigation.Tests
{
    public sealed class RoadResetTests
    {
        private static RoadSourceList Roads(params (Vector3[] pts, bool twoWay)[] roads)
        {
            var list = new RoadSourceList();
            foreach (var r in roads) list.Add(r.pts, r.twoWay);
            return list;
        }

        private static RoadResetSettings Settings(float lift = 0f, float lane = 0f, float radius = 30f) =>
            new RoadResetSettings { LiftHeight = lift, LaneOffset = lane, LastOnRoadRadius = radius };

        private static void AssertVec(Vector3 actual, Vector3 expected, float tol = 1e-3f)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(tol), $"expected {expected} got {actual}");
        }

        [Test]
        public void NearestPointOnPolylinePicksClosestRoad()
        {
            var src = Roads(
                (new[] { Vector3.zero, new Vector3(10f, 0f, 0f) }, true),
                (new[] { new Vector3(0f, 0f, 20f), new Vector3(10f, 0f, 20f) }, true));

            Assert.That(RoadReset.TryFindNearest(src, new Vector3(4f, 0f, 3f), out RoadHit a), Is.True);
            Assert.That(a.RoadIndex, Is.EqualTo(0));
            AssertVec(a.Point, new Vector3(4f, 0f, 0f));
            Assert.That(a.Distance, Is.EqualTo(3f).Within(1e-3f));
            Assert.That(a.DistanceAlong, Is.EqualTo(4f).Within(1e-3f));

            Assert.That(RoadReset.TryFindNearest(src, new Vector3(6f, 1f, 18f), out RoadHit b), Is.True);
            Assert.That(b.RoadIndex, Is.EqualTo(1));
            AssertVec(b.Point, new Vector3(6f, 0f, 20f));

            Assert.That(RoadReset.TryFindNearest(src, new Vector3(-5f, 0f, 0f), out RoadHit c), Is.True);
            AssertVec(c.Point, Vector3.zero);
        }

        [Test]
        public void HeadingFollowsTheSegmentAtTheResetPoint()
        {
            var src = Roads((new[] { Vector3.zero, new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f) }, false));

            Assert.That(RoadReset.TryComputePose(src, new Vector3(3f, 0f, -2f), Vector3.forward, false, Vector3.zero,
                Settings(lift: 0.5f), out Vector3 p1, out Quaternion r1), Is.True);
            AssertVec(p1, new Vector3(3f, 0.5f, 0f));
            AssertVec(r1 * Vector3.forward, Vector3.right);

            Assert.That(RoadReset.TryComputePose(src, new Vector3(12f, 0f, 6f), Vector3.forward, false, Vector3.zero,
                Settings(), out Vector3 p2, out Quaternion r2), Is.True);
            AssertVec(p2, new Vector3(10f, 0f, 6f));
            AssertVec(r2 * Vector3.forward, Vector3.forward);
            AssertVec(r2 * Vector3.up, Vector3.up);
        }

        [Test]
        public void LastOnRoadPointWinsWithinRadiusOtherwiseNearest()
        {
            var src = Roads(
                (new[] { Vector3.zero, new Vector3(100f, 0f, 0f) }, true),
                (new[] { new Vector3(0f, 0f, 12f), new Vector3(100f, 0f, 12f) }, true));
            var vehicle = new Vector3(50f, 0f, 8f); // nearer the second road
            var last = new Vector3(45f, 0f, 0f);    // left the first road here

            Assert.That(RoadReset.TryChooseHit(src, vehicle, true, last, 30f, out RoadHit near, out bool usedLast), Is.True);
            Assert.That(usedLast, Is.True);
            Assert.That(near.RoadIndex, Is.EqualTo(0));
            AssertVec(near.Point, last);

            Assert.That(RoadReset.TryChooseHit(src, vehicle, true, last, 3f, out RoadHit far, out bool usedLast2), Is.True);
            Assert.That(usedLast2, Is.False);
            Assert.That(far.RoadIndex, Is.EqualTo(1));
            AssertVec(far.Point, new Vector3(50f, 0f, 12f));

            Assert.That(RoadReset.TryChooseHit(src, vehicle, false, last, 30f, out RoadHit none, out _), Is.True);
            Assert.That(none.RoadIndex, Is.EqualTo(1));
        }

        [Test]
        public void OneWayKeepsPointOrderTwoWayFollowsVehicleFacing()
        {
            Vector3 roadDir = Vector3.forward;
            AssertVec(RoadReset.Heading(roadDir, false, Vector3.back), Vector3.forward);
            AssertVec(RoadReset.Heading(roadDir, true, Vector3.back), Vector3.back);
            AssertVec(RoadReset.Heading(roadDir, true, new Vector3(1f, 0f, 0.1f)), Vector3.forward);
            AssertVec(RoadReset.Heading(roadDir, true, Vector3.up), Vector3.forward);

            var oneWay = Roads((new[] { Vector3.zero, new Vector3(0f, 0f, 10f) }, false));
            var twoWay = Roads((new[] { Vector3.zero, new Vector3(0f, 0f, 10f) }, true));
            RoadReset.TryComputePose(oneWay, new Vector3(3f, 0f, 5f), Vector3.back, false, Vector3.zero, Settings(),
                out _, out Quaternion r1);
            RoadReset.TryComputePose(twoWay, new Vector3(3f, 0f, 5f), Vector3.back, false, Vector3.zero, Settings(),
                out _, out Quaternion r2);
            AssertVec(r1 * Vector3.forward, Vector3.forward);
            AssertVec(r2 * Vector3.forward, Vector3.back);
        }

        [Test]
        public void LiftAndLaneOffsetMoveUpAndRightOfHeading()
        {
            var src = Roads((new[] { Vector3.zero, new Vector3(0f, 0f, 10f) }, true));
            RoadReset.TryComputePose(src, new Vector3(-3f, 0f, 5f), Vector3.forward, false, Vector3.zero,
                Settings(lift: 0.5f, lane: 2f), out Vector3 p, out _);
            AssertVec(p, new Vector3(2f, 0.5f, 5f));
            RoadReset.TryComputePose(src, new Vector3(-3f, 0f, 5f), Vector3.back, false, Vector3.zero,
                Settings(lift: 0.5f, lane: 2f), out Vector3 q, out _);
            AssertVec(q, new Vector3(-2f, 0.5f, 5f));
        }

        [Test]
        public void MissingOrEmptyRoadsReturnFalse()
        {
            Assert.That(RoadReset.TryFindNearest(null, Vector3.zero, out _), Is.False);
            Assert.That(RoadReset.TryFindNearest(new RoadSourceList(), Vector3.zero, out _), Is.False);
            var src = new RoadSourceList();
            src.Add(new Vector3[0]);
            src.Add((RoadPolyline)null);
            Assert.That(RoadReset.TryComputePose(src, Vector3.one, Vector3.forward, false, Vector3.zero, Settings(),
                out Vector3 p, out _), Is.False);
            Assert.That(p, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void ResetterStopsBodyPlacesOnRoadAndRecords()
        {
            var go = new GameObject("Vehicle");
            try
            {
                var rb = go.AddComponent<Rigidbody>();
                var resetter = go.AddComponent<RoadResetter>();
                resetter.Source = Roads((new[] { Vector3.zero, new Vector3(0f, 0f, 50f) }, true));
                resetter.LiftHeight = 1f;
                resetter.OnRoadDistance = 4f;

                go.transform.position = new Vector3(2f, 0f, 10f);
                Assert.That(resetter.RecordNow(), Is.True);
                AssertVec(resetter.LastOnRoadPoint, new Vector3(0f, 0f, 10f));

                go.transform.SetPositionAndRotation(new Vector3(15f, 3f, 20f), Quaternion.Euler(80f, 200f, 30f));
                Assert.That(resetter.RecordNow(), Is.False);
                rb.linearVelocity = new Vector3(5f, 2f, 1f);
                rb.angularVelocity = new Vector3(1f, 3f, 0f);
                int placed = 0;
                resetter.Placed += (pos, rot) => placed++;

                Assert.That(resetter.ResetToRoad(), Is.True);
                Assert.That(placed, Is.EqualTo(1));
                AssertVec(go.transform.position, new Vector3(0f, 1f, 10f));
                AssertVec(go.transform.up, Vector3.up);
                AssertVec(rb.linearVelocity, Vector3.zero);
                AssertVec(rb.angularVelocity, Vector3.zero);
                Assert.That(resetter.HasLastOnRoad, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}