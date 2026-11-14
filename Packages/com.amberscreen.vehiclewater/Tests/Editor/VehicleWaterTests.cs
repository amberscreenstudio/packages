using NUnit.Framework;
using UnityEngine;

namespace AmberScreen.VehicleWater.Tests
{
    public sealed class VehicleWaterTests
    {
        const float Dry = float.NegativeInfinity;

        static WaterIngressSettings Settings() => new WaterIngressSettings
        {
            intakeStallDepth = 0.05f,
            cabinFloodDepth = 0f,
            floodPerSecond = 0.1f,
            fullRateDepth = 0f,
            drainPerSecond = 0.02f,
            stallFloodLevel = 0.6f,
            stopFloodLevel = 0.3f,
            drainedLevel = 0.001f
        };

        [Test]
        public void IntakeUnderThresholdDoesNotStall()
        {
            var m = new WaterIngressModel(Settings());
            var e = m.Step(0.049f, Dry, 0.02f);
            Assert.That(m.IsStalled, Is.False);
            Assert.That(e & WaterIngressEvents.Stalled, Is.EqualTo(WaterIngressEvents.None));
        }

        [Test]
        public void IntakeOverThresholdStallsOnce()
        {
            var m = new WaterIngressModel(Settings());
            var first = m.Step(0.051f, Dry, 0.02f);
            var second = m.Step(0.2f, Dry, 0.02f);
            Assert.That(m.IsStalled, Is.True);
            Assert.That(first & WaterIngressEvents.Stalled, Is.EqualTo(WaterIngressEvents.Stalled));
            Assert.That(second & WaterIngressEvents.Stalled, Is.EqualTo(WaterIngressEvents.None));
            Assert.That(m.IsStoppedByWater, Is.True);
        }

        [Test]
        public void CabinFloodsAtTheSetRate()
        {
            var m = new WaterIngressModel(Settings());
            var first = m.Step(Dry, 0.5f, 1f);
            Assert.That(first & WaterIngressEvents.FloodStarted, Is.EqualTo(WaterIngressEvents.FloodStarted));
            m.Step(Dry, 0.5f, 1f);
            m.Step(Dry, 0.5f, 1f);
            Assert.That(m.Flood, Is.EqualTo(0.3f).Within(1e-5f));
            for (int i = 0; i < 20; i++) m.Step(Dry, 0.5f, 1f);
            Assert.That(m.Flood, Is.EqualTo(1f), "clamped at full");
        }

        [Test]
        public void ShallowCabinWaterFloodsSlowerWithFullRateDepth()
        {
            var s = Settings();
            s.fullRateDepth = 0.3f;
            var m = new WaterIngressModel(s);
            m.Step(Dry, 0.15f, 1f); // half of full-rate depth
            Assert.That(m.Flood, Is.EqualTo(0.05f).Within(1e-5f));
        }

        [Test]
        public void FloodDrainsSlowlyOutOfWater()
        {
            var m = new WaterIngressModel(Settings());
            m.SetFlood(0.5f);
            m.Step(Dry, Dry, 1f);
            Assert.That(m.Flood, Is.EqualTo(0.48f).Within(1e-5f));
            m.Step(-1f, -1f, 10f);
            Assert.That(m.Flood, Is.EqualTo(0.28f).Within(1e-5f));
        }

        [Test]
        public void HeavyFloodAloneStallsTheEngine()
        {
            var m = new WaterIngressModel(Settings());
            m.SetFlood(0.59f);
            m.Step(Dry, 0.5f, 0.2f);
            Assert.That(m.IsStalled, Is.True);
        }

        [Test]
        public void RestartOnlyAfterDrainedAndWhenGameAllows()
        {
            var m = new WaterIngressModel(Settings());
            m.Step(0.5f, 0.5f, 2f); // stall, flood 0.2
            Assert.That(m.IsStalled, Is.True);
            Assert.That(m.NotifyRestarted(), Is.False, "can't restart under water");

            m.Step(Dry, Dry, 1f); // out of the water, still wet
            Assert.That(m.CanRestart, Is.False);
            Assert.That(m.Flood, Is.GreaterThan(0f));

            WaterIngressEvents events = WaterIngressEvents.None;
            for (int i = 0; i < 20 && m.Flood > 0f; i++) events = m.Step(Dry, Dry, 1f, gameAllowsRestart: false);
            Assert.That(m.Flood, Is.EqualTo(0f));
            Assert.That(m.CanRestart, Is.False, "game said no");

            events = m.Step(Dry, Dry, 0.02f, gameAllowsRestart: true);
            Assert.That(m.CanRestart, Is.True);
            Assert.That(events & WaterIngressEvents.RestartAllowed, Is.EqualTo(WaterIngressEvents.RestartAllowed));
            Assert.That(m.NotifyRestarted(), Is.True);
            Assert.That(m.IsStalled, Is.False);
        }

        [Test]
        public void DrainedEventWhenFloodReachesZero()
        {
            var m = new WaterIngressModel(Settings());
            m.SetFlood(0.01f);
            var e = m.Step(Dry, Dry, 1f);
            Assert.That(e & WaterIngressEvents.Drained, Is.EqualTo(WaterIngressEvents.Drained));
        }

        [Test]
        public void StoppedUntilFloodDropsBelowStopLevel()
        {
            var m = new WaterIngressModel(Settings());
            m.Step(0.5f, 0.5f, 4f); // stall, flood 0.4
            Assert.That(m.IsStoppedByWater, Is.True);
            m.Step(Dry, Dry, 1f); // flood 0.38, still over stop level
            Assert.That(m.IsStoppedByWater, Is.True);
            var e = m.Step(Dry, Dry, 5f); // flood 0.28
            Assert.That(m.IsStoppedByWater, Is.False);
            Assert.That(e & WaterIngressEvents.ReleasedByWater, Is.EqualTo(WaterIngressEvents.ReleasedByWater));
        }

        [Test]
        public void DepthTreatsNoWaterAsDry()
        {
            Assert.That(WaterIngressModel.Depth(1f, 0.25f), Is.EqualTo(0.75f).Within(1e-6f));
            Assert.That(float.IsNegativeInfinity(WaterIngressModel.Depth(float.NegativeInfinity, 0f)), Is.True);
            Assert.That(float.IsNegativeInfinity(WaterIngressModel.Depth(float.NaN, 0f)), Is.True);
        }

        [Test]
        public void FlatWaterPlaneHeightAndArea()
        {
            var go = new GameObject("Water");
            try
            {
                go.transform.position = new Vector3(10f, 1.5f, 0f);
                var plane = go.AddComponent<FlatWaterPlane>();
                plane.heightOffset = 0.25f;
                Assert.That(plane.GetWaterHeight(new Vector3(500f, -3f, 500f)), Is.EqualTo(1.75f));
                plane.limitToArea = true;
                plane.areaSize = new Vector2(4f, 4f);
                Assert.That(plane.GetWaterHeight(new Vector3(11f, 0f, 1f)), Is.EqualTo(1.75f));
                Assert.That(float.IsNegativeInfinity(plane.GetWaterHeight(new Vector3(13f, 0f, 0f))), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        sealed class FakeEngine : IVehicleEngineControl
        {
            public bool running = true;
            public int stalls, restarts;
            public bool IsEngineRunning => running;
            public bool CanStart { get; set; } = true;
            public void Stall() { running = false; stalls++; }
            public void Restart() { if (CanStart) { running = true; restarts++; } }
        }

        sealed class FakeWater : IWaterHeightProvider
        {
            public float height;
            public float GetWaterHeight(Vector3 worldPosition) => height;
        }

        [Test]
        public void ComponentStallsEngineAddsDragAndRestartsWhenDrained()
        {
            var car = new GameObject("Car");
            try
            {
                var body = car.AddComponent<Rigidbody>();
                body.linearDamping = 0.1f;
                var ingress = car.AddComponent<VehicleWaterIngress>();
                ingress.body = body;
                ingress.settings = Settings();
                var engine = new FakeEngine();
                var water = new FakeWater { height = 1f }; // car at y = 0: one metre under
                ingress.SetEngineControl(engine);
                ingress.SetWaterProvider(water);
                int stalledEvents = 0;
                ingress.onStalled.AddListener(() => stalledEvents++);

                ingress.Simulate(1f);
                Assert.That(engine.running, Is.False);
                Assert.That(engine.stalls, Is.EqualTo(1));
                Assert.That(engine.CanStart, Is.False);
                Assert.That(stalledEvents, Is.EqualTo(1));
                Assert.That(body.linearDamping, Is.EqualTo(0.1f + ingress.settings.extraLinearDamping).Within(1e-5f));

                engine.running = true; // something tries to run it while drowned
                ingress.Simulate(0.02f);
                Assert.That(engine.running, Is.False, "stalled again");

                water.height = float.NegativeInfinity;
                for (int i = 0; i < 100 && !ingress.CanRestart; i++) ingress.Simulate(1f);
                Assert.That(ingress.CanRestart, Is.True);
                Assert.That(engine.CanStart, Is.True);
                Assert.That(body.linearDamping, Is.EqualTo(0.1f).Within(1e-5f), "damping restored");
                Assert.That(ingress.TryRestart(), Is.True);
                Assert.That(engine.running, Is.True);
                Assert.That(ingress.IsStalled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(car);
            }
        }
    }
}
