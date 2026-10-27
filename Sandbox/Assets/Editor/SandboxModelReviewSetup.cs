using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AmberScreen.Sandbox.Editor
{
    /// <summary>Creates Sandbox/Assets/ModelReviewTest prop for modelreview package smoke tests.</summary>
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

            string prefabPath = Folder + "/ReviewTestProp.prefab";
            var root = new GameObject("ReviewTestProp");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube";
            cube.transform.SetParent(root.transform, false);
            cube.transform.position = new Vector3(-1.2f, 0.5f, 0f);
            cube.GetComponent<MeshRenderer>().sharedMaterial = lit;
            Object.DestroyImmediate(cube.GetComponent<Collider>());

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Sphere";
            sphere.transform.SetParent(root.transform, false);
            sphere.transform.position = new Vector3(0f, 0.5f, 0f);
            sphere.GetComponent<MeshRenderer>().sharedMaterial = legacy;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());

            var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = "Cylinder";
            cyl.transform.SetParent(root.transform, false);
            cyl.transform.position = new Vector3(1.2f, 0.5f, 0f);
            cyl.GetComponent<MeshRenderer>().sharedMaterial = null;
            Object.DestroyImmediate(cyl.GetComponent<Collider>());

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Sandbox] ModelReviewTest prop ready at " + prefabPath);
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

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
