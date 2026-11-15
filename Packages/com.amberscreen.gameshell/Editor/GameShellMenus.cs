using System.IO;
using AmberScreen.GameShell.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AmberScreen.GameShell.Editor
{
    public static class GameShellMenus
    {
        const string SettingsPath = "Assets/Resources/" + GameShellSettings.ResourcesName + ".asset";

        [MenuItem("Tools/AmberScreen/Game Shell/Create Settings Asset")]
        public static void CreateSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameShellSettings>(SettingsPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                Debug.Log("Game shell settings already exist at " + SettingsPath);
                return;
            }
            Directory.CreateDirectory("Assets/Resources");
            var asset = ScriptableObject.CreateInstance<GameShellSettings>();
            AssetDatabase.CreateAsset(asset, SettingsPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            Debug.Log("Created " + SettingsPath);
        }

        [MenuItem("Tools/AmberScreen/Game Shell/Create Game Shell Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var spinner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spinner.name = "Spinner";
            spinner.transform.position = new Vector3(0f, 1f, 0f);
            var go = new GameObject("GameShellDemo");
            var demo = go.AddComponent<GameShellDemo>();
            demo.spinner = spinner.transform;
            Selection.activeGameObject = go;
            Debug.Log("Game shell demo scene created (unsaved). Press Play, then Esc.");
        }
    }
}
