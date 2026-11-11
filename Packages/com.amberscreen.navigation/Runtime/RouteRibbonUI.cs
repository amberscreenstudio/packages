using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Navigation
{
    /// <summary>
    /// Projects a RoutePath into this RectTransform. The game supplies world-XZ bounds.
    /// Draws the path only; no player pip, arrows or follow-dots.
    /// </summary>
    public sealed class RouteRibbonUI : MaskableGraphic
    {
        [SerializeField] private Vector2 worldMin = new Vector2(-50f, -50f);
        [SerializeField] private Vector2 worldMax = new Vector2(50f, 50f);
        [SerializeField] private float worldYawDeg;
        [SerializeField] private float width = 8f;
        [SerializeField] private bool showRemainingOnly;
        [SerializeField] private float progressDistance;
        private RoutePath path;

        public Vector2 WorldMin { get => worldMin; set { worldMin = value; SetVerticesDirty(); } }
        public Vector2 WorldMax { get => worldMax; set { worldMax = value; SetVerticesDirty(); } }
        public float WorldYawDeg { get => worldYawDeg; set { worldYawDeg = value; SetVerticesDirty(); } }
        public float Width { get => width; set { width = value; SetVerticesDirty(); } }
        public bool ShowRemainingOnly { get => showRemainingOnly; set { showRemainingOnly = value; SetVerticesDirty(); } }
        public float ProgressDistance { get => progressDistance; set { progressDistance = value; SetVerticesDirty(); } }

        public RoutePath Path
        {
            get => path;
            set
            {
                if (path == value) return;
                if (path != null) path.Changed -= OnPathChanged;
                path = value;
                if (path != null) path.Changed += OnPathChanged;
                SetVerticesDirty();
            }
        }

        private void OnDisable() => SetVerticesDirty();

        private void OnDestroy()
        {
            if (path != null) path.Changed -= OnPathChanged;
        }

        private void LateUpdate() => SetVerticesDirty();
        private void OnPathChanged() => SetVerticesDirty();

        /// <summary>EditMode hook: OnEnable is not invoked on plain components, so tests call this.</summary>
        public void RenderNow() => SetVerticesDirty();

        /// <summary>EditMode hook to populate a vertex helper without a canvas rebuild.</summary>
        public void FillMesh(VertexHelper vh) => OnPopulateMesh(vh);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!isActiveAndEnabled || !NavigationHud.Visible) return;
            if (path == null || path.Points.Count < 2 || path.Length <= 1e-8f) return;
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 1e-8f || rect.height <= 1e-8f) return;
            Vector2 size = worldMax - worldMin;
            if (Mathf.Abs(size.x) <= 1e-8f || Mathf.Abs(size.y) <= 1e-8f) return;

            var slice = RouteRibbonUtil.RemainingPoints(path, showRemainingOnly ? progressDistance : 0f, showRemainingOnly);
            if (slice.Count < 2) return;

            var ui = new List<Vector2>(slice.Count);
            for (int i = 0; i < slice.Count; i++)
                ui.Add(Project(slice[i], rect, size));

            float half = Mathf.Max(0.01f, width) * 0.5f;
            Color32 tint = color;
            for (int i = 0; i < ui.Count; i++)
            {
                Vector2 n = TangentNormal(ui, i);
                var v = UIVertex.simpleVert;
                v.color = tint;
                v.uv0 = Vector2.zero;
                v.position = new Vector3(ui[i].x + n.x * half, ui[i].y + n.y * half, 0f);
                vh.AddVert(v);
                v.position = new Vector3(ui[i].x - n.x * half, ui[i].y - n.y * half, 0f);
                vh.AddVert(v);
            }
            for (int i = 0; i < ui.Count - 1; i++)
            {
                int a = i * 2;
                vh.AddTriangle(a, a + 1, a + 2);
                vh.AddTriangle(a + 1, a + 3, a + 2);
            }
        }

        private Vector2 Project(Vector3 world, Rect rect, Vector2 size)
        {
            Vector2 center = (worldMin + worldMax) * 0.5f;
            Vector2 p = new Vector2(world.x, world.z) - center;
            if (worldYawDeg != 0f)
            {
                float rad = -worldYawDeg * Mathf.Deg2Rad;
                float c = Mathf.Cos(rad);
                float s = Mathf.Sin(rad);
                p = new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
            }
            float u = (p.x + size.x * 0.5f) / size.x;
            float v = (p.y + size.y * 0.5f) / size.y;
            return new Vector2(rect.x + u * rect.width, rect.y + v * rect.height);
        }

        private static Vector2 TangentNormal(List<Vector2> pts, int i)
        {
            Vector2 incoming = i > 0 ? pts[i] - pts[i - 1] : Vector2.zero;
            Vector2 outgoing = i < pts.Count - 1 ? pts[i + 1] - pts[i] : Vector2.zero;
            if (incoming.sqrMagnitude > 1e-12f) incoming.Normalize();
            else incoming = Vector2.zero;
            if (outgoing.sqrMagnitude > 1e-12f) outgoing.Normalize();
            else outgoing = Vector2.zero;
            Vector2 t = incoming + outgoing;
            if (t.sqrMagnitude < 1e-12f) t = incoming.sqrMagnitude > 1e-12f ? incoming : outgoing;
            if (t.sqrMagnitude < 1e-12f) t = new Vector2(1f, 0f);
            else t.Normalize();
            return new Vector2(-t.y, t.x);
        }
    }

    internal static class RouteRibbonUtil
    {
        public static List<Vector3> RemainingPoints(RoutePath path, float progress, bool remainingOnly)
        {
            var pts = path.Points;
            var result = new List<Vector3>(pts.Count + 1);
            if (!remainingOnly)
            {
                for (int i = 0; i < pts.Count; i++) result.Add(pts[i]);
                return result;
            }
            if (path.Length <= 1e-8f) return result;
            if (!IsFinite(progress)) progress = 0f;
            if (progress >= path.Length - 1e-6f) return result;
            if (progress <= 0f)
            {
                for (int i = 0; i < pts.Count; i++) result.Add(pts[i]);
                return result;
            }
            if (!path.SampleAtDistance(progress, out Vector3 start, out _)) return result;
            result.Add(start);
            var cum = path.CumulativeLengths;
            for (int i = 1; i < pts.Count; i++)
                if (cum[i] > progress + 1e-6f)
                    result.Add(pts[i]);
            return result;
        }

        private static bool IsFinite(float v) => !(float.IsNaN(v) || float.IsInfinity(v));
    }
}
