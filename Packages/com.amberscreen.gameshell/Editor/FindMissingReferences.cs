using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmberScreen.GameShell.Editor
{
    /// <summary>
    /// Scans open scenes and all prefabs under Assets for missing scripts and
    /// object references that fail to resolve. Handy after moving code into a package.
    /// </summary>
    public static class FindMissingReferences
    {
        [MenuItem("Tools/AmberScreen/Game Shell/Find Missing References")]
        public static void FindMenu() => Find();

        /// <summary>Logs one warning per hit and returns the hits ("asset | path | component | property").</summary>
        public static List<string> Find()
        {
            var hits = new List<string>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                    ScanGameObject(root, scene.path, hits);
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ScanGameObject(root, path, hits);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            foreach (var hit in hits)
                Debug.LogWarning(hit);

            Debug.Log($"Missing references: {hits.Count}");
            return hits;
        }

        internal static void ScanGameObject(GameObject go, string assetPath, List<string> hits)
        {
            var components = go.GetComponentsInChildren<Component>(true);
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    hits.Add($"{assetPath} | {GetTransformPath(go.transform)} | <Missing Script>");
                    continue;
                }

                var so = new SerializedObject(comp);
                var prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference)
                        continue;
                    var fileId = prop.FindPropertyRelative("m_FileID");
                    bool pointsSomewhere = fileId != null && fileId.intValue != 0;
                    if (prop.objectReferenceValue != null || !pointsSomewhere) continue; // only null-but-pointing refs are missing

                    hits.Add(
                        $"{assetPath} | {GetTransformPath(comp.transform)} | {comp.GetType().Name} | {prop.propertyPath}");
                }
            }
        }

        static string GetTransformPath(Transform t)
        {
            var parts = new List<string>();
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
