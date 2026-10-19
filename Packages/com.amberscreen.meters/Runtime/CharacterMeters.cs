using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Meters
{
    /// <summary>
    /// Holds named <see cref="Meter"/> instances built from <see cref="MeterDefinition"/> assets.
    /// Does not decide drain/fill — gameplay controllers call Get(id) and Drain/Fill themselves.
    /// </summary>
    public sealed class CharacterMeters : MonoBehaviour
    {
        [SerializeField] List<MeterDefinition> definitions = new();

        readonly Dictionary<string, Meter> _meters = new(StringComparer.OrdinalIgnoreCase);
        readonly List<Meter> _ordered = new();

        public IReadOnlyList<Meter> All => _ordered;
        public int Count => _ordered.Count;

        void Awake()
        {
            // Skip if EnsureCharacterMeters/Register already ran; only build from serialized defs.
            if (_ordered.Count > 0) return;
            if (definitions != null && definitions.Count > 0)
                Rebuild();
        }

        /// <summary>Rebuild meters from the serialized definition list (clears runtime values).</summary>
        public void Rebuild()
        {
            _meters.Clear();
            _ordered.Clear();
            if (definitions == null) return;
            foreach (var def in definitions)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.Id))
                    continue;
                if (_meters.ContainsKey(def.Id))
                {
                    Debug.LogWarning($"[CharacterMeters] Duplicate meter id '{def.Id}' on {name}", this);
                    continue;
                }
                var meter = Meter.FromDefinition(def);
                _meters.Add(def.Id, meter);
                _ordered.Add(meter);
            }
        }

        public void SetDefinitions(IEnumerable<MeterDefinition> defs)
        {
            definitions = defs != null ? new List<MeterDefinition>(defs) : new List<MeterDefinition>();
            Rebuild();
        }

        /// <summary>Register a runtime meter (e.g. when definitions were loaded via Resources).</summary>
        public void Register(Meter meter)
        {
            if (meter == null || string.IsNullOrEmpty(meter.Id))
                return;
            if (_meters.TryGetValue(meter.Id, out var existing))
            {
                _ordered.Remove(existing);
                existing.ClearEvents();
            }
            _meters[meter.Id] = meter;
            _ordered.Add(meter);
        }

        public Meter Get(string id)
        {
            if (TryGet(id, out var meter))
                return meter;
            throw new KeyNotFoundException($"Meter '{id}' not found on {name}");
        }

        public bool TryGet(string id, out Meter meter)
        {
            if (string.IsNullOrEmpty(id))
            {
                meter = null;
                return false;
            }
            return _meters.TryGetValue(id, out meter);
        }

        public MeterState[] CaptureAll()
        {
            var states = new MeterState[_ordered.Count];
            for (int i = 0; i < _ordered.Count; i++)
                states[i] = _ordered[i].Capture();
            return states;
        }

        public void ApplyAll(IReadOnlyList<MeterState> states)
        {
            if (states == null) return;
            foreach (var state in states)
            {
                if (string.IsNullOrEmpty(state.Id)) continue;
                if (TryGet(state.Id, out var meter))
                    meter.Apply(state);
            }
        }

        /// <summary>Optional: tick all meters' passive rates. Controllers may ignore this.</summary>
        public void TickPassiveAll(float dt)
        {
            for (int i = 0; i < _ordered.Count; i++)
                _ordered[i].TickPassive(dt);
        }
    }
}
