using AmberScreen.Inspect.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AmberScreen.Inspect.Editor
{
    /// <summary>Builds an unsaved demo scene: a table with three shapes that carry marked spots.</summary>
    public static class InspectDemoMenu
    {
        [MenuItem("Tools/AmberScreen/Inspect/Create Inspect Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Camera.main;
            if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" }.GetComponent<Camera>();
            cam.transform.position = new Vector3(0f, 1.7f, -2.6f);
            cam.transform.LookAt(new Vector3(0f, 0.9f, 0f));
            cam.nearClipPlane = 0.03f;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            Tint(floor, new Color(0.35f, 0.35f, 0.38f));

            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Table";
            table.transform.position = new Vector3(0f, 0.4f, 0f);
            table.transform.localScale = new Vector3(3f, 0.8f, 1.2f);
            Tint(table, new Color(0.45f, 0.32f, 0.22f));

            var box = Shape(PrimitiveType.Cube, "Box", new Vector3(-0.9f, 0.95f, 0f), Vector3.one * 0.3f,
                new Color(0.3f, 0.55f, 0.85f));
            box.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            Spot(box, "Serial number", new Vector3(0f, -0.2f, -0.51f), 0);
            Spot(box, "Hinge", new Vector3(0.51f, 0.3f, 0f), 1);
            Spot(box, "Scratch", new Vector3(-0.2f, 0.51f, 0.2f), 2);

            var canister = Shape(PrimitiveType.Cylinder, "Canister", new Vector3(0f, 1.0f, 0f), new Vector3(0.2f, 0.2f, 0.2f),
                new Color(0.8f, 0.3f, 0.3f));
            Spot(canister, "Label", new Vector3(0f, 0.3f, -0.51f), 0);
            Spot(canister, "Valve", new Vector3(0f, 1.02f, 0f), 1);

            var ball = Shape(PrimitiveType.Sphere, "Ball", new Vector3(0.9f, 0.93f, 0f), Vector3.one * 0.25f,
                new Color(0.4f, 0.75f, 0.4f));
            Spot(ball, "Dent", new Vector3(0.51f, 0.1f, 0f), 0);

            var viewer = cam.gameObject.AddComponent<InspectViewer>();
            viewer.viewCamera = cam;

            var demoGo = new GameObject("InspectDemo");
            demoGo.AddComponent<InspectDemo>().viewer = viewer;

            Selection.activeGameObject = demoGo;
            Debug.Log("Inspect demo scene created (unsaved). Press Play and click a shape on the table.");
        }

        static GameObject Shape(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            go.AddComponent<Inspectable>().displayName = name;
            Tint(go, color);
            return go;
        }

        static void Spot(GameObject owner, string label, Vector3 localPosition, int order)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Spot: " + label;
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.transform.SetParent(owner.transform, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = Vector3.one * 0.12f;
            Tint(marker, new Color(1f, 0.7f, 0.2f));
            var spot = marker.AddComponent<InspectHotspot>();
            spot.label = label;
            spot.order = order;
        }

        static void Tint(GameObject go, Color color)
        {
            go.AddComponent<DemoTint>().color = color;
        }
    }
}
