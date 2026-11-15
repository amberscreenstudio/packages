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

        /// <summary>
        /// Largest half-extent of the object's enabled renderers (world space), at least 0.0005 m.
        /// 0.01 m when it has no renderers.
        /// </summary>
        public static float RenderedExtent(Transform target)
        {
            float extent = 0.01f;
            if (target == null) return extent;
            var rs = target.GetComponentsInChildren<Renderer>(true);
            bool any = false;
            var b = new Bounds();
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null || !rs[i].enabled) continue;
                if (!any) { b = rs[i].bounds; any = true; }
                else b.Encapsulate(rs[i].bounds);
            }
            if (any) extent = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
            return Mathf.Max(extent, 0.0005f);
        }

        /// <summary>Hold, min and max distances from a half-extent and three multipliers (defaults 3, 1, 8).</summary>
        public static void FitDistances(float extent, float holdMultiplier, float minMultiplier, float maxMultiplier,
            out float hold, out float min, out float max)
        {
            extent = Mathf.Max(extent, 0.0005f);
            hold = extent * holdMultiplier;
            min = extent * minMultiplier;
            max = extent * maxMultiplier;
        }

        /// <summary>Wraps an index for next/previous stepping.</summary>
        public static int Step(int index, int direction, int count)
        {
            if (count <= 0) return -1;
            if (index < 0) return direction >= 0 ? 0 : count - 1;
            return ((index + direction) % count + count) % count;
        }
    }
}
