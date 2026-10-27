using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using AmberScreen.ModelReview.Editor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Object = UnityEngine.Object;

namespace AmberScreen.ModelReview.Tests
{
    public sealed class ModelReviewTests
    {
        [Test]
        public void PackageInfo_NameVersionDependencies()
        {
            var info = PackageInfo.FindForAssembly(typeof(ModelReviewRig).Assembly);
            Assert.That(info, Is.Not.Null);
            Assert.That(info.name, Is.EqualTo("com.amberscreen.modelreview"));
            Assert.That(info.version, Is.EqualTo("0.1.1"));
            Assert.That(info.dependencies, Has.Some.Matches<UnityEditor.PackageManager.DependencyInfo>(
                d => d.name == "com.unity.render-pipelines.universal"));
            Assert.That(info.dependencies.Length, Is.EqualTo(1));
        }

        [Test]
        public void Report_CubeWithUrpLit_NoWarnings()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var result = ModelMaterialReport.Build(go);
            Assert.That(result.Renderers, Is.EqualTo(1));
            Assert.That(result.Slots, Is.EqualTo(1));
            Assert.That(result.Warnings, Is.EqualTo(0));
            Object.DestroyImmediate(mat);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Report_NullMaterial_Warns()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.GetComponent<MeshRenderer>().sharedMaterial = null;
            var result = ModelMaterialReport.Build(go);
            Assert.That(result.Warnings, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.Entries[0].Warning, Does.Contain("missing material"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Report_ErrorShader_Warns()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mat = new Material(Shader.Find("Hidden/InternalErrorShader"));
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var result = ModelMaterialReport.Build(go);
            Assert.That(result.Warnings, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.Entries[0].Warning, Does.Contain("non-URP"));
            Object.DestroyImmediate(mat);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Report_SummaryLine_Format()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.GetComponent<MeshRenderer>().sharedMaterial =
                new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var result = ModelMaterialReport.Build(go);
            Assert.That(result.SummaryLine, Does.Match(@"ModelReview report: \d+ renderers, \d+ slots, \d+ warnings\."));
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>().sharedMaterial);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ViewMode_ClayThenDelivered_RestoresExactSharedMaterials()
        {
            var root = new GameObject("Root");
            var a = GameObject.CreatePrimitive(PrimitiveType.Cube);
            a.transform.SetParent(root.transform);
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.transform.SetParent(root.transform);

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var m0 = new Material(lit) { name = "M0" };
            var m1 = new Material(lit) { name = "M1" };
            m0.SetColor("_BaseColor", Color.red);
            m1.SetColor("_BaseColor", Color.blue);
            var ra = a.GetComponent<MeshRenderer>();
            var rb = b.GetComponent<MeshRenderer>();
            ra.sharedMaterials = new[] { m0, null, m1 };
            rb.sharedMaterials = new[] { m0 };

            var id0 = m0.GetEntityId();
            var id1 = m1.GetEntityId();
            Color c0 = m0.GetColor("_BaseColor");
            Color c1 = m1.GetColor("_BaseColor");

            var clay = new Material(lit) { name = "Clay" };
            var wire = new Material(lit) { name = "Wire" };
            var rigGo = new GameObject("Rig");
            var rig = rigGo.AddComponent<ModelReviewRig>();
            rig.WireReferences(null, null, null, null, null, null, null, null, null, clay, wire, null);
            rig.SetTarget(root);

            rig.SetViewMode(ModelReviewViewMode.Clay);
            rig.SetViewMode(ModelReviewViewMode.Wireframe);
            rig.SetViewMode(ModelReviewViewMode.Delivered);

            Assert.That(ra.sharedMaterials.Length, Is.EqualTo(3));
            Assert.That(ra.sharedMaterials[0], Is.SameAs(m0));
            Assert.That(ra.sharedMaterials[1], Is.Null);
            Assert.That(ra.sharedMaterials[2], Is.SameAs(m1));
            Assert.That(rb.sharedMaterials[0], Is.SameAs(m0));
            Assert.That(m0.GetEntityId(), Is.EqualTo(id0));
            Assert.That(m1.GetEntityId(), Is.EqualTo(id1));
            Assert.That(m0.GetColor("_BaseColor"), Is.EqualTo(c0));
            Assert.That(m1.GetColor("_BaseColor"), Is.EqualTo(c1));

            Object.DestroyImmediate(rigGo);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(clay);
            Object.DestroyImmediate(wire);
            Object.DestroyImmediate(m0);
            Object.DestroyImmediate(m1);
        }

        [Test]
        public void ViewMode_Clay_CoversLegacyNullAndMultiMaterial_ThenRestoresExactReferences()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var legacyShader = Shader.Find("Standard") ?? Shader.Find("Hidden/InternalErrorShader");
            Assert.That(legacyShader, Is.Not.Null);

            var urpA = new Material(lit) { name = "UrpA" };
            urpA.SetColor("_BaseColor", Color.green);
            var legacyB = new Material(legacyShader) { name = "LegacyB" };
            var legacySolo = new Material(legacyShader) { name = "LegacySolo" };
            var clay = new Material(lit) { name = "Clay" };
            var wire = new Material(lit) { name = "Wire" };

            var root = new GameObject("Root");
            var legacyCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legacyCube.name = "Legacy";
            legacyCube.transform.SetParent(root.transform);
            legacyCube.GetComponent<MeshRenderer>().sharedMaterial = legacySolo;

            var nullCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nullCube.name = "NullSlot";
            nullCube.transform.SetParent(root.transform);
            nullCube.GetComponent<MeshRenderer>().sharedMaterial = null;

            var multiCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            multiCube.name = "Multi";
            multiCube.transform.SetParent(root.transform);
            multiCube.GetComponent<MeshRenderer>().sharedMaterials = new[] { urpA, null, legacyB };

            var skinnedGo = new GameObject("Skinned");
            skinnedGo.transform.SetParent(root.transform);
            var skinned = skinnedGo.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = legacyCube.GetComponent<MeshFilter>().sharedMesh;
            var skinnedMat = new Material(lit) { name = "SkinnedLit" };
            skinned.sharedMaterial = skinnedMat;

            var idUrp = urpA.GetEntityId();
            var idLegacy = legacyB.GetEntityId();
            var idSolo = legacySolo.GetEntityId();
            var idSkinned = skinnedMat.GetEntityId();
            Color baseUrp = urpA.GetColor("_BaseColor");
            EditorUtility.ClearDirty(urpA);
            EditorUtility.ClearDirty(legacyB);
            EditorUtility.ClearDirty(legacySolo);
            EditorUtility.ClearDirty(skinnedMat);

            var rigGo = new GameObject("Rig");
            var rig = rigGo.AddComponent<ModelReviewRig>();
            rig.WireReferences(null, null, null, null, null, null, null, null, null, clay, wire, null);
            rig.SetTarget(root);

            rig.SetViewMode(ModelReviewViewMode.Clay);
            AssertAllSlotsAre(root, clay);

            rig.SetViewMode(ModelReviewViewMode.Wireframe);
            Material expectedWire = SystemInfo.supportsGeometryShaders ? wire : clay;
            AssertAllSlotsAre(root, expectedWire);

            rig.SetViewMode(ModelReviewViewMode.Delivered);
            Assert.That(legacyCube.GetComponent<MeshRenderer>().sharedMaterials[0], Is.SameAs(legacySolo));
            Assert.That(nullCube.GetComponent<MeshRenderer>().sharedMaterials[0], Is.Null);
            var multi = multiCube.GetComponent<MeshRenderer>().sharedMaterials;
            Assert.That(multi.Length, Is.EqualTo(3));
            Assert.That(multi[0], Is.SameAs(urpA));
            Assert.That(multi[1], Is.Null);
            Assert.That(multi[2], Is.SameAs(legacyB));
            Assert.That(skinned.sharedMaterials[0], Is.SameAs(skinnedMat));

            Assert.That(urpA.GetEntityId(), Is.EqualTo(idUrp));
            Assert.That(legacyB.GetEntityId(), Is.EqualTo(idLegacy));
            Assert.That(legacySolo.GetEntityId(), Is.EqualTo(idSolo));
            Assert.That(skinnedMat.GetEntityId(), Is.EqualTo(idSkinned));
            Assert.That(urpA.shader, Is.EqualTo(lit));
            Assert.That(urpA.GetColor("_BaseColor"), Is.EqualTo(baseUrp));
            Assert.That(EditorUtility.IsDirty(urpA), Is.False);
            Assert.That(EditorUtility.IsDirty(legacyB), Is.False);
            Assert.That(EditorUtility.IsDirty(legacySolo), Is.False);
            Assert.That(EditorUtility.IsDirty(skinnedMat), Is.False);

            Object.DestroyImmediate(rigGo);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(clay);
            Object.DestroyImmediate(wire);
            Object.DestroyImmediate(urpA);
            Object.DestroyImmediate(legacyB);
            Object.DestroyImmediate(legacySolo);
            Object.DestroyImmediate(skinnedMat);
        }

        [Test]
        public void ViewMode_SourceAssetUntouched()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var srcMat = new Material(lit) { name = "Src" };
            srcMat.SetColor("_BaseColor", Color.cyan);
            var source = GameObject.CreatePrimitive(PrimitiveType.Cube);
            source.name = "Source";
            source.GetComponent<MeshRenderer>().sharedMaterial = srcMat;
            var srcArr = source.GetComponent<MeshRenderer>().sharedMaterials;
            var srcId = srcMat.GetEntityId();
            EditorUtility.ClearDirty(srcMat);
            EditorUtility.ClearDirty(source);

            var instance = Object.Instantiate(source);
            var clay = new Material(lit) { name = "Clay" };
            var wire = new Material(lit) { name = "Wire" };
            var rigGo = new GameObject("Rig");
            var rig = rigGo.AddComponent<ModelReviewRig>();
            rig.WireReferences(null, null, null, null, null, null, null, null, null, clay, wire, null);
            rig.SetTarget(instance);
            rig.SetViewMode(ModelReviewViewMode.Clay);
            rig.SetViewMode(ModelReviewViewMode.Delivered);

            Assert.That(source.GetComponent<MeshRenderer>().sharedMaterials[0], Is.SameAs(srcMat));
            Assert.That(source.GetComponent<MeshRenderer>().sharedMaterials.Length, Is.EqualTo(srcArr.Length));
            Assert.That(srcMat.GetEntityId(), Is.EqualTo(srcId));
            Assert.That(EditorUtility.IsDirty(srcMat), Is.False);
            Assert.That(EditorUtility.IsDirty(source), Is.False);

            string prefabPath = "Assets/ModelReviewTest/ReviewTestProp.prefab";
            Assume.That(File.Exists(prefabPath), "Sandbox ReviewTestProp.prefab not present");
            string[] hashPaths =
            {
                prefabPath,
                "Assets/ModelReviewTest/ReviewTest_Lit.mat",
                "Assets/ModelReviewTest/ReviewTest_Legacy.mat"
            };
            var before = hashPaths.Select(FileHash).ToArray();
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assume.That(asset, Is.Not.Null);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rig2Go = new GameObject("Rig2");
            var rig2 = rig2Go.AddComponent<ModelReviewRig>();
            rig2.WireReferences(null, null, null, null, null, null, null, null, null, clay, wire, null);
            ModelReviewTargetLoader.SpawnPreview(asset, scene, rig2);
            rig2.SetViewMode(ModelReviewViewMode.Clay);
            rig2.SetViewMode(ModelReviewViewMode.Wireframe);
            rig2.SetViewMode(ModelReviewViewMode.Delivered);
            ModelReviewTargetLoader.DestroyAllPreviews();
            Object.DestroyImmediate(rig2Go);
            for (int i = 0; i < hashPaths.Length; i++)
                Assert.That(FileHash(hashPaths[i]), Is.EqualTo(before[i]), hashPaths[i]);

            Object.DestroyImmediate(rigGo);
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(srcMat);
            Object.DestroyImmediate(clay);
            Object.DestroyImmediate(wire);
        }

        [Test]
        public void Loader_TryReloadTwice_LeavesExactlyOnePreview()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var clay = new Material(lit);
            var wire = new Material(lit);
            var asset = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var rigGo = new GameObject("Rig");
            var rig = rigGo.AddComponent<ModelReviewRig>();
            rig.WireReferences(null, null, null, null, null, null, null, null, null, clay, wire, null);

            ModelReviewTargetLoader.SpawnPreview(asset, scene, rig);
            ModelReviewTargetLoader.SpawnPreview(asset, scene, rig);
            Assert.That(ModelReviewTargetLoader.FindAllPreviewRoots().Count, Is.EqualTo(1));

            ModelReviewTargetLoader.DestroyAllPreviews();
            Object.DestroyImmediate(rigGo);
            Object.DestroyImmediate(asset);
            Object.DestroyImmediate(clay);
            Object.DestroyImmediate(wire);
        }

        [Test]
        public void Loader_DestroyAll_FindsDontSaveRoots()
        {
            ModelReviewTargetLoader.DestroyAllPreviews();

            var a = new GameObject(ModelReviewTargetLoader.TargetRootName);
            a.hideFlags = HideFlags.DontSave;
            a.AddComponent<ModelReviewPreviewMarker>();

            var b = new GameObject("OtherPreview");
            b.hideFlags = HideFlags.DontSave;
            b.AddComponent<ModelReviewPreviewMarker>();

            var legacy = new GameObject(ModelReviewTargetLoader.TargetRootName);
            legacy.hideFlags = HideFlags.DontSave;

            ModelReviewTargetLoader.DestroyAllPreviews();

            var markers = Resources.FindObjectsOfTypeAll<ModelReviewPreviewMarker>()
                .Where(m => m != null && !EditorUtility.IsPersistent(m))
                .ToArray();
            Assert.That(markers, Is.Empty);

            var leftovers = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(g => g != null
                            && !EditorUtility.IsPersistent(g)
                            && g.name == ModelReviewTargetLoader.TargetRootName)
                .ToArray();
            Assert.That(leftovers, Is.Empty);
        }

        [Test]
        public void OpenReviewClose_LeavesNoPreviewObjects()
        {
            // Drive the same destroy/spawn path without opening the package scene (heavy in batch).
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var clay = new Material(lit);
            var wire = new Material(lit);
            var asset = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var rigGo = new GameObject("Rig");
            var rig = rigGo.AddComponent<ModelReviewRig>();
            rig.WireReferences(null, null, null, null, null, null, null, null, null, clay, wire, null);

            ModelReviewTargetLoader.SpawnPreview(asset, scene, rig);
            Assert.That(ModelReviewTargetLoader.FindAllPreviewRoots().Count, Is.EqualTo(1));

            // Same order as CloseReviewScene: destroy previews, then leave.
            ModelReviewTargetLoader.DestroyAllPreviews();
            Object.DestroyImmediate(rigGo);

            var markers = Resources.FindObjectsOfTypeAll<ModelReviewPreviewMarker>()
                .Where(m => m != null && !EditorUtility.IsPersistent(m))
                .ToArray();
            Assert.That(markers, Is.Empty);
            var leftovers = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(g => g != null
                            && !EditorUtility.IsPersistent(g)
                            && g.name == ModelReviewTargetLoader.TargetRootName)
                .ToArray();
            Assert.That(leftovers, Is.Empty);

            Object.DestroyImmediate(asset);
            Object.DestroyImmediate(clay);
            Object.DestroyImmediate(wire);
        }

        [Test]
        public void Shaders_AllPackageShadersCompileAndAreSupported()
        {
            var info = PackageInfo.FindForAssembly(typeof(ModelReviewRig).Assembly);
            Assert.That(info, Is.Not.Null);
            string shaderDir = Path.Combine(info.resolvedPath, "Runtime", "Shaders");
            var shaders = Directory.GetFiles(shaderDir, "*.shader", SearchOption.AllDirectories);
            Assert.That(shaders.Length, Is.GreaterThan(0));

            // Force-compile package materials (import-time messages alone missed ShadowCaster issues).
            string matDir = Path.Combine(info.resolvedPath, "Rendering", "Materials");
            foreach (var matPath in Directory.GetFiles(matDir, "*.mat"))
            {
                string assetPath = "Packages/com.amberscreen.modelreview/Rendering/Materials/"
                                   + Path.GetFileName(matPath);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (mat == null || mat.shader == null) continue;
                int passes = mat.passCount;
                for (int p = 0; p < passes; p++)
                    ShaderUtil.CompilePass(mat, p, true);
            }

            foreach (var full in shaders)
            {
                string file = Path.GetFileName(full);
                string assetPath = "Packages/com.amberscreen.modelreview/Runtime/Shaders/" + file;
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
                Assert.That(shader, Is.Not.Null, assetPath);
                bool skipSupported = file.IndexOf("ClayWire", System.StringComparison.OrdinalIgnoreCase) >= 0
                                     && !SystemInfo.supportsGeometryShaders;
                if (!skipSupported)
                    Assert.That(shader.isSupported, Is.True, assetPath + " not supported");
                Assert.That(ShaderUtil.ShaderHasError(shader), Is.False, assetPath + " has error");
                var msgs = ShaderUtil.GetShaderMessages(shader);
                foreach (var m in msgs)
                    Assert.That(m.severity, Is.Not.EqualTo(ShaderCompilerMessageSeverity.Error),
                        assetPath + ": " + m.message);
            }
        }

        [Test]
        public void PipelineGuard_SwitchRestore_RestoresOriginal_AndProjectSettingsFilesUnchanged()
        {
            string gfxPath = "ProjectSettings/GraphicsSettings.asset";
            string qualPath = "ProjectSettings/QualitySettings.asset";
            string gfxBefore = FileHash(gfxPath);
            string qualBefore = FileHash(qualPath);

            var originalDefault = GraphicsSettings.defaultRenderPipeline;
            if (!ModelReviewPipelineGuard.CanSwitch(out _))
            {
                Assert.That(ModelReviewPipelineGuard.TrySwitch(), Is.False);
                Assert.That(FileHash(gfxPath), Is.EqualTo(gfxBefore));
                Assert.That(FileHash(qualPath), Is.EqualTo(qualBefore));
                Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(originalDefault));
                return;
            }

            bool switched = ModelReviewPipelineGuard.TrySwitch();
            if (switched)
            {
                Assert.That(ModelReviewPipelineGuard.ActiveClone, Is.Not.Null);
                ModelReviewPipelineGuard.Restore();
            }

            Assert.That(FileHash(gfxPath), Is.EqualTo(gfxBefore));
            Assert.That(FileHash(qualPath), Is.EqualTo(qualBefore));
            Assert.That(ReferenceEquals(GraphicsSettings.defaultRenderPipeline, originalDefault)
                        || (originalDefault == null && GraphicsSettings.defaultRenderPipeline == null),
                Is.True);
        }

        [Test]
        public void LoadTarget_DoesNotDirtyScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(scene.isDirty, Is.False);

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var clay = new Material(lit);
            var wire = new Material(lit);
            var asset = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var rigGo = new GameObject("Rig");
            var rig = rigGo.AddComponent<ModelReviewRig>();
            rig.WireReferences(null, null, null, null, null, null, null, null, null, clay, wire, null);

            bool dirtyBefore = scene.isDirty;
            ModelReviewTargetLoader.SpawnPreview(asset, scene, rig);
            Assert.That(scene.isDirty, Is.EqualTo(dirtyBefore),
                "SpawnPreview/MoveGameObjectToScene must not dirty the review scene");

            ModelReviewTargetLoader.DestroyAllPreviews();
            Object.DestroyImmediate(rigGo);
            Object.DestroyImmediate(asset);
            Object.DestroyImmediate(clay);
            Object.DestroyImmediate(wire);
        }

        [Test]
        public void Package_NeverWritesImportersOrForeignAssets()
        {
            var info = PackageInfo.FindForAssembly(typeof(ModelReviewRig).Assembly);
            Assert.That(info, Is.Not.Null);
            string root = info.resolvedPath;
            var forbidden = new[]
            {
                "SaveAndReimport", "ImportAsset", "AssetDatabase.SaveAssets", "SaveAssetIfDirty",
                "ForceReserializeAssets", "WriteImportSettingsIfDirty", "AddRemap", "RemoveRemap",
                "ExtractAsset", "CreateAsset", "DeleteAsset", "MoveAsset", "SetDirty",
                "ApplyModifiedProperties", "SaveAsPrefabAsset", "ApplyPrefabInstance",
                "SaveScene", "MarkSceneDirty", "materialImportMode =",
                "PrefabUtility.SavePrefabAsset", "Undo.RecordObject"
            };
            var materialsRegex = new Regex(@"\.materials?\b");
            var setRegex = new Regex(@"material\.Set");

            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (name == "ModelReviewPackageAssetBuilder.cs" || name == "ModelReviewTests.cs")
                    continue;
                string text = File.ReadAllText(file);
                foreach (var token in forbidden)
                    Assert.That(text.Contains(token), Is.False, name + " contains " + token);
                Assert.That(materialsRegex.IsMatch(text), Is.False, name + " uses .material(s)");
                Assert.That(setRegex.IsMatch(text), Is.False, name + " uses material.Set");
            }
        }

        static void AssertAllSlotsAre(GameObject root, Material expected)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
                var mats = r.sharedMaterials;
                Assert.That(mats.Length, Is.GreaterThan(0), r.name + " has no slots after override");
                foreach (var m in mats)
                    Assert.That(m, Is.SameAs(expected), r.name);
            }
        }

        static string FileHash(string relativePath)
        {
            string full = Path.GetFullPath(relativePath);
            if (!File.Exists(full)) return "";
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] bytes = File.ReadAllBytes(full);
            return System.BitConverter.ToString(sha.ComputeHash(bytes));
        }
    }
}
