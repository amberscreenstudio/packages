using System;
using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>Authored text, triggered by game-supplied route progress OR a horizontal world radius.</summary>
    [Serializable]
    public sealed class PaceNote
    {
        [SerializeField] private string text;
        [SerializeField] private bool usesWorldPosition;
        [SerializeField] private float routeDistance;
        [SerializeField] private Vector3 position;
        [SerializeField] private float radius;
        public string Text => text;
        public bool UsesWorldPosition => usesWorldPosition;
        public float RouteDistance => routeDistance;
        public Vector3 Position => position;
        public float Radius => radius;
        public PaceNote(string text, float routeDistance)
        {
            if (float.IsNaN(routeDistance) || float.IsInfinity(routeDistance) || routeDistance < 0f)
                throw new ArgumentOutOfRangeException(nameof(routeDistance));
            this.text = text ?? ""; this.routeDistance = routeDistance;
        }
        public PaceNote(string text, Vector3 position, float radius)
        {
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0f)
                throw new ArgumentOutOfRangeException(nameof(radius));
            this.text = text ?? ""; this.position = position; this.radius = radius; usesWorldPosition = true;
        }
    }
}
