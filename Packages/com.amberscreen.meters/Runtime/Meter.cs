using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Meters
{
    /// <summary>
    /// Game-free capacity meter (stamina, breath, coolant, etc.).
    /// Controllers call Drain/Fill/Add; this type never decides when.
    /// </summary>
    [Serializable]
    public class Meter
    {
        [SerializeField] string id;
        [SerializeField] float max = 100f;
        [SerializeField] float current = 100f;
        [SerializeField] float passiveDrainPerSecond;
        [SerializeField] float passiveFillPerSecond;
        [SerializeField] List<MeterThreshold> thresholds = new();

        readonly Dictionary<string, bool> _wasAtOrBelow = new(StringComparer.OrdinalIgnoreCase);
        bool _wasEmpty;
        bool _wasFull = true;

        public string Id => id;
        public float Max => max;
        public float Current => current;
        public float Normalized => max > 0.0001f ? Mathf.Clamp01(current / max) : 0f;
        public bool IsEmpty => current <= 0.0001f;
        public bool IsFull => current >= max - 0.0001f;
        public float PassiveDrainPerSecond
        {
            get => passiveDrainPerSecond;
            set => passiveDrainPerSecond = Mathf.Max(0f, value);
        }
        public float PassiveFillPerSecond
        {
            get => passiveFillPerSecond;
            set => passiveFillPerSecond = Mathf.Max(0f, value);
        }
        public IReadOnlyList<MeterThreshold> Thresholds => thresholds;

        /// <summary>Fired once when Current reaches 0 from above.</summary>
        public event Action BecameEmpty;
        /// <summary>Fired once when Current reaches Max from below.</summary>
        public event Action BecameFull;
        /// <summary>Fired once when Normalized crosses at-or-below a named threshold downward.</summary>
        public event Action<string> ThresholdCrossedDown;
        /// <summary>Fired once when Normalized crosses above a named threshold upward.</summary>
        public event Action<string> ThresholdCrossedUp;

        public Meter() { }

        public Meter(string meterId, float capacity, float startValue,
            float passiveDrain = 0f, float passiveFill = 0f,
            IEnumerable<MeterThreshold> thresholdList = null)
        {
            id = meterId;
            max = Mathf.Max(0.0001f, capacity);
            current = Mathf.Clamp(startValue, 0f, max);
            passiveDrainPerSecond = Mathf.Max(0f, passiveDrain);
            passiveFillPerSecond = Mathf.Max(0f, passiveFill);
            if (thresholdList != null)
                thresholds = new List<MeterThreshold>(thresholdList);
            SeedCrossingFlags();
        }

        public static Meter FromDefinition(MeterDefinition def)
        {
            if (def == null)
                throw new ArgumentNullException(nameof(def));
            return new Meter(
                def.Id,
                def.Max,
                def.StartValue,
                def.DefaultDrainPerSecond,
                def.DefaultFillPerSecond,
                def.Thresholds);
        }

        public void SetMax(float newMax, bool keepNormalized = false)
        {
            float n = Normalized;
            max = Mathf.Max(0.0001f, newMax);
            current = keepNormalized ? Mathf.Clamp(n * max, 0f, max) : Mathf.Clamp(current, 0f, max);
            EvaluateTransitions();
        }

        public void Set(float value)
        {
            current = Mathf.Clamp(value, 0f, max);
            EvaluateTransitions();
        }

        public void Add(float amount)
        {
            if (Mathf.Abs(amount) < 0.0000001f) return;
            current = Mathf.Clamp(current + amount, 0f, max);
            EvaluateTransitions();
        }

        public void Drain(float perSecond, float dt)
        {
            if (perSecond <= 0f || dt <= 0f) return;
            Add(-perSecond * dt);
        }

        public void Fill(float perSecond, float dt)
        {
            if (perSecond <= 0f || dt <= 0f) return;
            Add(perSecond * dt);
        }

        /// <summary>Applies optional passive drain then passive fill for this frame.</summary>
        public void TickPassive(float dt)
        {
            if (dt <= 0f) return;
            if (passiveDrainPerSecond > 0f)
                Drain(passiveDrainPerSecond, dt);
            if (passiveFillPerSecond > 0f)
                Fill(passiveFillPerSecond, dt);
        }

        public MeterState Capture() => new MeterState(id, current, max);

        public void Apply(MeterState state)
        {
            if (!string.IsNullOrEmpty(state.Id) &&
                !string.Equals(state.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[Meter] Apply id mismatch: meter={id} state={state.Id}");
            }
            if (state.Max > 0.0001f)
                max = state.Max;
            current = Mathf.Clamp(state.Current, 0f, max);
            SeedCrossingFlags();
        }

        public void ClearEvents()
        {
            BecameEmpty = null;
            BecameFull = null;
            ThresholdCrossedDown = null;
            ThresholdCrossedUp = null;
        }

        void SeedCrossingFlags()
        {
            _wasEmpty = IsEmpty;
            _wasFull = IsFull;
            _wasAtOrBelow.Clear();
            float n = Normalized;
            if (thresholds == null) return;
            foreach (var t in thresholds)
            {
                if (string.IsNullOrEmpty(t.Name)) continue;
                _wasAtOrBelow[t.Name] = n <= t.Normalized;
            }
        }

        void EvaluateTransitions()
        {
            bool empty = IsEmpty;
            bool full = IsFull;

            if (empty && !_wasEmpty)
                BecameEmpty?.Invoke();
            if (full && !_wasFull)
                BecameFull?.Invoke();

            _wasEmpty = empty;
            _wasFull = full;

            if (thresholds == null || thresholds.Count == 0)
                return;

            float n = Normalized;
            foreach (var t in thresholds)
            {
                if (string.IsNullOrEmpty(t.Name)) continue;
                bool nowBelow = n <= t.Normalized;
                _wasAtOrBelow.TryGetValue(t.Name, out bool wasBelow);
                if (nowBelow && !wasBelow)
                    ThresholdCrossedDown?.Invoke(t.Name);
                else if (!nowBelow && wasBelow)
                    ThresholdCrossedUp?.Invoke(t.Name);
                _wasAtOrBelow[t.Name] = nowBelow;
            }
        }
    }
}
