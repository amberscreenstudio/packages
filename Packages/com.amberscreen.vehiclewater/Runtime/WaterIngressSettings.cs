using System;
using UnityEngine;

namespace AmberScreen.VehicleWater
{
    [Serializable]
    public sealed class WaterIngressSettings
    {
        [Tooltip("Water this many metres above the intake point stalls the engine.")]
        public float intakeStallDepth = 0.05f;
        [Tooltip("Water this many metres above the cabin point starts flooding the cabin.")]
        public float cabinFloodDepth = 0.02f;

        [Tooltip("Cabin flood gained per second at full rate (1 = completely full).")]
        public float floodPerSecond = 0.08f;
        [Tooltip("Metres of water over the cabin threshold for the full rate (0 = always full rate).")]
        public float fullRateDepth = 0.3f;
        [Tooltip("Cabin flood lost per second once out of the water.")]
        public float drainPerSecond = 0.02f;

        [Range(0f, 1f), Tooltip("Flood level that also stalls the engine, even with a dry intake.")]
        public float stallFloodLevel = 0.6f;
        [Range(0f, 1f), Tooltip("Flood level at which the vehicle is held stopped even out of deep water.")]
        public float stopFloodLevel = 0.3f;
        [Range(0f, 1f), Tooltip("Flood must drain to this level before a restart is allowed.")]
        public float drainedLevel = 0.001f;

        [Header("Stopping")]
        [Tooltip("Added to the rigidbody's linear damping while stopped by water.")]
        public float extraLinearDamping = 2f;
        [Tooltip("Added to the rigidbody's angular damping while stopped by water.")]
        public float extraAngularDamping = 1f;
        [Tooltip("Horizontal braking (metres per second, per second) while stopped by water.")]
        public float brakeDeceleration = 4f;

        public WaterIngressSettings Clone() => (WaterIngressSettings)MemberwiseClone();
    }
}
