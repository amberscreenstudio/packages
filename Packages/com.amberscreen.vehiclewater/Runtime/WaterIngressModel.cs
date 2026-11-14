using System;
using UnityEngine;

namespace AmberScreen.VehicleWater
{
    [Flags]
    public enum WaterIngressEvents
    {
        None = 0,
        Stalled = 1 << 0,
        FloodStarted = 1 << 1,
        Drained = 1 << 2,
        RestartAllowed = 1 << 3,
        StoppedByWater = 1 << 4,
        ReleasedByWater = 1 << 5
    }

    /// <summary>
    /// The rules, with no Unity objects: feed it the water depth at the intake and cabin points each step.
    /// Depth = water surface height minus the point's height (positive = under water).
    /// </summary>
    public sealed class WaterIngressModel
    {
        public WaterIngressSettings Settings { get; set; }

        /// <summary>0 = dry cabin, 1 = full.</summary>
        public float Flood { get; private set; }
        public bool IsStalled { get; private set; }
        public bool IntakeSubmerged { get; private set; }
        public bool CabinSubmerged { get; private set; }
        public bool IsStoppedByWater { get; private set; }
        /// <summary>Drained, intake dry, stalled by water, and the game allowed it.</summary>
        public bool CanRestart { get; private set; }

        public WaterIngressModel(WaterIngressSettings settings = null)
        {
            Settings = settings ?? new WaterIngressSettings();
        }

        /// <summary>Water depth over a point (positive = under water). No water = negative infinity.</summary>
        public static float Depth(float waterHeight, float pointHeight)
        {
            if (float.IsNaN(waterHeight) || float.IsNegativeInfinity(waterHeight)) return float.NegativeInfinity;
            return waterHeight - pointHeight;
        }

        /// <summary>Advances the rules by <paramref name="dt"/> seconds and returns what changed.</summary>
        public WaterIngressEvents Step(float intakeDepth, float cabinDepth, float dt, bool gameAllowsRestart = true)
        {
            var s = Settings;
            var events = WaterIngressEvents.None;
            dt = Mathf.Max(0f, dt);

            IntakeSubmerged = intakeDepth > s.intakeStallDepth;
            CabinSubmerged = cabinDepth > s.cabinFloodDepth;

            float before = Flood;
            if (CabinSubmerged)
            {
                float over = cabinDepth - s.cabinFloodDepth;
                float rate = s.fullRateDepth > 0f ? s.floodPerSecond * Mathf.Clamp01(over / s.fullRateDepth) : s.floodPerSecond;
                Flood = Mathf.Clamp01(Flood + rate * dt);
            }
            else
            {
                Flood = Mathf.Clamp01(Flood - s.drainPerSecond * dt);
            }
            if (before <= 0f && Flood > 0f) events |= WaterIngressEvents.FloodStarted;
            if (before > 0f && Flood <= 0f) events |= WaterIngressEvents.Drained;

            bool drowned = IntakeSubmerged || Flood >= s.stallFloodLevel;
            if (drowned && !IsStalled)
            {
                IsStalled = true;
                events |= WaterIngressEvents.Stalled;
            }

            bool canRestart = IsStalled && !drowned && !IntakeSubmerged && Flood <= s.drainedLevel && gameAllowsRestart;
            if (canRestart && !CanRestart) events |= WaterIngressEvents.RestartAllowed;
            CanRestart = canRestart;

            bool stopped = (IsStalled && (IntakeSubmerged || CabinSubmerged)) || Flood >= s.stopFloodLevel;
            if (stopped && !IsStoppedByWater) events |= WaterIngressEvents.StoppedByWater;
            if (!stopped && IsStoppedByWater) events |= WaterIngressEvents.ReleasedByWater;
            IsStoppedByWater = stopped;

            return events;
        }

        /// <summary>Call once the engine has been restarted. Returns false (and changes nothing) unless <see cref="CanRestart"/>.</summary>
        public bool NotifyRestarted()
        {
            if (!CanRestart) return false;
            IsStalled = false;
            CanRestart = false;
            return true;
        }

        /// <summary>Sets the flood level directly (loading a save, debug).</summary>
        public void SetFlood(float value) => Flood = Mathf.Clamp01(value);

        /// <summary>Dry, running, not stopped.</summary>
        public void Reset()
        {
            Flood = 0f;
            IsStalled = false;
            CanRestart = false;
            IntakeSubmerged = false;
            CabinSubmerged = false;
            IsStoppedByWater = false;
        }
    }
}
