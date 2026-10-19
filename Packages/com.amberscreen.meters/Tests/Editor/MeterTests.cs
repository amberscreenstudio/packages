using System.Collections.Generic;
using NUnit.Framework;

namespace AmberScreen.Meters.Tests
{
    public class MeterTests
    {
        [Test]
        public void DrainToEmpty_FiresBecameEmptyOnce()
        {
            var meter = new Meter("stamina", 100f, 100f);
            int emptyCount = 0;
            meter.BecameEmpty += () => emptyCount++;

            meter.Drain(100f, 1f);
            Assert.AreEqual(1, emptyCount);
            Assert.IsTrue(meter.IsEmpty);

            meter.Drain(50f, 1f);
            Assert.AreEqual(1, emptyCount);
        }

        [Test]
        public void FillToMax_FiresBecameFullOnce()
        {
            var meter = new Meter("stamina", 100f, 0f);
            int fullCount = 0;
            meter.BecameFull += () => fullCount++;

            meter.Fill(100f, 1f);
            Assert.AreEqual(1, fullCount);
            Assert.IsTrue(meter.IsFull);

            meter.Fill(50f, 1f);
            Assert.AreEqual(1, fullCount);
        }

        [Test]
        public void CrossingThreshold_FiresOncePerCrossing()
        {
            var thresholds = new List<MeterThreshold>
            {
                new MeterThreshold("low", 0.25f)
            };
            var meter = new Meter("stamina", 100f, 100f, thresholdList: thresholds);
            int downCount = 0;
            int upCount = 0;
            meter.ThresholdCrossedDown += name =>
            {
                if (name == "low")
                    downCount++;
            };
            meter.ThresholdCrossedUp += name =>
            {
                if (name == "low")
                    upCount++;
            };

            meter.Set(20f);
            Assert.AreEqual(1, downCount);

            meter.Set(10f);
            Assert.AreEqual(1, downCount);

            meter.Set(50f);
            Assert.AreEqual(1, upCount);

            meter.Set(80f);
            Assert.AreEqual(1, upCount);

            meter.Set(20f);
            Assert.AreEqual(2, downCount);
        }

        [Test]
        public void CaptureThenRestore_GivesSameValue()
        {
            var meter = new Meter("stamina", 100f, 100f);
            meter.Set(42.5f);
            var state = meter.Capture();

            meter.Set(7f);
            Assert.AreEqual(7f, meter.Current, 0.0001f);

            meter.Apply(state);
            Assert.AreEqual(42.5f, meter.Current, 0.0001f);
            Assert.AreEqual(100f, meter.Max, 0.0001f);
            Assert.AreEqual("stamina", state.Id);
        }
    }
}
