using AmberScreen.Navigation.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Navigation.Editor
{
    /// <summary>Builds an unsaved demo scene so the compass can be seen in one click.</summary>
    public static class CompassDemoMenu
    {
        [MenuItem("Tools/AmberScreen/Navigation/Create Compass Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var body = new GameObject("Body");

            var navDemo = new GameObject("NavDemo");
            var heading = navDemo.AddComponent<HeadingSource>();
            var registry = navDemo.AddComponent<MarkerRegistry>();
            heading.ReferenceTransform = body.transform;
            var demo = navDemo.AddComponent<CompassDemo>();
            demo.heading = heading;
            demo.registry = registry;
            demo.spinningReference = body.transform;

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var strip = new GameObject("CompassStrip", typeof(RectTransform));
            var rect = (RectTransform)strip.transform;
            rect.SetParent(canvasGo.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -20f);
            rect.sizeDelta = new Vector2(640f, 110f);
            var presenter = strip.AddComponent<CompassStripPresenter>();
            presenter.headingSource = heading;
            presenter.registry = registry;
            presenter.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            presenter.fieldOfView = 120f;

            Selection.activeGameObject = navDemo;
            Debug.Log("Compass demo scene created (unsaved). Press Play to see the compass.");
        }
    }
}
