using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
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
            EnsureFolder("Packages/com.amberscreen.modelreview/Scenes");

            var renderer = CreateOrReplaceRenderer("Packages/com.amberscreen.modelreview/Rendering/ModelReview_Renderer.asset");
            var urp = CreateOrReplaceUrp("Packages/com.amberscreen.modelreview/Rendering/ModelReview_URP.asset", renderer);
            var volume = CreateVolumeProfile("Packages/com.amberscreen.modelreview/Rendering/ModelReview_Volume.asset");
            CreateLightingSettings("Packages/com.amberscreen.modelreview/Rendering/ModelReview_Lighting.lighting");

            var clay = CreateLitMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_Clay.mat",
                new Color(0.5f, 0.5f, 0.5f), 0.25f);
            var floor = CreateLitMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_Floor.mat",
                new Color(0.42f, 0.42f, 0.42f), 0.1f);
            var capsule = CreateLitMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_Capsule.mat",
                new Color(0.6f, 0.6f, 0.6f), 0.2f);
            var wire = CreateShaderMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_ClayWire.mat",
                "AmberScreen/ModelReview/ClayWire");
            var grid = CreateShaderMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_Grid.mat",
                "AmberScreen/ModelReview/Grid");
            var sky = CreateSkyMat("Packages/com.amberscreen.modelreview/Rendering/Materials/ModelReview_Sky.mat");

            BuildScene(
                "Packages/com.amberscreen.modelreview/Scenes/ModelReview.unity",
                urp, volume, clay, floor, capsule, wire, grid, sky);

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

        static void BuildScene(
            string scenePath,
            UniversalRenderPipelineAsset urp,
            VolumeProfile volumeProfile,
            Material clay,
            Material floorMat,
            Material capsuleMat,
            Material wireMat,
            Material gridMat,
            Material skyMat)
        {
            AssertPackagePath(scenePath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("ReviewCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.32f, 0.32f, 0.32f);
            cam.fieldOfView = ReviewOrbitCamera.DefaultFov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.allowMSAA = true;
            var urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = false;
            var orbit = camGo.AddComponent<ReviewOrbitCamera>();
            orbit.BindCamera(cam);

            // Lights
            var key = CreateDirLight("KeyLight", new Vector3(50f, -30f, 0f), 1.1f, true);
            var fill = CreateDirLight("FillLight", new Vector3(20f, 150f, 0f), 0.35f, false);
            var rim = CreateDirLight("RimLight", new Vector3(25f, 200f, 0f), 0.6f, false);

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2f, 1f, 2f); // 20x20 m
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // Grid (off)
            var grid = GameObject.CreatePrimitive(PrimitiveType.Quad);
            grid.name = "Grid";
            grid.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            grid.transform.position = new Vector3(0f, 0.001f, 0f);
            grid.transform.localScale = new Vector3(20f, 20f, 1f);
            Object.DestroyImmediate(grid.GetComponent<Collider>());
            grid.GetComponent<MeshRenderer>().sharedMaterial = gridMat;
            grid.SetActive(false);

            // Capsule (off)
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "ScaleCapsule";
            capsule.transform.position = new Vector3(2f, 0.9f, 0f);
            capsule.transform.localScale = new Vector3(0.35f, 0.9f, 0.35f); // ~1.8 m tall
            Object.DestroyImmediate(capsule.GetComponent<Collider>());
            capsule.GetComponent<MeshRenderer>().sharedMaterial = capsuleMat;
            capsule.SetActive(false);

            // Volume (disabled)
            var volGo = new GameObject("Volume");
            var volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = volumeProfile;
            volume.enabled = false;

            // Probe
            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.position = new Vector3(0f, 2f, 0f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.size = new Vector3(20f, 20f, 20f);
            probe.resolution = 256;
            probe.intensity = 1f;

            // Render settings
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.62f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.48f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.30f, 0.30f);
            RenderSettings.skybox = skyMat;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            // Lighting settings asset
            var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(
                "Packages/com.amberscreen.modelreview/Rendering/ModelReview_Lighting.lighting");
            if (lighting != null)
                Lightmapping.lightingSettings = lighting;

            // Rig
            var rigGo = new GameObject("ModelReviewRig");
            var rig = rigGo.AddComponent<ModelReviewRig>();
            rig.WireReferences(orbit, key, fill, rim, floor, grid, capsule, volume, probe, clay, wireMat, urp);

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        static Light CreateDirLight(string name, Vector3 euler, float intensity, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.rotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.colorTemperature = 6500f;
            light.useColorTemperature = true;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            var data = light.GetUniversalAdditionalLightData();
            data.usePipelineSettings = true;
            if (shadows)
                data.softShadowQuality = SoftShadowQuality.High;
            return light;
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
