using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>Immutable registration data. Null ShowDistance inherits the marker type default.</summary>
    public sealed class NavigationMarker
    {
        public string Id { get; }
        public Transform Target { get; }
        public Vector3 WorldPosition { get; }
        public string TypeId { get; }
        public string Label { get; }
        public bool? ShowDistance { get; }
        public int Priority { get; }
        private readonly bool usesTransform;

        public NavigationMarker(string id, Vector3 worldPosition, string typeId = "",
            string label = null, bool? showDistance = null, int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Marker id is required.", nameof(id));
            Id = id; WorldPosition = worldPosition; TypeId = typeId ?? "";
            Label = label; ShowDistance = showDistance; Priority = priority;
        }

        public NavigationMarker(string id, Transform target, string typeId = "",
            string label = null, bool? showDistance = null, int priority = 0)
            : this(id, Vector3.zero, typeId, label, showDistance, priority)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            Target = target; usesTransform = true;
        }

        // Destroyed transforms do not silently turn into world-origin markers.
        public bool TryGetPosition(out Vector3 position)
        {
            position = usesTransform && Target != null ? Target.position : WorldPosition;
            return !usesTransform || Target != null;
        }
    }

    /// <summary>Scene-owned, explicit opt-in only. No discovery logic or persistent host.</summary>
    public sealed class MarkerRegistry : MonoBehaviour
    {
        private readonly Dictionary<string, NavigationMarker> entries =
            new Dictionary<string, NavigationMarker>(StringComparer.Ordinal);
        public IReadOnlyCollection<NavigationMarker> Entries => entries.Values;
        public int Count => entries.Count;

        public bool Register(NavigationMarker marker)
        {
            if (marker == null) throw new ArgumentNullException(nameof(marker));
            if (entries.ContainsKey(marker.Id)) return false;
            entries.Add(marker.Id, marker);
            return true;
        }
        public void UpdateEntry(NavigationMarker marker)
        {
            if (marker == null) throw new ArgumentNullException(nameof(marker));
            if (!entries.ContainsKey(marker.Id)) throw new KeyNotFoundException(marker.Id);
            entries[marker.Id] = marker;
        }
        public bool Unregister(string id) => id != null && entries.Remove(id);
        public bool TryGet(string id, out NavigationMarker marker) => entries.TryGetValue(id, out marker);
        public void Clear() => entries.Clear();
    }
}
