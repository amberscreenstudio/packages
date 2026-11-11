using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>World-space polyline snapshot. The game sets the points; this type does not find routes.</summary>
    public sealed class RoutePath
    {
        private static readonly Vector3[] EmptyPoints = Array.Empty<Vector3>();
        private static readonly float[] EmptyLengths = Array.Empty<float>();
        private Vector3[] points = EmptyPoints;
        private float[] cumulative = EmptyLengths;

        public IReadOnlyList<Vector3> Points => points;
        public IReadOnlyList<float> CumulativeLengths => cumulative;
        public float Length { get; private set; }
        public event Action Changed;

        public void SetPoints(IReadOnlyList<Vector3> worldPoints)
        {
            var copy = new List<Vector3>();
            if (worldPoints != null)
            {
                for (int i = 0; i < worldPoints.Count; i++)
                {
                    Vector3 p = worldPoints[i];
                    if (!Finite(p)) continue;
                    if (copy.Count > 0 && (p - copy[copy.Count - 1]).sqrMagnitude < 1e-12f) continue;
                    copy.Add(p);
                }
            }

            points = copy.Count == 0 ? EmptyPoints : copy.ToArray();
            cumulative = points.Length == 0 ? EmptyLengths : new float[points.Length];
            float total = 0f;
            for (int i = 1; i < points.Length; i++)
            {
                total += Vector3.Distance(points[i - 1], points[i]);
                cumulative[i] = total;
            }
            Length = total;
            Changed?.Invoke();
        }

        public bool SampleAtDistance(float distance, out Vector3 position, out Vector3 direction)
        {
            if (points.Length == 0)
            {
                position = Vector3.zero;
                direction = Vector3.forward;
                return false;
            }
            if (points.Length == 1 || Length <= 1e-8f)
            {
                position = points[0];
                direction = Vector3.forward;
                return true;
            }
            if (!Finite(distance)) distance = 0f;
            if (distance <= 0f)
            {
                position = points[0];
                direction = SegmentDirection(0);
                return true;
            }
            if (distance >= Length)
            {
                position = points[points.Length - 1];
                direction = SegmentDirection(points.Length - 2);
                return true;
            }
            int i = 1;
            while (i < cumulative.Length && cumulative[i] < distance) i++;
            int a = i - 1;
            float span = cumulative[i] - cumulative[a];
            float t = span > 1e-8f ? (distance - cumulative[a]) / span : 0f;
            position = Vector3.Lerp(points[a], points[i], t);
            direction = SegmentDirection(a);
            return true;
        }

        public float ClosestDistance(Vector3 worldPos)
        {
            if (points.Length == 0 || !Finite(worldPos)) return 0f;
            if (points.Length == 1 || Length <= 1e-8f) return 0f;
            float bestSqr = float.PositiveInfinity;
            float bestAlong = 0f;
            for (int i = 1; i < points.Length; i++)
            {
                Vector3 a = points[i - 1];
                Vector3 b = points[i];
                Vector3 ab = b - a;
                float l2 = ab.sqrMagnitude;
                float t = l2 > 1e-12f ? Mathf.Clamp01(Vector3.Dot(worldPos - a, ab) / l2) : 0f;
                Vector3 closest = a + ab * t;
                float sqr = (worldPos - closest).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestAlong = cumulative[i - 1] + t * (cumulative[i] - cumulative[i - 1]);
                }
            }
            return bestAlong;
        }

        private Vector3 SegmentDirection(int startIndex)
        {
            for (int i = startIndex; i < points.Length - 1; i++)
            {
                Vector3 d = points[i + 1] - points[i];
                if (d.sqrMagnitude > 1e-12f) return d.normalized;
            }
            for (int i = startIndex - 1; i >= 0; i--)
            {
                Vector3 d = points[i + 1] - points[i];
                if (d.sqrMagnitude > 1e-12f) return d.normalized;
            }
            return Vector3.forward;
        }

        private static bool Finite(Vector3 p) =>
            !(float.IsNaN(p.x) || float.IsInfinity(p.x) ||
              float.IsNaN(p.y) || float.IsInfinity(p.y) ||
              float.IsNaN(p.z) || float.IsInfinity(p.z));

        private static bool Finite(float v) => !(float.IsNaN(v) || float.IsInfinity(v));
    }
}
