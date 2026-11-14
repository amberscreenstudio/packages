using UnityEngine;

namespace AmberScreen.Inspect.Demo
{
    /// <summary>Sandbox demo: click a shape to inspect it; shows the controls, the spot label and the events.</summary>
    public sealed class InspectDemo : MonoBehaviour
    {
        public InspectViewer viewer;

        string m_Label = "";
        bool m_PlayerControls = true;
        bool m_Background;
        GUIStyle m_Small, m_Big;

        void OnEnable()
        {
            if (viewer == null) return;
            viewer.onHotspotLabel.AddListener(OnLabel);
            viewer.onPlayerControlsEnabled.AddListener(OnControls);
            viewer.onBackgroundFocus.AddListener(OnBackground);
        }

        void OnDisable()
        {
            if (viewer == null) return;
            viewer.onHotspotLabel.RemoveListener(OnLabel);
            viewer.onPlayerControlsEnabled.RemoveListener(OnControls);
            viewer.onBackgroundFocus.RemoveListener(OnBackground);
        }

        void OnLabel(string label) => m_Label = label;
        void OnControls(bool on) => m_PlayerControls = on;
        void OnBackground(bool on) => m_Background = on;

        void Update()
        {
            if (viewer == null || viewer.IsInspecting) return;
            if (!InspectInput.PrimaryPressedThisFrame || !InspectInput.TryGetPointer(out var pointer)) return;
            var cam = viewer.viewCamera != null ? viewer.viewCamera : Camera.main;
            if (cam == null) return;
            if (Physics.Raycast(cam.ScreenPointToRay(pointer), out var hit, 100f))
            {
                var item = hit.collider.GetComponentInParent<Inspectable>();
                if (item != null) viewer.Inspect(item.transform);
            }
        }

        void OnGUI()
        {
            if (m_Small == null)
            {
                m_Small = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
                m_Big = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                m_Big.normal.textColor = new Color(1f, 0.75f, 0.3f);
            }

            GUILayout.BeginArea(new Rect(12, 12, 420, 210), GUI.skin.box);
            GUILayout.Label("<b>Inspect demo</b>", m_Small);
            if (viewer == null || !viewer.IsInspecting)
            {
                GUILayout.Label("Click a shape on the table to pick it up.", m_Small);
            }
            else
            {
                GUILayout.Label("Drag with the left mouse button: turn it\nMouse wheel: closer / further\n" +
                                "Tab / Shift+Tab: next / previous marked spot\nEsc or right-click: put it back\n" +
                                "Gamepad: right stick turn, triggers zoom, LB/RB spots, B put back", m_Small);
                GUILayout.Label($"Spots on this object: {viewer.Hotspots.Count}   Distance: {viewer.Distance:0.00} m", m_Small);
            }
            GUILayout.Label($"Player controls: {(m_PlayerControls ? "ON" : "OFF (inspecting)")}   Background focus event: {(m_Background ? "on" : "off")}", m_Small);
            GUILayout.EndArea();

            if (!string.IsNullOrEmpty(m_Label))
                GUI.Label(new Rect(0, Screen.height - 80, Screen.width, 50), m_Label, m_Big);
        }
    }
}
