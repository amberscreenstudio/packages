using System;

namespace AmberScreen.Navigation
{
    /// <summary>Pure XZ-plane maths. +Y is up; angles increase clockwise viewed from above.</summary>
    public static class NavigationMath
    {
        public static float Wrap360(float angle)
        {
            float result = angle % 360f;
            return result < 0f ? result + 360f : result;
        }

        /// <summary>Range (-180, 180]; the ambiguous half-turn is always +180.</summary>
        public static float WrapSigned(float angle)
        {
            float result = Wrap360(angle);
            return result > 180f ? result - 360f : result;
        }

        public static float RelativeAngle(float bearing, float heading) => WrapSigned(bearing - heading);

        /// <summary>Degenerate direction returns wrapped fallback. Degenerate north defaults to +Z.</summary>
        public static float DirectionBearing(float x, float z, float northX = 0f,
            float northZ = 1f, float fallback = 0f)
        {
            if ((double)x * x + (double)z * z < 1e-12) return Wrap360(fallback);
            if ((double)northX * northX + (double)northZ * northZ < 1e-12)
            { northX = 0f; northZ = 1f; }
            double cross = (double)northZ * x - (double)northX * z;
            double dot = (double)northX * x + (double)northZ * z;
            return Wrap360((float)(Math.Atan2(cross, dot) * 180.0 / Math.PI));
        }

        public static float Bearing(float fromX, float fromZ, float toX, float toZ,
            float northX = 0f, float northZ = 1f, float fallback = 0f) =>
            DirectionBearing(toX - fromX, toZ - fromZ, northX, northZ, fallback);

        public static float HorizontalDistance(float fromX, float fromZ, float toX, float toZ)
        {
            double x = (double)toX - fromX, z = (double)toZ - fromZ;
            return (float)Math.Sqrt(x * x + z * z);
        }

    }

    /// <summary>Sand &amp; Ink CompassInstrument spring, separated only for scene-free testing.</summary>
    public sealed class CompassSpring
    {
        private float velocity;
        private float lastVy;
        public float HeadingDeg { get; private set; }
        public float TrueHeadingDeg { get; private set; }

        // Match CompassInstrument: reset angular velocity, not the remembered vertical velocity.
        public void SetHeading(float degrees)
        {
            HeadingDeg = degrees;
            velocity = 0f;
        }

        public void Step(float trueHeadingDeg, float deltaTime, float verticalVelocity = 0f,
            float stiffness = 26f, float damping = 6f, float bumpGain = 3.5f, float maxOffset = 16f)
        {
            float dt = Math.Max(deltaTime, 1e-4f);
            TrueHeadingDeg = trueHeadingDeg;
            // RelativeAngle is the pure equivalent of Mathf.DeltaAngle(current, target).
            float err = NavigationMath.RelativeAngle(TrueHeadingDeg, HeadingDeg);
            float acc = err * stiffness - velocity * damping;
            acc += (verticalVelocity - lastVy) / dt * bumpGain;
            lastVy = verticalVelocity;
            velocity += acc * dt;
            HeadingDeg += velocity * dt;
            float offset = NavigationMath.RelativeAngle(HeadingDeg, TrueHeadingDeg);
            HeadingDeg = TrueHeadingDeg + Math.Max(-maxOffset, Math.Min(maxOffset, offset));
        }
    }
}
