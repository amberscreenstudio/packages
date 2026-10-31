using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using AmberScreen.ModelReview.Editor;
using Object = UnityEngine.Object;

namespace AmberScreen.ModelReview.Tests
{
    public sealed class ModelReviewBrowserTests
    {
        const string TempRoot = "Assets/ModelReviewBrowserTemp_013";
        string _folderA;
        string _folderB;
        readonly List<string> _createdPaths = new();

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
                AssetDatabase.DeleteAsset(TempRoot);
            AssetDatabase.CreateFolder("Assets", "ModelReviewBrowserTemp_013");
            _folderA = TempRoot + "/SetA";
            _folderB = TempRoot + "/SetB";
            AssetDatabase.CreateFolder(TempRoot, "SetA");
            AssetDatabase.CreateFolder(TempRoot, "SetB");
        }

        [TearDown]
        public void TearDown()
        {
            ModelReviewTargetLoader.DestroyAllPreviews();
            if (ModelReviewPipelineGuard.IsReviewSceneActive())
                ModelReviewMenus.CloseReviewScene();
            ModelReviewPipelineGuard.Restore();
            ModelReviewBrowser.SetList(System.Array.Empty<string>(), 0);
            if (AssetDatabase.IsValidFolder(TempRoot))
                AssetDatabase.DeleteAsset(TempRoot);
            _createdPaths.Clear();
            AssetDatabase.Refresh();
        }

        [Test]
        public void Browser_GatherFolder_FindsPrefabsAndModelsOnly_Sorted()
        {
            CreatePrefab(_folderA + "/C_Last.prefab", PrimitiveType.Cube);
            CreatePrefab(_folderA + "/A_First.prefab", PrimitiveType.Sphere);
            CreatePrefab(_folderA + "/B_Mid.prefab", PrimitiveType.Capsule);
            CreateMaterial(_folderA + "/Ignore.mat");
            CreateTexture(_folderA + "/Ignore.png");

            var guids = ModelReviewBrowser.GatherGuids(_folderA);
            Assert.That(guids.Count, Is.EqualTo(3));
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList();
            Assert.That(paths[0], Does.EndWith("A_First.prefab"));
            Assert.That(paths[1], Does.EndWith("B_Mid.prefab"));
            Assert.That(paths[2], Does.EndWith("C_Last.prefab"));
        }

        [Test]
        public void Browser_NextPrevious_WrapAround()
        {
            var guids = new List<string>
            {
                CreatePrefab(_folderA + "/P0.prefab", PrimitiveType.Cube),
                CreatePrefab(_folderA + "/P1.prefab", PrimitiveType.Sphere),
                CreatePrefab(_folderA + "/P2.prefab", PrimitiveType.Capsule)
            };
            ModelReviewBrowser.SetList(guids, 2);
            Assert.That(ModelReviewBrowser.Index, Is.EqualTo(2));
            ModelReviewBrowser.Next();
            Assert.That(ModelReviewBrowser.Index, Is.EqualTo(0));
            ModelReviewBrowser.Previous();
            Assert.That(ModelReviewBrowser.Index, Is.EqualTo(2));
        }

        [Test]
        public void Browser_Swap_LeavesExactlyOneTarget()
        {
            var guids = new List<string>
            {
                CreatePrefab(_folderA + "/S0.prefab", PrimitiveType.Cube),
                CreatePrefab(_folderA + "/S1.prefab", PrimitiveType.Sphere),
                CreatePrefab(_folderA + "/S2.prefab", PrimitiveType.Capsule)
            };
            OpenReviewWithList(guids, 0);
            for (int i = 0; i < 5; i++)
            {
                ModelReviewBrowser.Next();
                Assert.That(ModelReviewTargetLoader.FindAllPreviewRoots().Count, Is.EqualTo(1));
                var root = ModelReviewTargetLoader.FindAllPreviewRoots()[0];
                Assert.That(root.transform.childCount, Is.EqualTo(1));
                string expectPath = AssetDatabase.GUIDToAssetPath(ModelReviewBrowser.CurrentGuid);
                string expectName = Path.GetFileNameWithoutExtension(expectPath);
                Assert.That(root.transform.GetChild(0).name, Is.EqualTo(expectName));
            }
            ModelReviewMenus.CloseReviewScene();
        }

        [Test]
        public void Browser_Swap_SourcesUntouched()
        {
            string p0 = _folderA + "/U0.prefab";
            string p1 = _folderA + "/U1.prefab";
            var g0 = CreatePrefab(p0, PrimitiveType.Cube);
            var g1 = CreatePrefab(p1, PrimitiveType.Sphere);
            byte[] bytes0 = File.ReadAllBytes(Path.GetFullPath(p0));
            byte[] bytes1 = File.ReadAllBytes(Path.GetFullPath(p1));
            var asset0 = AssetDatabase.LoadAssetAtPath<GameObject>(p0);
            var mats0 = asset0.GetComponentInChildren<Renderer>().sharedMaterials.ToArray();

            OpenReviewWithList(new List<string> { g0, g1 }, 0);
            ModelReviewBrowser.Next();
            ModelReviewBrowser.Select(0);
            ModelReviewBrowser.Next();

            Assert.That(File.ReadAllBytes(Path.GetFullPath(p0)), Is.EqualTo(bytes0));
            Assert.That(File.ReadAllBytes(Path.GetFullPath(p1)), Is.EqualTo(bytes1));
            Assert.That(EditorUtility.IsDirty(asset0), Is.False);
            var matsAfter = asset0.GetComponentInChildren<Renderer>().sharedMaterials;
            Assert.That(matsAfter.Length, Is.EqualTo(mats0.Length));
            for (int i = 0; i < mats0.Length; i++)
                Assert.That(matsAfter[i], Is.SameAs(mats0[i]));
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.False);

            ModelReviewMenus.CloseReviewScene();
        }

        [Test]
        public void Browser_SessionState_SurvivesReload()
        {
            var guids = new List<string>
            {
                CreatePrefab(_folderA + "/R0.prefab", PrimitiveType.Cube),
                CreatePrefab(_folderA + "/R1.prefab", PrimitiveType.Sphere)
            };
            OpenReviewWithList(guids, 1);
            Assert.That(ModelReviewBrowser.Index, Is.EqualTo(1));
            ModelReviewTargetLoader.TryReload();
            Assert.That(ModelReviewBrowser.Count, Is.EqualTo(2));
            Assert.That(ModelReviewBrowser.Index, Is.EqualTo(1));
            Assert.That(ModelReviewBrowser.CurrentGuid, Is.EqualTo(guids[1]));
            ModelReviewMenus.CloseReviewScene();
        }

        [Test]
        public void Browser_FolderMenuValidator_OnlyFolders()
        {
            string prefabGuid = CreatePrefab(_folderA + "/V.prefab", PrimitiveType.Cube);
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(ModelReviewMenus.ValidateReviewFolder(), Is.False);

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(_folderA);
            Assert.That(ModelReviewMenus.ValidateReviewFolder(), Is.True);
        }

        [Test]
        public void Tone_Cycles_OffNeutralAces_AndAssetUntouched()
        {
            string volPath = "Packages/com.amberscreen.modelreview/Rendering/ModelReview_Volume.asset";
            byte[] before = File.ReadAllBytes(Path.GetFullPath(volPath));
            var volAsset = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volPath);
            Assert.That(EditorUtility.IsDirty(volAsset), Is.False);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ModelReviewMenus.OpenReviewScene();
            Assert.That(ModelReviewPipelineGuard.IsReviewSceneActive(), Is.True);
            var rig = Object.FindAnyObjectByType<ModelReviewRig>();
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.Tone, Is.EqualTo(ModelReviewTone.Off));

            rig.CycleTone();
            Assert.That(rig.Tone, Is.EqualTo(ModelReviewTone.Neutral));
            AssertTonemap(rig, TonemappingMode.Neutral);

            rig.CycleTone();
            Assert.That(rig.Tone, Is.EqualTo(ModelReviewTone.Aces));
            AssertTonemap(rig, TonemappingMode.ACES);

            rig.CycleTone();
            Assert.That(rig.Tone, Is.EqualTo(ModelReviewTone.Off));

            Assert.That(File.ReadAllBytes(Path.GetFullPath(volPath)), Is.EqualTo(before));
            Assert.That(EditorUtility.IsDirty(volAsset), Is.False);
            ModelReviewMenus.CloseReviewScene();
        }

        [Test]
        public void Legend_UsesPlainWords()
        {
            string text = ModelReviewRig.BuildLegend(
                "Model 1 / 2: Box",
                "Size 1.00 x 1.00 x 1.00 m · 12 triangles · 1 renderers",
                "Tone: ACES · View: Clay",
                "ACES",
                false,
                false);
            Assert.That(text, Does.Contain("previous/next model"));
            Assert.That(text, Does.Not.Contain("LMB"));
            Assert.That(text, Does.Not.Contain("RMB"));
            Assert.That(text, Does.Not.Contain("MMB"));
        }

        [Test]
        public void Triangles_CountSkinnedMeshes()
        {
            var root = new GameObject("SkinnedRoot");
            var child = new GameObject("Skin");
            child.transform.SetParent(root.transform);
            var smr = child.AddComponent<SkinnedMeshRenderer>();
            var mesh = new Mesh { name = "SkinMesh" };
            mesh.vertices = new[]
            {
                Vector3.zero, Vector3.right, Vector3.up, Vector3.forward
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            smr.sharedMesh = mesh;

            var rigGo = new GameObject("Rig");
            var rig = rigGo.AddComponent<ModelReviewRig>();
            rig.SetTarget(root);
            Assert.That(rig.CountTriangles(), Is.EqualTo(2));

            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(rigGo);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Zoom_LimitsScaleWithBounds()
        {
            var small = new Bounds(Vector3.zero, Vector3.one * 0.2f);
            ReviewOrbitCamera.LimitsForBounds(small, out float minS, out float maxS);
            Assert.That(minS, Is.EqualTo(0.05f * small.size.magnitude).Within(0.001f));
            Assert.That(maxS, Is.EqualTo(20f * small.size.magnitude).Within(0.001f));

            float d = ReviewOrbitCamera.ZoomedDistance(10f, 1f, 0.15f, 1f, 5f);
            Assert.That(d, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void Report_MaterialImportNone_Warns()
        {
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
            ModelImporter importer = null;
            string path = null;
            foreach (var g in modelGuids)
            {
                path = AssetDatabase.GUIDToAssetPath(g);
                importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null && importer.materialImportMode == ModelImporterMaterialImportMode.None)
                    break;
                importer = null;
            }

            Assume.That(importer != null,
                "No model with materialImportMode None in Assets; skipping");

            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assume.That(root != null);
            var result = ModelMaterialReport.Build(root, path);
            Assert.That(result.WarningMessages.Any(w =>
                w.IndexOf("materials not imported", System.StringComparison.OrdinalIgnoreCase) >= 0),
                Is.True);
        }

        static void AssertTonemap(ModelReviewRig rig, TonemappingMode mode)
        {
            var vol = Object.FindAnyObjectByType<Volume>();
            Assert.That(vol, Is.Not.Null);
            Assert.That(vol.enabled, Is.True);
            Assert.That(vol.sharedProfile, Is.Not.Null);
            Assert.That(vol.sharedProfile.TryGet(out Tonemapping tone), Is.True);
            Assert.That(tone.mode.value, Is.EqualTo(mode));
        }

        void OpenReviewWithList(List<string> guids, int index)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ModelReviewBrowser.SetList(guids, index);
            ModelReviewMenus.OpenReviewScene();
            Assert.That(ModelReviewPipelineGuard.IsReviewSceneActive(), Is.True,
                "OpenReviewScene did not activate a marker review scene");
        }

        string CreatePrefab(string path, PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            _createdPaths.Add(path);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.AssetPathToGUID(path);
        }

        void CreateMaterial(string path)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                   ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
            _createdPaths.Add(path);
        }

        void CreateTexture(string path)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            _createdPaths.Add(path);
        }
    }
}
