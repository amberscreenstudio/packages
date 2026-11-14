using AmberScreen.Settings.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AmberScreen.Settings.Editor
{
    /// <summary>Builds an unsaved demo scene for the settings menu in one click.</summary>
    public static class SettingsDemoMenu
    {
        [MenuItem("Tools/AmberScreen/Settings/Create Settings Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("SettingsDemo");
            var menu = go.AddComponent<SettingsMenu>();
            var demo = go.AddComponent<SettingsDemo>();
            demo.menu = menu;

            Selection.activeGameObject = go;
            Debug.Log("Settings demo scene created (unsaved). Press Play, then use the buttons at the top left.");
        }
    }
}
