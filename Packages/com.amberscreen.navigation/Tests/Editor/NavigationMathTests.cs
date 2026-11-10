using System;
using NUnit.Framework;

namespace AmberScreen.Navigation.Tests
{
    public sealed class NavigationMathTests
    {
        [TestCase(0f, 0f)]
        [TestCase(360f, 0f)]
        [TestCase(-1f, 359f)]
        [TestCase(721f, 1f)]
        [TestCase(-720f, 0f)]
        public void WrapIsInUnsignedRange(float input, float expected) =>
            Assert.That(NavigationMath.Wrap360(input), Is.EqualTo(expected).Within(0.0001f));

        [TestCase(0f, 0f)]
        [TestCase(180f, 180f)]
        [TestCase(-180f, 180f)]
        [TestCase(181f, -179f)]
        [TestCase(-181f, 179f)]
        public void SignedWrapUsesPositiveHalfTurn(float input, float expected) =>
            Assert.That(NavigationMath.WrapSigned(input), Is.EqualTo(expected).Within(0.0001f));

        [TestCase(0f, 1f, 0f)]
        [TestCase(1f, 0f, 90f)]
        [TestCase(0f, -1f, 180f)]
        [TestCase(-1f, 0f, 270f)]
        public void CardinalBearingsAreClockwise(float x, float z, float expected) =>
            Assert.That(NavigationMath.Bearing(0, 0, x, z), Is.EqualTo(expected).Within(0.0001f));

        [Test] public void RelativeAngle350Against10IsMinus20() =>
            Assert.That(NavigationMath.RelativeAngle(350f, 10f), Is.EqualTo(-20f));

        [Test] public void ConfigurableNorthCanPointEast() =>
            Assert.That(NavigationMath.DirectionBearing(0f, -1f, 1f, 0f), Is.EqualTo(90f).Within(0.0001f));

        [Test] public void NonUnitNorthDoesNotChangeBearing() =>
            Assert.That(NavigationMath.DirectionBearing(1f, 0f, 0f, 20f), Is.EqualTo(90f).Within(0.0001f));

        [Test] public void ZeroNorthFallsBackToPositiveZ() =>
            Assert.That(NavigationMath.DirectionBearing(1f, 0f, 0f, 0f), Is.EqualTo(90f).Within(0.0001f));

        [Test] public void CoincidentXZReturnsDocumentedFallback()
        {
            Assert.That(NavigationMath.Bearing(3f, 9f, 3f, 9f, fallback: 392f), Is.EqualTo(32f));
            Assert.That(NavigationMath.Bearing(0, 0, 0, 0), Is.EqualTo(0f));
        }
        [Test] public void ZeroDistanceIsZero() =>
            Assert.That(NavigationMath.HorizontalDistance(5, 8, 5, 8), Is.Zero);

        [Test] public void HorizontalDistanceUsesPythagoras() =>
            Assert.That(NavigationMath.HorizontalDistance(10, 20, 13, 24), Is.EqualTo(5f));

        [Test] public void SpringCrossesNorthOnShortArc()
        {
            var spring = new CompassSpring(); spring.SetHeading(359f);
            spring.Step(1f, 0.1f);
            Assert.That(NavigationMath.RelativeAngle(spring.HeadingDeg, 359f), Is.EqualTo(0.52f).Within(0.0001f));
        }
        [Test] public void SpringUsesOriginalStiffnessAndDamping()
        {
            var spring = new CompassSpring(); spring.SetHeading(0f);
            spring.Step(10f, 0.1f);
            Assert.That(spring.HeadingDeg, Is.EqualTo(2.6f).Within(0.0001f));
            spring.Step(10f, 0.1f);
            Assert.That(spring.HeadingDeg, Is.EqualTo(5.564f).Within(0.0001f));
        }
        [Test] public void SpringClampsOffsetToOriginalDefault16Degrees()
        {
            var spring = new CompassSpring(); spring.SetHeading(0f);
            spring.Step(180f, 0.01f);
            Assert.That(spring.HeadingDeg, Is.EqualTo(164f).Within(0.0001f));
        }
        [Test] public void VerticalBumpUsesVelocityDifferenceNotVelocity()
        {
            var spring = new CompassSpring(); spring.SetHeading(0f);
            spring.Step(0f, 0.1f, 2f, stiffness: 0f, damping: 0f);
            Assert.That(spring.HeadingDeg, Is.EqualTo(0.7f).Within(0.0001f));
            spring.Step(0f, 0.1f, 2f, stiffness: 0f, damping: 0f);
            Assert.That(spring.HeadingDeg, Is.EqualTo(1.4f).Within(0.0001f));
        }
        [Test] public void SetHeadingResetsAngularVelocity()
        {
            var spring = new CompassSpring(); spring.Step(10f, 0.1f);
            spring.SetHeading(5f);
            spring.Step(5f, 0.1f, stiffness: 0f, damping: 0f, bumpGain: 0f);
            Assert.That(spring.HeadingDeg, Is.EqualTo(5f));
        }
        [Test] public void SetHeadingKeepsLastVerticalVelocityLikeOriginal()
        {
            var spring = new CompassSpring(); spring.Step(0f, 0.1f, 2f);
            spring.SetHeading(0f);
            spring.Step(0f, 0.1f, 2f, stiffness: 0f, damping: 0f);
            Assert.That(spring.HeadingDeg, Is.Zero);
        }
        [Test] public void ZeroAndNegativeDeltaUseOriginalMinimumTimeStep()
        {
            var zero = new CompassSpring(); zero.Step(10f, 0f);
            var negative = new CompassSpring(); negative.Step(10f, -1f);
            var minimum = new CompassSpring(); minimum.Step(10f, 1e-4f);
            Assert.That(zero.HeadingDeg, Is.EqualTo(minimum.HeadingDeg));
            Assert.That(negative.HeadingDeg, Is.EqualTo(minimum.HeadingDeg));
            Assert.That(float.IsNaN(zero.HeadingDeg), Is.False);
        }
        [Test] public void SpringOffsetRemainsBoundedUnderTurningAndBumps()
        {
            var spring = new CompassSpring();
            for (int i = 0; i < 2000; i++)
            {
                float trueHeading = (float)Math.Sin(i * 0.037) * 180f;
                spring.Step(trueHeading, 0.017f, (float)Math.Sin(i * 0.13) * 4f);
                Assert.That(Math.Abs(NavigationMath.RelativeAngle(spring.HeadingDeg, trueHeading)),
                    Is.LessThanOrEqualTo(16.0001f));
            }
        }
        [Test] public void TranslationDoesNotChangeBearing() =>
            Assert.That(NavigationMath.Bearing(7, 8, 8, 9), Is.EqualTo(45f).Within(0.0001f));
    }
}
