using UnityEngine;

namespace AmberScreen.VehicleWater.Demo
{
    /// <summary>Sandbox demo readout and buttons; also keeps the camera beside the car.</summary>
    public sealed class VehicleWaterDemo : MonoBehaviour
    {
        public VehicleWaterIngress ingress;
        public BoxCarEngine engine;
        public Transform cameraTransform;
        public Vector3 cameraOffset = new Vector3(9f, 1.5f, -1f);
        public Vector3 startPosition = new Vector3(0f, 2.6f, -6f);
        public bool gameAllowsRestart = true;

        GUIStyle m_Style;

        void OnEnable()
        {
            if (ingress != null) ingress.RestartAllowed = () => gameAllowsRestart;
        }

        void LateUpdate()
        {
            if (cameraTransform == null || ingress == null) return;
            Vector3 car = ingress.transform.position;
            cameraTransform.position = car + cameraOffset;
            cameraTransform.LookAt(car);
        }

        void OnGUI()
        {
            if (ingress == null || engine == null) return;
            if (m_Style == null) m_Style = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };

            GUILayout.BeginArea(new Rect(12, 12, 360, 430), GUI.skin.box);
            GUILayout.Label("<b>Vehicle water demo</b>", m_Style);
            GUILayout.Label(
                $"Water over intake: {DepthText(ingress.IntakeDepth)}\n" +
                $"Water over cabin floor: {DepthText(ingress.CabinDepth)}\n" +
                $"Cabin flooded: {ingress.Flood * 100f:0}%\n" +
                $"Engine: {(engine.IsEngineRunning ? "<color=#7f7>RUNNING</color>" : "<color=#f77>STALLED / OFF</color>")}\n" +
                $"Stopped by water: {(ingress.IsStoppedByWater ? "YES" : "no")}\n" +
                $"Engine may restart: {(ingress.CanRestart ? "<color=#7f7>YES</color>" : "no")}\n" +
                $"Speed: {(ingress.body != null ? ingress.body.linearVelocity.magnitude : 0f):0.0} m/s", m_Style);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Forward", GUILayout.Height(28))) engine.throttle = 1f;
            if (GUILayout.Button("Coast", GUILayout.Height(28))) engine.throttle = 0f;
            if (GUILayout.Button("Reverse", GUILayout.Height(28))) engine.throttle = -1f;
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Tow the car back to the top (stays wet)", GUILayout.Height(28))) Tow();

            GUI.enabled = ingress.CanRestart;
            if (GUILayout.Button("Start engine", GUILayout.Height(28))) ingress.TryRestart();
            GUI.enabled = true;

            if (GUILayout.Button("Game allows restart: " + (gameAllowsRestart ? "YES" : "NO"), GUILayout.Height(28)))
                gameAllowsRestart = !gameAllowsRestart;
            GUILayout.EndArea();
        }

        void Tow()
        {
            engine.throttle = 0f;
            var body = ingress.body;
            if (body == null) return;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = startPosition;
            body.rotation = Quaternion.identity;
            ingress.transform.SetPositionAndRotation(startPosition, Quaternion.identity);
        }

        static string DepthText(float depth)
        {
            if (float.IsNegativeInfinity(depth)) return "no water";
            return depth > 0f ? $"{depth:0.00} m (under water)" : $"{-depth:0.00} m above the surface";
        }
    }
}
