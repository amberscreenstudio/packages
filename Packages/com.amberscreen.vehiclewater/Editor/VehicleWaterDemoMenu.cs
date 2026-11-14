using AmberScreen.VehicleWater.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AmberScreen.VehicleWater.Editor
{
    /// <summary>Builds an unsaved demo scene: a box car on a ramp that drives into a flat water plane.</summary>
    public static class VehicleWaterDemoMenu
    {
        [MenuItem("Tools/AmberScreen/Vehicle Water/Create Vehicle Water Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Dry platform, top at y = 2, ending at z = 0.
            Block("Platform", new Vector3(0f, 1.5f, -6f), new Vector3(6f, 1f, 12f), Quaternion.identity, new Color(0.45f, 0.42f, 0.38f));

            // Ramp from (z 0, y 1.99) down to (z 20.5, y -2.6), 0.5 thick.
            Vector3 top = new Vector3(0f, 1.99f, 0f);
            Vector3 bottom = new Vector3(0f, -2.6f, 20.5f);
            Vector3 along = bottom - top;
            float angle = Mathf.Atan2(-along.y, along.z) * Mathf.Rad2Deg;
            var rampRotation = Quaternion.Euler(angle, 0f, 0f);
            Vector3 normal = rampRotation * Vector3.up;
            Block("Ramp", (top + bottom) * 0.5f - normal * 0.25f, new Vector3(6f, 0.5f, along.magnitude), rampRotation,
                new Color(0.5f, 0.45f, 0.38f));

            // Pool floor, top at y = -2.5, and an end wall.
            Block("PoolFloor", new Vector3(0f, -3f, 30f), new Vector3(6f, 1f, 20f), Quaternion.identity, new Color(0.35f, 0.33f, 0.3f));
            Block("EndWall", new Vector3(0f, -1f, 40.5f), new Vector3(6f, 4f, 1f), Quaternion.identity, new Color(0.35f, 0.33f, 0.3f));

            // Water: flat surface at y = 0 (the visual plane has no collider).
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.transform.position = new Vector3(0f, 0f, 27f);
            water.transform.localScale = new Vector3(0.6f, 1f, 3.6f);
            Tint(water, new Color(0.15f, 0.4f, 0.75f));
            water.AddComponent<FlatWaterPlane>();

            // Box car.
            var car = new GameObject("BoxCar");
            car.transform.position = new Vector3(0f, 2.6f, -6f);
            var body = car.AddComponent<Rigidbody>();
            body.mass = 1200f;
            body.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var collider = car.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.8f, 1f, 4f);

            var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shell.name = "Body";
            Object.DestroyImmediate(shell.GetComponent<Collider>());
            shell.transform.SetParent(car.transform, false);
            shell.transform.localScale = new Vector3(1.8f, 1f, 4f);
            Tint(shell, new Color(0.85f, 0.65f, 0.2f));

            var intake = Marker("Intake", car.transform, new Vector3(0f, 0.1f, 1.7f), new Color(1f, 0.35f, 0.2f));
            var cabin = Marker("CabinFloor", car.transform, new Vector3(0f, -0.3f, 0f), new Color(0.2f, 0.6f, 1f));

            var engine = car.AddComponent<BoxCarEngine>();
            engine.body = body;

            var ingress = car.AddComponent<VehicleWaterIngress>();
            ingress.body = body;
            ingress.intakePoint = intake;
            ingress.cabinPoint = cabin;
            ingress.engineSource = engine;
            ingress.waterSource = water.GetComponent<FlatWaterPlane>();
            ingress.settings.floodPerSecond = 0.2f;   // faster than the default so the demo is quick
            ingress.settings.drainPerSecond = 0.15f;

            var demo = new GameObject("VehicleWaterDemo").AddComponent<VehicleWaterDemo>();
            demo.ingress = ingress;
            demo.engine = engine;
            demo.startPosition = car.transform.position;
            if (Camera.main != null) demo.cameraTransform = Camera.main.transform;

            Selection.activeGameObject = car;
            Debug.Log("Vehicle water demo scene created (unsaved). Press Play: the box car drives down the ramp into the water.");
        }

        static void Block(string name, Vector3 position, Vector3 scale, Quaternion rotation, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            Tint(go, color);
        }

        static Transform Marker(string name, Transform parent, Vector3 localPosition, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * 0.25f;
            Tint(go, color);
            return go.transform;
        }

        static void Tint(GameObject go, Color color)
        {
            go.AddComponent<DemoTint>().color = color;
        }
    }
}
