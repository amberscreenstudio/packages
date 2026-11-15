using AmberScreen.Dev.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AmberScreen.Dev.Editor
{
    /// <summary>Builds an unsaved scene with the 0.5.0 demo component.</summary>
    public static class DevConsoleDemoMenu
    {
        [MenuItem("Tools/AmberScreen/Dev Console/Open Parity Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject("DevConsoleParityDemo");
            go.AddComponent<DevConsoleParityDemo>();
            Selection.activeGameObject = go;
#if AMBERSCREEN_DEVCONSOLE
            Debug.Log("Dev Console demo scene created (unsaved). Press Play, then F1. Open the Demo section.");
#else
            Debug.LogWarning("Dev Console demo scene created, but AMBERSCREEN_DEVCONSOLE is not defined, so the console is off. " +
                             "Use Amber Screen > Dev Console > Add Scripting Define first.");
#endif
        }
    }
}
