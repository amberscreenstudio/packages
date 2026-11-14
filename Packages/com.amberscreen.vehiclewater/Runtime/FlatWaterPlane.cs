using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.VehicleWater
{
    /// <summary>Default water: a flat surface at this transform's height, everywhere or within an area.</summary>
    public sealed class FlatWaterPlane : MonoBehaviour, IWaterHeightProvider
    {
        static readonly List<FlatWaterPlane> s_Active = new List<FlatWaterPlane>();

        /// <summary>Enabled planes, oldest first. Used when a vehicle has no water source assigned.</summary>
        public static IReadOnlyList<FlatWaterPlane> Active => s_Active;

        [Tooltip("Added to this transform's Y.")]
        public float heightOffset;
        [Tooltip("Only count water inside a rectangle (local X/Z size, centred on this transform).")]
        public bool limitToArea;
        public Vector2 areaSize = new Vector2(100f, 100f);

        public float SurfaceHeight => transform.position.y + heightOffset;

        void OnEnable()
        {
            if (!s_Active.Contains(this)) s_Active.Add(this);
        }

        void OnDisable() => s_Active.Remove(this);

        public float GetWaterHeight(Vector3 worldPosition)
        {
            if (limitToArea)
            {
                Vector3 local = transform.InverseTransformPoint(worldPosition);
                Vector3 scale = transform.lossyScale;
                float halfX = areaSize.x * 0.5f / Mathf.Max(1e-4f, Mathf.Abs(scale.x));
                float halfZ = areaSize.y * 0.5f / Mathf.Max(1e-4f, Mathf.Abs(scale.z));
                if (Mathf.Abs(local.x) > halfX || Mathf.Abs(local.z) > halfZ) return float.NegativeInfinity;
            }
            return SurfaceHeight;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.6f);
            Vector3 c = new Vector3(transform.position.x, SurfaceHeight, transform.position.z);
            Vector2 size = limitToArea ? areaSize : new Vector2(20f, 20f);
            Gizmos.matrix = Matrix4x4.TRS(c, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0f, size.y));
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
