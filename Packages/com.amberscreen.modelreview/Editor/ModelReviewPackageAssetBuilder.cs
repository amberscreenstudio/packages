using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AmberScreen.ModelReview.Editor
{
    /// <summary>
    /// Maintainer-only builder. Writes exclusively under Packages/com.amberscreen.modelreview/.
    /// </summary>
    public static class ModelReviewPackageAssetBuilder
    {
        const string PackageId = "com.amberscreen.modelreview";

        [MenuItem("Tools/Amber Screen/Model Review/Maintainer/Rebuild Package Assets")]
        public static void RebuildFromMenu()
        {
            if (!Guard(out string root))
                return;
            Build(root);
            Debug.Log("[ModelReview] Package assets rebuilt under " + root);
        }

        public static void BuildFromCommandLine()
        {
            if (!Guard(out string root))
            {
                Debug.LogError("[ModelReview] Builder guard failed.");
                EditorApplication.Exit(1);
                return;
            }

            try
            {
                Build(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[ModelReview] BuildFromCommandLine OK: " + root);
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        static bool Guard(out string packageRoot)
        {
            packageRoot = null;
            var info = PackageInfo.FindForAssembly(typeof(ModelReviewPackageAssetBuilder).Assembly);
            if (info == null || (info.source != PackageSource.Local && info.source != PackageSource.Embedded))
            {
                Debug.LogError("[ModelReview] Builder runs only on Local/Embedded package installs.");
                return false;
            }
            packageRoot = info.resolvedPath.Replace('\\', '/');
            if (!packageRoot.Contains("com.amberscreen.modelreview"))
            {
                Debug.LogError("[ModelReview] Unexpected package root: " + packageRoot);
                return false;
            }
            return true;
        }

        static void AssertPackagePath(string assetPath)
        {
            string p = assetPath.Replace('\\', '/');
            if (!p.StartsWith("Packages/com.amberscreen.modelreview/"))
                throw new System.InvalidOperationException("Write outside package: " + assetPath);
        }

        static void Build(string packageRoot)
        {
            EnsureFolder("Packages/com.amberscreen.modelreview/Rendering");
            EnsureFolder("Packages/com.amberscreen.modelreview/Rendering/Materials");

            var renderer = CreateOrReplaceRenderer(
                "Packages/com.amberscreen.modelreview/Rendering/ModelReview_Renderer.asset");
            CreateOrReplaceUrp(
                "Packages/com.amberscreen.modelreview/Rendering/ModelReview_URP.asset", renderer);
            CreateVolumeProfile("Packages/com.amberscreen.modelreview/Rendering/ModelReview_Volume.asset");
            CreateLightingSettings("Packages/com.amberscreen.modelreview/Rendering/ModelReview_Lighting.lighting");

            CreateLitMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_Clay.mat",
                new Color(0.5f, 0.5f, 0.5f), 0.25f);
            CreateLitMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_Floor.mat",
                new Color(0.42f, 0.42f, 0.42f), 0.1f);
            CreateLitMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_Capsule.mat",
                new Color(0.6f, 0.6f, 0.6f), 0.2f);
            CreateShaderMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_ClayWire.mat",
                "AmberScreen/ModelReview/ClayWire");
            CreateShaderMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_Grid.mat",
                "AmberScreen/ModelReview/Grid");
            CreateSkyMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_Sky.mat");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static UniversalRendererData CreateOrReplaceRenderer(string path)
        {
            AssertPackagePath(path);
            AssetDatabase.DeleteAsset(path);
            var data = ScriptableObject.CreateInstance<UniversalRendererData>();
            // Forward+ when available via SerializedObject
            var so = new SerializedObject(data);
            var mode = so.FindProperty("m_RenderingMode");
            if (mode != null)
            {
                // ForwardPlus = 2 in URP 17
                mode.intValue = 2;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        static UniversalRenderPipelineAsset CreateOrReplaceUrp(string path, UniversalRendererData renderer)
        {
            AssertPackagePath(path);
            AssetDatabase.DeleteAsset(path);
            var asset = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(asset, path);

            // Public setters where available; internal-set fields via SerializedObject on this package asset only.
            asset.mainLightShadowmapResolution = 4096;
            asset.additionalLightsShadowmapResolution = 4096;
            asset.shadowDistance = 20f;
            asset.shadowCascadeCount = 4;
            asset.shadowDepthBias = 1.0f;
            asset.shadowNormalBias = 0.4f;
            asset.msaaSampleCount = 4;
            asset.supportsHDR = true;
            asset.renderScale = 1f;
            asset.useSRPBatcher = true;

            var so = new SerializedObject(asset);
            SetBool(so, "m_MainLightShadowsSupported", true);
            SetInt(so, "m_AdditionalLightsRenderingMode", (int)LightRenderingMode.PerPixel);
            SetBool(so, "m_AdditionalLightShadowsSupported", true);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetInt(so, "m_SoftShadowQuality", (int)SoftShadowQuality.High);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            return asset;
        }

        static VolumeProfile CreateVolumeProfile(string path)
        {
            AssertPackagePath(path);
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void CreateLightingSettings(string path)
        {
            AssertPackagePath(path);
            AssetDatabase.DeleteAsset(path);
            var settings = new LightingSettings
            {
                bakedGI = false,
                realtimeGI = false,
                autoGenerate = false
            };
            AssetDatabase.CreateAsset(settings, path);
        }

        static Material CreateLitMat(string path, Color color, float smoothness)
        {
            AssertPackagePath(path);
            AssetDatabase.DeleteAsset(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static Material CreateShaderMat(string path, string shaderName)
        {
            AssertPackagePath(path);
            AssetDatabase.DeleteAsset(path);
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("[ModelReview] Shader missing at build time: " + shaderName);
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            var mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static Material CreateSkyMat(string path)
        {
            AssertPackagePath(path);
            AssetDatabase.DeleteAsset(path);
            var shader = Shader.Find("Skybox/Procedural");
            var mat = new Material(shader);
            if (mat.HasProperty("_SkyTint")) mat.SetColor("_SkyTint", new Color(0.55f, 0.55f, 0.58f));
            if (mat.HasProperty("_GroundColor")) mat.SetColor("_GroundColor", new Color(0.35f, 0.35f, 0.35f));
            if (mat.HasProperty("_SunSize")) mat.SetFloat("_SunSize", 0f);
            if (mat.HasProperty("_AtmosphereThickness")) mat.SetFloat("_AtmosphereThickness", 0.5f);
            if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 0.9f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void EnsureFolder(string assetPath)
        {
            AssertPackagePath(assetPath + "/_");
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string name = Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static void SetBool(SerializedObject so, string name, bool value)
        {
            var p = so.FindProperty(name);
            if (p != null) p.boolValue = value;
        }

        static void SetInt(SerializedObject so, string name, int value)
        {
            var p = so.FindProperty(name);
            if (p != null) p.intValue = value;
        }
    }
}
