using System;
using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>Nearest point on one of the supplied roads.</summary>
    public struct RoadHit
    {
        public int RoadIndex;
        public RoadPolyline Road;
        /// <summary>Distance along the road's polyline, from its first point.</summary>
        public float DistanceAlong;
        public Vector3 Point;
        /// <summary>Direction of the polyline segment at <see cref="Point"/> (point order).</summary>
        public Vector3 Direction;
        /// <summary>3D distance from the query position to <see cref="Point"/>.</summary>
        public float Distance;
    }

    [Serializable]
    public struct RoadResetSettings
    {
        [Tooltip("Metres above the road point the vehicle pivot is placed.")]
        public float LiftHeight;
        [Tooltip("Metres to the right of the road centre (negative = left), e.g. to the lane centre.")]
        public float LaneOffset;
        [Tooltip("If the vehicle is within this many metres of the last recorded on-road point, reset there instead of to the nearest road.")]
        public float LastOnRoadRadius;

        public static RoadResetSettings Default => new RoadResetSettings
        {
            LiftHeight = 0.5f,
            LaneOffset = 0f,
            LastOnRoadRadius = 30f
        };
    }

    /// <summary>Pure reset-to-road maths. No physics, no input, no road or vehicle asset.</summary>
    public static class RoadReset
    {
        /// <summary>Finds the closest point on any road to <paramref name="worldPos"/>.</summary>
        public static bool TryFindNearest(IRoadSource source, Vector3 worldPos, out RoadHit hit)
        {
            hit = new RoadHit { RoadIndex = -1, Direction = Vector3.forward, Distance = float.PositiveInfinity };
            if (source == null || !Finite(worldPos)) return false;
            var roads = source.Roads;
            if (roads == null) return false;

            float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < roads.Count; i++)
            {
                RoadPolyline road = roads[i];
                if (road == null || road.Path == null || road.Path.Points.Count == 0) continue;
                float along = road.Path.ClosestDistance(worldPos);
                road.Path.SampleAtDistance(along, out Vector3 point, out Vector3 direction);
                float sqr = (worldPos - point).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    hit.RoadIndex = i;
                    hit.Road = road;
                    hit.DistanceAlong = along;
                    hit.Point = point;
                    hit.Direction = direction;
                }
            }
            if (hit.RoadIndex < 0) return false;
            hit.Distance = Mathf.Sqrt(bestSqr);
            return true;
        }

        /// <summary>
        /// Uses the last on-road point when the vehicle is still within <paramref name="lastOnRoadRadius"/>
        /// of it (so it goes back where it left the road); otherwise the road nearest the vehicle.
        /// </summary>
        public static bool TryChooseHit(IRoadSource source, Vector3 vehiclePosition, bool hasLastOnRoad,
            Vector3 lastOnRoadPoint, float lastOnRoadRadius, out RoadHit hit, out bool usedLastOnRoad)
        {
            usedLastOnRoad = false;
            if (hasLastOnRoad && Finite(lastOnRoadPoint) && lastOnRoadRadius > 0f &&
                (vehiclePosition - lastOnRoadPoint).sqrMagnitude <= lastOnRoadRadius * lastOnRoadRadius &&
                TryFindNearest(source, lastOnRoadPoint, out hit))
            {
                usedLastOnRoad = true;
                return true;
            }
            return TryFindNearest(source, vehiclePosition, out hit);
        }

        /// <summary>
        /// Facing for the reset. One-way roads face the point order. Two-way roads face whichever
        /// way along the road is closer to the vehicle's current forward (flattened to XZ).
        /// </summary>
        public static Vector3 Heading(Vector3 roadDirection, bool twoWay, Vector3 vehicleForward)
        {
            Vector3 dir = roadDirection.sqrMagnitude > 1e-12f ? roadDirection.normalized : Vector3.forward;
            if (!twoWay) return dir;
            var forward = new Vector3(vehicleForward.x, 0f, vehicleForward.z);
            var flat = new Vector3(dir.x, 0f, dir.z);
            if (forward.sqrMagnitude < 1e-8f || flat.sqrMagnitude < 1e-8f) return dir;
            return Vector3.Dot(forward, flat) < 0f ? -dir : dir;
        }

        /// <summary>Road point moved right by <paramref name="laneOffset"/> and up by <paramref name="liftHeight"/>.</summary>
        public static Vector3 OffsetPoint(Vector3 roadPoint, Vector3 heading, float laneOffset, float liftHeight)
        {
            var flat = new Vector3(heading.x, 0f, heading.z);
            Vector3 right = flat.sqrMagnitude > 1e-8f ? Vector3.Cross(Vector3.up, flat.normalized) : Vector3.right;
            return roadPoint + right * laneOffset + Vector3.up * liftHeight;
        }

        public static Quaternion RotationFromHeading(Vector3 heading)
        {
            if (heading.sqrMagnitude < 1e-8f) return Quaternion.identity;
            Vector3 dir = heading.normalized;
            if (Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.999f) return Quaternion.identity;
            return Quaternion.LookRotation(dir, Vector3.up);
        }

        public static bool TryComputePose(IRoadSource source, Vector3 vehiclePosition, Vector3 vehicleForward,
            bool hasLastOnRoad, Vector3 lastOnRoadPoint, RoadResetSettings settings,
            out Vector3 position, out Quaternion rotation)
        {
            return TryComputePose(source, vehiclePosition, vehicleForward, hasLastOnRoad, lastOnRoadPoint, settings,
                out position, out rotation, out _);
        }

        public static bool TryComputePose(IRoadSource source, Vector3 vehiclePosition, Vector3 vehicleForward,
            bool hasLastOnRoad, Vector3 lastOnRoadPoint, RoadResetSettings settings,
            out Vector3 position, out Quaternion rotation, out RoadHit hit)
        {
            position = vehiclePosition;
            rotation = Quaternion.identity;
            if (!TryChooseHit(source, vehiclePosition, hasLastOnRoad, lastOnRoadPoint, settings.LastOnRoadRadius,
                    out hit, out _))
                return false;
            bool twoWay = hit.Road == null || hit.Road.TwoWay;
            Vector3 heading = Heading(hit.Direction, twoWay, vehicleForward);
            position = OffsetPoint(hit.Point, heading, settings.LaneOffset, settings.LiftHeight);
            rotation = RotationFromHeading(heading);
            return true;
        }

        private static bool Finite(Vector3 p) =>
            !(float.IsNaN(p.x) || float.IsInfinity(p.x) ||
              float.IsNaN(p.y) || float.IsInfinity(p.y) ||
              float.IsNaN(p.z) || float.IsInfinity(p.z));
    }
}