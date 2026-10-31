using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace AmberScreen.ModelReview.Editor
{
    /// <summary>
    /// Builds the ideal-conditions review studio into an untitled scene from package assets.
    /// Works for git, local and embedded installs — never opens a package scene path.
    /// </summary>
    public static class ModelReviewSceneBuilder
    {
        const string PackageRoot = "Packages/com.amberscreen.modelreview/";

        public struct ModelReviewPackageAssets
        {
            public UniversalRenderPipelineAsset Urp;
            public VolumeProfile VolumeProfile;
            public LightingSettings Lighting;
            public Material ClayMat;
            public Material ClayWireMat;
            public Material FloorMat;
            public Material GridMat;
            public Material CapsuleMat;
            public Material SkyMat;
        }

        internal static bool TryLoadPackageAssets(out ModelReviewPackageAssets a, out string error)
        {
            a = default;
            error = null;

            a.Urp = Load<UniversalRenderPipelineAsset>("Rendering/ModelReview_URP.asset");
            a.VolumeProfile = Load<VolumeProfile>("Rendering/ModelReview_Volume.asset");
            a.Lighting = Load<LightingSettings>("Rendering/ModelReview_Lighting.lighting");
            a.ClayMat = Load<Material>("Rendering/Materials/ModelReview_Clay.mat");
            a.ClayWireMat = Load<Material>("Rendering/Materials/ModelReview_ClayWire.mat");
            a.FloorMat = Load<Material>("Rendering/Materials/ModelReview_Floor.mat");
            a.GridMat = Load<Material>("Rendering/Materials/ModelReview_Grid.mat");
            a.CapsuleMat = Load<Material>("Rendering/Materials/ModelReview_Capsule.mat");
            a.SkyMat = Load<Material>("Rendering/Materials/ModelReview_Sky.mat");

            if (a.Urp == null) { error = "ModelReview_URP.asset missing"; return false; }
            if (a.VolumeProfile == null) { error = "ModelReview_Volume.asset missing"; return false; }
            if (a.Lighting == null) { error = "ModelReview_Lighting.lighting missing"; return false; }
            if (a.ClayMat == null) { error = "ModelReview_Clay.mat missing"; return false; }
            if (a.ClayWireMat == null) { error = "ModelReview_ClayWire.mat missing"; return false; }
            if (a.FloorMat == null) { error = "ModelReview_Floor.mat missing"; return false; }
            if (a.GridMat == null) { error = "ModelReview_Grid.mat missing"; return false; }
            if (a.CapsuleMat == null) { error = "ModelReview_Capsule.mat missing"; return false; }
            if (a.SkyMat == null) { error = "ModelReview_Sky.mat missing"; return false; }
            return true;
        }

        internal static ModelReviewRig BuildInto(Scene scene, ModelReviewPackageAssets a)
        {
            // Camera
            var camGo = new GameObject("ReviewCamera");
            SceneManager.MoveGameObjectToScene(camGo, scene);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.32f, 0.32f, 0.32f, 1f);
            cam.fieldOfView = ReviewOrbitCamera.DefaultFov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.useOcclusionCulling = true;
            var urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = false;
            camGo.transform.rotation = Quaternion.identity.normalized;
            var orbit = camGo.AddComponent<ReviewOrbitCamera>();
            orbit.BindCamera(cam);

            // Lights
            var key = CreateDirLight(scene, "KeyLight", new Vector3(50f, -30f, 0f), 1.1f, true);
            var fill = CreateDirLight(scene, "FillLight", new Vector3(20f, 150f, 0f), 0.35f, false);
            var rim = CreateDirLight(scene, "RimLight", new Vector3(25f, 200f, 0f), 0.6f, false);

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            SceneManager.MoveGameObjectToScene(floor, scene);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            var floorRenderer = floor.GetComponent<MeshRenderer>();
            floorRenderer.sharedMaterial = a.FloorMat;
            floorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            floorRenderer.receiveShadows = true;

            // Grid (off)
            var grid = GameObject.CreatePrimitive(PrimitiveType.Quad);
            SceneManager.MoveGameObjectToScene(grid, scene);
            grid.name = "Grid";
            grid.transform.rotation = Quaternion.Euler(90f, 0f, 0f).normalized;
            grid.transform.position = new Vector3(0f, 0.001f, 0f);
            grid.transform.localScale = new Vector3(20f, 20f, 1f);
            Object.DestroyImmediate(grid.GetComponent<Collider>());
            grid.GetComponent<MeshRenderer>().sharedMaterial = a.GridMat;
            grid.SetActive(false);

            // Capsule (off)
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            SceneManager.MoveGameObjectToScene(capsule, scene);
            capsule.name = "ScaleCapsule";
            capsule.transform.position = new Vector3(2f, 0.9f, 0f);
            capsule.transform.localScale = new Vector3(0.35f, 0.9f, 0.35f);
            Object.DestroyImmediate(capsule.GetComponent<Collider>());
            capsule.GetComponent<MeshRenderer>().sharedMaterial = a.CapsuleMat;
            capsule.SetActive(false);

            // Volume (disabled) — sharedProfile so T (tonemap) works; never read .profile (clones).
            var volGo = new GameObject("Volume");
            SceneManager.MoveGameObjectToScene(volGo, scene);
            var volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.sharedProfile = a.VolumeProfile;
            volume.enabled = false;

            // Probe
            var probeGo = new GameObject("ReflectionProbe");
            SceneManager.MoveGameObjectToScene(probeGo, scene);
            probeGo.transform.position = new Vector3(0f, 2f, 0f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.size = new Vector3(20f, 20f, 20f);
            probe.resolution = 256;
            probe.intensity = 1f;
            probe.hdr = true;
            probe.blendDistance = 1f;
            probe.boxProjection = false;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;

            // Render settings (EmptyScene defaults are wrong — set every value)
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.62f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.48f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.30f, 0.30f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.skybox = a.SkyMat;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.reflectionIntensity = 1f;
            RenderSettings.reflectionBounces = 1;
            RenderSettings.defaultReflectionResolution = 128;
            RenderSettings.fog = false;
            RenderSettings.sun = null;

            Lightmapping.lightingSettings = a.Lighting;

            // Rig + scene marker
            var rigGo = new GameObject("ModelReviewRig");
            SceneManager.MoveGameObjectToScene(rigGo, scene);
            rigGo.AddComponent<ModelReviewSceneMarker>();
            var rig = rigGo.AddComponent<ModelReviewRig>();
            // Re-resolve URP by path in case the struct field was invalidated after NewScene.
            var urp = a.Urp != null
                ? a.Urp
                : Load<UniversalRenderPipelineAsset>("Rendering/ModelReview_URP.asset");
            rig.WireReferences(
                orbit, key, fill, rim,
                floor, grid, capsule, volume, probe,
                a.ClayMat, a.ClayWireMat, urp);
            if (rig.ReviewUrpAsset == null)
                Debug.LogError("[ModelReview] Review URP asset failed to wire onto the rig.");
            return rig;
        }

        internal static bool TryClearSceneDirtiness(Scene scene, out bool foundApi)
        {
            foundApi = false;
            var mi = typeof(EditorSceneManager).GetMethod(
                "ClearSceneDirtiness",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new[] { typeof(Scene) },
                null);
            if (mi == null)
                return false;
            foundApi = true;
            mi.Invoke(null, new object[] { scene });
            return true;
        }

        static T Load<T>(string relative) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(PackageRoot + relative);

        static Light CreateDirLight(Scene scene, string name, Vector3 euler, float intensity, bool shadows)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.rotation = Quaternion.Euler(euler).normalized;
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = Color.white;
            light.colorTemperature = 6500f;
            light.useColorTemperature = true;
            if (shadows)
            {
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 1f;
                light.shadowBias = 0.05f;
                light.shadowNormalBias = 0.4f;
                light.shadowNearPlane = 0.2f;
            }
            else
            {
                light.shadows = LightShadows.None;
            }
            var data = light.GetUniversalAdditionalLightData();
            data.usePipelineSettings = true;
            if (shadows)
                data.softShadowQuality = SoftShadowQuality.High;
            return light;
        }
    }
}
