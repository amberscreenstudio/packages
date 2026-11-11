using AmberScreen.Navigation.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Navigation.Editor
{
    public static class RibbonDemoMenu
    {
        [MenuItem("Tools/AmberScreen/Navigation/Create Route Ribbon Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var path = new RoutePath();
            path.SetPoints(RibbonDemo.DefaultCurve());

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, 0f, 12f);
            ground.transform.localScale = new Vector3(3f, 1f, 3.5f);

            var worldHost = new GameObject("World Ribbon");
            var worldRibbon = worldHost.AddComponent<RouteRibbonWorld>();
            worldRibbon.HeightOffset = 0.15f;
            worldRibbon.Width = 1.2f;
            worldRibbon.Path = path;
            worldRibbon.ShowRemainingOnly = true;
            worldRibbon.RenderNow();

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Traveller";
            cube.transform.localScale = new Vector3(0.8f, 0.8f, 1.2f);

            var canvasObject = new GameObject("Ribbon Demo HUD", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            scaler.matchWidthOrHeight = 0.5f;

            var panelGo = new GameObject("Ribbon Panel", typeof(RectTransform));
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.SetParent(canvasObject.transform, false);
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-40f, 0f);
            panelRect.sizeDelta = new Vector2(360f, 360f);
            var panelBg = panelGo.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.09f, 0.11f, 0.92f);
            panelBg.raycastTarget = false;

            var ribbonGo = new GameObject("UI Ribbon", typeof(RectTransform));
            var ribbonRect = (RectTransform)ribbonGo.transform;
            ribbonRect.SetParent(panelRect, false);
            ribbonRect.anchorMin = Vector2.zero;
            ribbonRect.anchorMax = Vector2.one;
            ribbonRect.offsetMin = new Vector2(16f, 16f);
            ribbonRect.offsetMax = new Vector2(-16f, -16f);
            var uiRibbon = ribbonGo.AddComponent<RouteRibbonUI>();
            uiRibbon.color = new Color(1f, 0.82f, 0.2f, 1f);
            uiRibbon.Width = 10f;
            uiRibbon.WorldMin = new Vector2(-10f, -2f);
            uiRibbon.WorldMax = new Vector2(10f, 26f);
            uiRibbon.raycastTarget = false;
            uiRibbon.Path = path;
            uiRibbon.ShowRemainingOnly = true;
            uiRibbon.RenderNow();

            var help = CreateText(canvasObject.transform, "Instructions", -40f, 20);
            help.text = "Select RibbonDemo in the Hierarchy. In the Inspector toggle Hud Visible or Show Remaining Only.\nThe cube travels the path on its own; the ribbons trim behind it when Show Remaining Only is on.";

            var demoObject = new GameObject("RibbonDemo");
            var demo = demoObject.AddComponent<RibbonDemo>();
            demo.uiRibbon = uiRibbon;
            demo.worldRibbon = worldRibbon;
            demo.traveller = cube.transform;
            demo.helpLabel = help;
            demo.hudVisible = true;
            demo.showRemainingOnly = true;
            demo.UsePath(path);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 18f, -6f);
                cam.transform.LookAt(new Vector3(0f, 0f, 10f));
            }

            Selection.activeGameObject = demoObject;
            Debug.Log("Route ribbon demo scene created (unsaved). Press Play, then toggle Hud Visible or Show Remaining Only on RibbonDemo.");
        }

        private static Text CreateText(Transform parent, string name, float y, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(1100f, 80f);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }
    }
}
