using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmberScreen.Save.Editor
{
    /// <summary>Small helpers: make the settings asset in the right place, open the save folder.</summary>
    public static class SaveMenus
    {
        const string AssetPath = "Assets/Resources/" + SaveSettings.ResourcesName + ".asset";

        [MenuItem("Tools/AmberScreen/Save/Create Save Settings Asset")]
        public static void CreateSettingsAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SaveSettings>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("[Save] Settings asset already exists: " + AssetPath);
                return;
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            var asset = ScriptableObject.CreateInstance<SaveSettings>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            SaveSettings.ClearCache();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log("[Save] Created " + AssetPath + " (used in the Editor and in builds).");
        }

        [MenuItem("Tools/AmberScreen/Save/Open Save Folder")]
        public static void OpenSaveFolder()
        {
            string folder = SaveStore.Folder;
            Directory.CreateDirectory(folder);
            EditorUtility.RevealInFinder(folder);
            Debug.Log("[Save] Save folder: " + folder);
        }
    }
}
