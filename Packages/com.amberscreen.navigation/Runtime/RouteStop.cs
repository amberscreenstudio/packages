using System;
using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>Navigation data only: no parcel, delivery outcome or game-state ownership.</summary>
    [Serializable]
    public sealed class RouteStop
    {
        [SerializeField] private string id;
        [SerializeField] private string label;
        [SerializeField] private Vector3 position;
        public string Id => id;
        public string Label => label;
        public Vector3 Position => position;

        public RouteStop(string id, string label, Vector3 position)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Stop id is required.", nameof(id));
            this.id = id; this.label = label ?? ""; this.position = position;
        }
    }
}
