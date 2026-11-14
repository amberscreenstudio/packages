using UnityEngine;

namespace AmberScreen.Inspect
{
    public static class InspectMath
    {
        /// <summary>Clamps a zoom distance into [min, max] (swaps them if given the wrong way round).</summary>
        public static float ClampDistance(float distance, float min, float max)
        {
            if (min > max) (min, max) = (max, min);
            min = Mathf.Max(0.01f, min);
            max = Mathf.Max(min, max);
            return Mathf.Clamp(distance, min, max);
        }

        /// <summary>Applies a drag/stick rotation (degrees) in camera space: x turns left/right, y tilts up/down.</summary>
        public static Quaternion RotateInViewSpace(Quaternion localRotation, Vector2 degrees) =>
            Quaternion.AngleAxis(-degrees.x, Vector3.up) * Quaternion.AngleAxis(degrees.y, Vector3.right) * localRotation;

        /// <summary>Wraps an index for next/previous stepping.</summary>
        public static int Step(int index, int direction, int count)
        {
            if (count <= 0) return -1;
            if (index < 0) return direction >= 0 ? 0 : count - 1;
            return ((index + direction) % count + count) % count;
        }
    }
}
