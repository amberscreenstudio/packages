using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Navigation.Demo
{
    /// <summary>Moves a cube along a sample path and trims the UI ribbon behind it. No input required.</summary>
    public sealed class RibbonDemo : MonoBehaviour
    {
        public RouteRibbonUI uiRibbon;
        public RouteRibbonWorld worldRibbon;
        public Transform traveller;
        public Text helpLabel;
        public bool hudVisible = true;
        public bool showRemainingOnly = true;
        public float speed = 8f;
        private RoutePath path;
        private float progress;
        private bool previousHud;

        public RoutePath Path => path;

        public void UsePath(RoutePath shared)
        {
            path = shared;
            if (uiRibbon != null) uiRibbon.Path = path;
            if (worldRibbon != null) worldRibbon.Path = path;
        }

        private void OnEnable()
        {
            previousHud = NavigationHud.Visible;
            if (path == null)
            {
                path = new RoutePath();
                path.SetPoints(DefaultCurve());
            }
            if (uiRibbon != null) uiRibbon.Path = path;
            if (worldRibbon != null) worldRibbon.Path = path;
            progress = 0f;
            ApplyToggles();
        }

        private void OnDisable() => NavigationHud.Visible = previousHud;

        private void Update()
        {
            ApplyToggles();
            if (path == null || path.Length <= 1e-8f) return;
            progress += Mathf.Max(0f, Time.deltaTime) * speed;
            if (progress > path.Length) progress -= path.Length;
            path.SampleAtDistance(progress, out Vector3 position, out Vector3 direction);
            if (traveller != null)
            {
                traveller.position = position + Vector3.up * 0.5f;
                if (direction.sqrMagnitude > 1e-8f) traveller.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
            if (uiRibbon != null)
            {
                uiRibbon.ShowRemainingOnly = showRemainingOnly;
                uiRibbon.ProgressDistance = progress;
                uiRibbon.RenderNow();
            }
            if (worldRibbon != null)
            {
                worldRibbon.ShowRemainingOnly = showRemainingOnly;
                worldRibbon.ProgressDistance = progress;
                worldRibbon.RenderNow();
            }
        }

        private void ApplyToggles()
        {
            NavigationHud.Visible = hudVisible;
            if (helpLabel != null && string.IsNullOrEmpty(helpLabel.text) == false)
                helpLabel.enabled = true;
        }

        public static Vector3[] DefaultCurve()
        {
            const int n = 25;
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float z = t * 24f;
                float x = Mathf.Sin(t * Mathf.PI) * 8f;
                pts[i] = new Vector3(x, 0f, z);
            }
            return pts;
        }
    }
}
