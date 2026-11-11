using AmberScreen.Navigation.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Navigation.Editor
{
    public static class RouteDemoMenu
    {
        [MenuItem("Tools/AmberScreen/Navigation/Create Route Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var demoObject = new GameObject("RouteDemo");
            var demo = demoObject.AddComponent<RouteDemo>();
            demo.registry = demoObject.AddComponent<MarkerRegistry>();
            var canvasObject = new GameObject("Route Demo HUD", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            scaler.matchWidthOrHeight = 0.5f;
            demo.nextStopLabel = CreateText(canvasObject.transform, "Next stop", -60f, 28);
            Text paceLabel = CreateText(canvasObject.transform, "Pace note", -130f, 32);
            demo.paceNotePresenter = paceLabel.gameObject.AddComponent<PaceNotePresenter>();
            demo.paceNotePresenter.label = paceLabel;
            Text help = CreateText(canvasObject.transform, "Instructions", -220f, 20);
            help.text = "Select RouteDemo in the Hierarchy. In the Inspector toggle Mags Present or Hud Visible.\nThe route repeats automatically; no driving controls needed.";
            Selection.activeGameObject = demoObject;
            Debug.Log("Route demo scene created (unsaved). Press Play, then toggle Mags Present in RouteDemo's Inspector.");
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
            text.fontSize = fontSize; text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white; text.raycastTarget = false; text.supportRichText = false;
            return text;
        }
    }
}
