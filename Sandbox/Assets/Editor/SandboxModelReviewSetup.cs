using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AmberScreen.Sandbox.Editor
{
    /// <summary>
    /// Creates Sandbox/Assets/ModelReviewTest props for modelreview smoke tests:
    /// three separate prefabs so Browse shows exactly one model at a time.
    /// </summary>
    public static class SandboxModelReviewSetup
    {
        const string Folder = "Assets/ModelReviewTest";

        [MenuItem("AmberScreen/Create Model Review Test Prop")]
        public static void CreateTestProp()
        {
            EnsureFolder(Folder);

            string texPath = Folder + "/ReviewChecker.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
            {
                var tex = new Texture2D(256, 256, TextureFormat.RGBA32, true);
                for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                {
                    bool c = ((x / 32) + (y / 32)) % 2 == 0;
                    tex.SetPixel(x, y, c ? Color.white : Color.gray);
                }
                tex.Apply(true);
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            var checker = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            string litPath = Folder + "/ReviewTest_Lit.mat";
            var lit = AssetDatabase.LoadAssetAtPath<Material>(litPath);
            if (lit == null)
            {
                lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                lit.SetTexture("_BaseMap", checker);
                AssetDatabase.CreateAsset(lit, litPath);
            }

            string legacyPath = Folder + "/ReviewTest_Legacy.mat";
            var legacy = AssetDatabase.LoadAssetAtPath<Material>(legacyPath);
            if (legacy == null)
            {
                legacy = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(legacy, legacyPath);
            }

            // Remove the old 3-in-1 prefab so Browse Whole Project is one-model-at-a-time.
            const string oldCombined = Folder + "/ReviewTestProp.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(oldCombined) != null)
                AssetDatabase.DeleteAsset(oldCombined);

            WritePrimitivePrefab(Folder + "/ReviewTest_Cube.prefab", PrimitiveType.Cube, lit);
            WritePrimitivePrefab(Folder + "/ReviewTest_Sphere.prefab", PrimitiveType.Sphere, legacy);
            WritePrimitivePrefab(Folder + "/ReviewTest_Cylinder.prefab", PrimitiveType.Cylinder, null);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Sandbox] ModelReviewTest prefabs ready (Cube/Sphere/Cylinder) under " + Folder);
        }

        public static void CreateTestPropBatch()
        {
            try
            {
                CreateTestProp();
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        static void WritePrimitivePrefab(string path, PrimitiveType type, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = Path.GetFileNameWithoutExtension(path);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
