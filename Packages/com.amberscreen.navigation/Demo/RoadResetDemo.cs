using UnityEngine;

namespace AmberScreen.Navigation.Demo
{
    /// <summary>
    /// Three sample roads, a rigidbody box and on-screen buttons to knock it off the road and
    /// reset it. Uses IMGUI buttons so it needs no EventSystem or input package.
    /// </summary>
    public sealed class RoadResetDemo : MonoBehaviour
    {
        public RoadResetter resetter;
        public Rigidbody vehicle;
        public Transform lastOnRoadMarker;
        public RouteRibbonWorld[] roadLines = new RouteRibbonWorld[0];
        public float pushSpeed = 9f;
        private RoadSourceList roads;
        private string status = "";

        public RoadSourceList Roads => roads;

        private void OnEnable()
        {
            roads = CreateDefaultRoads();
            if (resetter != null) resetter.Source = roads;
            for (int i = 0; i < roadLines.Length && i < roads.Roads.Count; i++)
                if (roadLines[i] != null) roadLines[i].Path = roads.Roads[i].Path;
        }

        private void Update()
        {
            if (lastOnRoadMarker != null && resetter != null)
            {
                lastOnRoadMarker.gameObject.SetActive(resetter.HasLastOnRoad);
                if (resetter.HasLastOnRoad) lastOnRoadMarker.position = resetter.LastOnRoadPoint + Vector3.up * 0.3f;
            }
        }

        public void PushOffRoad()
        {
            if (vehicle == null) return;
            Vector3 away = vehicle.transform.right;
            if (RoadReset.TryFindNearest(roads, vehicle.position, out RoadHit hit))
            {
                Vector3 d = vehicle.position - hit.Point;
                d.y = 0f;
                if (d.sqrMagnitude > 0.01f) away = d.normalized;
                else away = Vector3.Cross(Vector3.up, hit.Direction).normalized;
            }
            vehicle.AddForce(away * pushSpeed + Vector3.up * 3f, ForceMode.VelocityChange);
            vehicle.AddTorque(new Vector3(0f, 2.5f, 0f), ForceMode.VelocityChange);
            status = "Pushed off the road.";
        }

        public void FlipOver()
        {
            if (vehicle == null) return;
            vehicle.AddForce(Vector3.up * 6f, ForceMode.VelocityChange);
            vehicle.AddTorque(vehicle.transform.forward * 7f, ForceMode.VelocityChange);
            status = "Flipped.";
        }

        public void ResetNow()
        {
            if (resetter == null) return;
            status = resetter.ResetToRoad() ? "Reset to road." : "No road found.";
        }

        private void OnGUI()
        {
            float s = Mathf.Max(1f, Screen.height / 800f);
            var button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(18 * s) };
            var label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(16 * s) };
            label.normal.textColor = Color.white;
            float w = 220f * s, h = 44f * s, x = 20f * s, y = 20f * s, gap = 10f * s;

            if (GUI.Button(new Rect(x, y, w, h), "Push Off Road", button)) PushOffRoad();
            if (GUI.Button(new Rect(x, y + (h + gap), w, h), "Flip Over", button)) FlipOver();
            if (GUI.Button(new Rect(x, y + 2f * (h + gap), w, h), "Reset To Road", button)) ResetNow();

            bool onRoad = false;
            if (resetter != null && RoadReset.TryFindNearest(roads, resetter.Target.position, out RoadHit hit))
                onRoad = hit.Distance <= resetter.OnRoadDistance;
            string text = (onRoad ? "Box is on a road." : "Box is off the road.") +
                          (resetter != null && resetter.HasLastOnRoad ? "  Last on-road point: white ball." : "") +
                          (string.IsNullOrEmpty(status) ? "" : "\n" + status);
            GUI.Label(new Rect(x, y + 3f * (h + gap), 900f * s, 60f * s), text, label);
        }

        /// <summary>A straight two-way road, a curved one-way loop side and a diagonal two-way road.</summary>
        public static RoadSourceList CreateDefaultRoads()
        {
            var list = new RoadSourceList();
            list.Add(new[] { new Vector3(-20f, 0f, -20f), new Vector3(-20f, 0f, 20f) }, true);

            const int n = 17;
            var curve = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float a = Mathf.Lerp(-80f, 80f, t) * Mathf.Deg2Rad;
                curve[i] = new Vector3(Mathf.Sin(a) * 14f + 4f, 0f, Mathf.Cos(a) * 14f - 4f);
            }
            list.Add(curve, false);

            list.Add(new[] { new Vector3(-10f, 0f, -24f), new Vector3(24f, 0f, -8f) }, true);
            return list;
        }
    }
}