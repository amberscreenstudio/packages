using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>Optional world LineRenderer for a RoutePath. The game supplies the material.</summary>
    public sealed class RouteRibbonWorld : MonoBehaviour
    {
        public Material lineMaterial;
        [SerializeField] private float heightOffset = 0.3f;
        [SerializeField] private float width = 1f;
        [SerializeField] private Color color = new Color(1f, 0.85f, 0.2f, 0.95f);
        [SerializeField] private bool showRemainingOnly;
        [SerializeField] private float progressDistance;
        private RoutePath path;
        private LineRenderer line;
        private Material fallbackMaterial;

        public float HeightOffset { get => heightOffset; set => heightOffset = value; }
        public float Width { get => width; set => width = value; }
        public Color Color { get => color; set => color = value; }
        public bool ShowRemainingOnly { get => showRemainingOnly; set => showRemainingOnly = value; }
        public float ProgressDistance { get => progressDistance; set => progressDistance = value; }
        public LineRenderer Line => line;

        public RoutePath Path
        {
            get => path;
            set
            {
                if (path == value) return;
                if (path != null) path.Changed -= OnPathChanged;
                path = value;
                if (path != null) path.Changed += OnPathChanged;
            }
        }

        private void LateUpdate() => RenderNow();
        private void OnPathChanged() => RenderNow();

        private void OnDisable()
        {
            // EditMode tests must also call RenderNow after enabled = false.
            HideLine();
        }

        private void OnDestroy()
        {
            if (path != null) path.Changed -= OnPathChanged;
            if (fallbackMaterial != null)
            {
                Object.DestroyImmediate(fallbackMaterial);
                fallbackMaterial = null;
            }
        }

        public void RenderNow()
        {
            EnsureLine();
            if (!isActiveAndEnabled || !NavigationHud.Visible || path == null || path.Points.Count < 2 || path.Length <= 1e-8f)
            {
                HideLine();
                return;
            }
            var slice = RouteRibbonUtil.RemainingPoints(path, showRemainingOnly ? progressDistance : 0f, showRemainingOnly);
            if (slice.Count < 2)
            {
                HideLine();
                return;
            }
            line.enabled = true;
            line.useWorldSpace = true;
            line.startWidth = line.endWidth = Mathf.Max(0.01f, width);
            line.startColor = line.endColor = color;
            ApplyMaterial();
            line.positionCount = slice.Count;
            for (int i = 0; i < slice.Count; i++)
            {
                Vector3 p = slice[i];
                p.y += heightOffset;
                line.SetPosition(i, p);
            }
        }

        private void EnsureLine()
        {
            if (line != null) return;
            line = GetComponent<LineRenderer>();
            if (line == null) line = gameObject.AddComponent<LineRenderer>();
        }

        private void ApplyMaterial()
        {
            if (lineMaterial != null)
            {
                line.sharedMaterial = lineMaterial;
                return;
            }
            if (fallbackMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                fallbackMaterial = shader != null ? new Material(shader) : null;
            }
            if (fallbackMaterial != null) line.sharedMaterial = fallbackMaterial;
        }

        private void HideLine()
        {
            if (line == null) return;
            line.positionCount = 0;
            line.enabled = false;
        }
    }
}
