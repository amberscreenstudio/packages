using AmberScreen.Navigation.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AmberScreen.Navigation.Editor
{
    public static class RoadResetDemoMenu
    {
        [MenuItem("Tools/AmberScreen/Navigation/Create Road Reset Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var roads = RoadResetDemo.CreateDefaultRoads();

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(7f, 1f, 7f);

            var colours = new[]
            {
                new Color(1f, 0.85f, 0.2f, 0.95f),
                new Color(0.3f, 0.75f, 1f, 0.95f),
                new Color(0.4f, 0.95f, 0.45f, 0.95f)
            };
            var lines = new RouteRibbonWorld[roads.Roads.Count];
            for (int i = 0; i < lines.Length; i++)
            {
                var host = new GameObject(roads.Roads[i].TwoWay ? $"Road {i + 1} (two-way)" : $"Road {i + 1} (one-way)");
                var line = host.AddComponent<RouteRibbonWorld>();
                line.HeightOffset = 0.03f;
                line.Width = 4f;
                line.Color = colours[i % colours.Length];
                line.Path = roads.Roads[i].Path;
                line.RenderNow();
                lines[i] = line;
            }

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Vehicle Box";
            box.transform.localScale = new Vector3(1.8f, 1f, 3.6f);
            box.transform.SetPositionAndRotation(new Vector3(-20f, 0.6f, -10f), Quaternion.identity);
            var body = box.AddComponent<Rigidbody>();
            body.mass = 1000f;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Front (cab)";
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(box.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.8f, 0.3f);
            nose.transform.localScale = new Vector3(0.9f, 0.6f, 0.25f);

            var resetter = box.AddComponent<RoadResetter>();
            resetter.Body = body;
            resetter.LiftHeight = 1f;
            resetter.OnRoadDistance = 3f;
            resetter.LastOnRoadRadius = 30f;

            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Last On-Road Point";
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.transform.localScale = Vector3.one * 0.6f;
            marker.SetActive(false);

            var demoObject = new GameObject("RoadResetDemo");
            var demo = demoObject.AddComponent<RoadResetDemo>();
            demo.resetter = resetter;
            demo.vehicle = body;
            demo.lastOnRoadMarker = marker.transform;
            demo.roadLines = lines;

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 42f, -34f);
                cam.transform.LookAt(new Vector3(0f, 0f, -2f));
            }

            Selection.activeGameObject = box;
            Debug.Log("Road reset demo scene created (unsaved). Press Play, then use the Push Off Road, Flip Over and Reset To Road buttons.");
        }
    }
}