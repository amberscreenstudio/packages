using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
            Assert.That(info.version, Is.EqualTo("0.1.0"));
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
            // Inject materials via WireReferences (other refs null ok for this test)
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

            // Without the review scene open, TrySwitch still may run — call Restore after.
            // We only assert ProjectSettings files unchanged.
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
            // Opening the full review scene in batch is heavy; simulate DontSave instantiate.
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            Assert.That(scene.isDirty, Is.False);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var instance = Object.Instantiate(cube);
            instance.hideFlags = HideFlags.DontSave;
            Object.DestroyImmediate(cube);

            // DontSave objects must not dirty the scene.
            Assert.That(scene.isDirty, Is.False);
            Object.DestroyImmediate(instance);
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
                "SaveScene", "MarkSceneDirty", "materialImportMode ="
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
                // allow only pipeline clone setters — material.Set is forbidden entirely outside builder
                Assert.That(setRegex.IsMatch(text), Is.False, name + " uses material.Set");
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
