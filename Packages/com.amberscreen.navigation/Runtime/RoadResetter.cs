using System;
using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>
    /// Records where a vehicle was last on a road and puts it back on the road, stopped, facing
    /// the way the road runs. The game assigns <see cref="Source"/> and calls <see cref="ResetToRoad"/>
    /// from its own input. No input, road or vehicle asset is referenced.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoadResetter : MonoBehaviour
    {
        [Tooltip("Transform to move. Empty = this GameObject.")]
        [SerializeField] private Transform target;
        [Tooltip("Body to stop. Empty = Rigidbody on this GameObject, if any.")]
        [SerializeField] private Rigidbody body;
        [Tooltip("Metres above the road point the vehicle pivot is placed.")]
        [SerializeField, Min(0f)] private float liftHeight = 0.5f;
        [Tooltip("Metres to the right of the road centre (negative = left), e.g. to the lane centre.")]
        [SerializeField] private float laneOffset;
        [Tooltip("If the vehicle is within this many metres of the last on-road point, reset there instead of to the nearest road.")]
        [SerializeField, Min(0f)] private float lastOnRoadRadius = 30f;
        [Tooltip("The vehicle counts as on the road when it is this close to a road centre line.")]
        [SerializeField, Min(0f)] private float onRoadDistance = 4f;
        [Tooltip("Seconds between automatic on-road checks.")]
        [SerializeField, Min(0f)] private float recordInterval = 0.2f;
        [SerializeField] private bool recordAutomatically = true;

        private float nextRecordTime;

        /// <summary>Road centre lines, supplied by the game.</summary>
        public IRoadSource Source { get; set; }

        public Transform Target { get => target != null ? target : transform; set => target = value; }
        public Rigidbody Body { get => body; set => body = value; }
        public float LiftHeight { get => liftHeight; set => liftHeight = Mathf.Max(0f, value); }
        public float LaneOffset { get => laneOffset; set => laneOffset = value; }
        public float LastOnRoadRadius { get => lastOnRoadRadius; set => lastOnRoadRadius = Mathf.Max(0f, value); }
        public float OnRoadDistance { get => onRoadDistance; set => onRoadDistance = Mathf.Max(0f, value); }
        public float RecordInterval { get => recordInterval; set => recordInterval = Mathf.Max(0f, value); }
        public bool RecordAutomatically { get => recordAutomatically; set => recordAutomatically = value; }

        public bool HasLastOnRoad { get; private set; }
        /// <summary>Road centre point recorded the last time the vehicle was on a road.</summary>
        public Vector3 LastOnRoadPoint { get; private set; }

        public RoadResetSettings Settings => new RoadResetSettings
        {
            LiftHeight = liftHeight,
            LaneOffset = laneOffset,
            LastOnRoadRadius = lastOnRoadRadius
        };

        /// <summary>Raised after a reset with the new position and rotation, so the game can clear its vehicle's inputs.</summary>
        public event Action<Vector3, Quaternion> Placed;

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (!recordAutomatically || Time.time < nextRecordTime) return;
            nextRecordTime = Time.time + recordInterval;
            RecordNow();
        }

        /// <summary>Records the nearest road point if the vehicle is within <see cref="OnRoadDistance"/>.</summary>
        public bool RecordNow()
        {
            if (!RoadReset.TryFindNearest(Source, Target.position, out RoadHit hit)) return false;
            if (hit.Distance > onRoadDistance) return false;
            HasLastOnRoad = true;
            LastOnRoadPoint = hit.Point;
            return true;
        }

        public void ClearLastOnRoad()
        {
            HasLastOnRoad = false;
            LastOnRoadPoint = Vector3.zero;
        }

        /// <summary>Puts the vehicle back on the road. Returns false if there is no road.</summary>
        public bool ResetToRoad()
        {
            Transform t = Target;
            if (!RoadReset.TryComputePose(Source, t.position, t.forward, HasLastOnRoad, LastOnRoadPoint, Settings,
                    out Vector3 position, out Quaternion rotation, out RoadHit hit))
                return false;
            Place(position, rotation);
            HasLastOnRoad = true;
            LastOnRoadPoint = hit.Point;
            Placed?.Invoke(position, rotation);
            return true;
        }

        /// <summary>
        /// Stop-and-place order adapted from proven game vehicle code: zero velocities, move the
        /// transform, sync the body pose, zero velocities again.
        /// </summary>
        public void Place(Vector3 position, Quaternion rotation)
        {
            Rigidbody rb = body != null ? body : GetComponent<Rigidbody>();
            Transform t = Target;
            Stop(rb);
            t.SetPositionAndRotation(position, rotation);
            if (rb != null && rb.transform == t)
            {
                rb.position = position;
                rb.rotation = rotation;
            }
            Stop(rb);
        }

        private static void Stop(Rigidbody rb)
        {
            if (rb == null || rb.isKinematic) return;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}