using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AmberScreen.ModelReview
{
    /// <summary>
    /// Studio rig for ideal-conditions model review. View modes only swap
    /// <see cref="Renderer.sharedMaterials"/> arrays; materials themselves are never modified.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ModelReviewRig : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] ReviewOrbitCamera orbitCamera;

        [Header("Lights")]
        [SerializeField] Light keyLight;
        [SerializeField] Light fillLight;
        [SerializeField] Light rimLight;

        [Header("Studio")]
        [SerializeField] GameObject floor;
        [SerializeField] GameObject grid;
        [SerializeField] GameObject scaleCapsule;
        [SerializeField] Volume volume;
        [SerializeField] ReflectionProbe reflectionProbe;

        [Header("Materials (package only)")]
        [SerializeField] Material clayMaterial;
        [SerializeField] Material clayWireMaterial;

        [Header("Pipeline")]
        [SerializeField] UniversalRenderPipelineAsset reviewUrpAsset;

        [NonSerialized] public GameObject TargetInstance;

        ModelReviewViewMode _mode = ModelReviewViewMode.Delivered;
        readonly Dictionary<Renderer, Material[]> _originalShared = new();
        bool _keyOnly;
        bool _showGrid;
        bool _showCapsule;
        bool _neutralTonemap;
        bool _showLegend = true;
        bool _usingReviewPipeline = true;
        UniversalRenderPipelineAsset _runtimePipelineClone;
        static bool s_LoggedWireFallback;

        public ModelReviewViewMode ViewMode => _mode;
        public bool KeyOnly => _keyOnly;
        public bool ShowGrid => _showGrid;
        public bool ShowCapsule => _showCapsule;
        public bool NeutralTonemap => _neutralTonemap;
        public bool ShowLegend => _showLegend;
        public bool UsingReviewPipeline
        {
            get => _usingReviewPipeline;
            set => _usingReviewPipeline = value;
        }
        public UniversalRenderPipelineAsset ReviewUrpAsset => reviewUrpAsset;
        public UniversalRenderPipelineAsset RuntimePipelineClone => _runtimePipelineClone;
        public Material ClayMaterial => clayMaterial;
        public Material ClayWireMaterial => clayWireMaterial;
        public ReviewOrbitCamera Orbit => orbitCamera;

        public void WireReferences(
            ReviewOrbitCamera cam,
            Light key,
            Light fill,
            Light rim,
            GameObject floorGo,
            GameObject gridGo,
            GameObject capsuleGo,
            Volume vol,
            ReflectionProbe probe,
            Material clay,
            Material wire,
            UniversalRenderPipelineAsset urp)
        {
            orbitCamera = cam;
            keyLight = key;
            fillLight = fill;
            rimLight = rim;
            floor = floorGo;
            grid = gridGo;
            scaleCapsule = capsuleGo;
            volume = vol;
            reflectionProbe = probe;
            clayMaterial = clay;
            clayWireMaterial = wire;
            reviewUrpAsset = urp;
        }

        public void SetRuntimePipelineClone(UniversalRenderPipelineAsset clone) =>
            _runtimePipelineClone = clone;

        public void SetTarget(GameObject instance)
        {
            RestoreAllFromCache();
            _originalShared.Clear();
            TargetInstance = instance;
            CacheOriginals();
            ApplyCurrentMode();
            FrameAndAutoscale();
            RefreshProbe();
        }

        public void ClearTarget()
        {
            RestoreAllFromCache();
            _originalShared.Clear();
            TargetInstance = null;
        }

        public void SetViewMode(ModelReviewViewMode mode)
        {
            RestoreAllFromCache();
            _mode = mode;
            ApplyCurrentMode();
        }

        public void SetKeyOnly(bool on)
        {
            _keyOnly = on;
            if (fillLight != null) fillLight.enabled = !on;
            if (rimLight != null) rimLight.enabled = !on;
        }

        public void SetGridVisible(bool on)
        {
            _showGrid = on;
            if (grid != null) grid.SetActive(on);
        }

        public void SetCapsuleVisible(bool on)
        {
            _showCapsule = on;
            if (scaleCapsule != null) scaleCapsule.SetActive(on);
        }

        public void SetNeutralTonemap(bool on)
        {
            _neutralTonemap = on;
            if (volume != null) volume.enabled = on;
            var cam = orbitCamera != null ? orbitCamera.Cam : null;
            if (cam != null && cam.TryGetComponent(out UniversalAdditionalCameraData urpCam))
                urpCam.renderPostProcessing = on;
        }

        public void SetLegendVisible(bool on) => _showLegend = on;

        public void FrameAndAutoscale()
        {
            if (TargetInstance == null) return;
            var bounds = ComputeBounds(TargetInstance);
            orbitCamera?.Frame(bounds);
            AutoScaleShadows(bounds);
        }

        public void AutoScaleShadows(Bounds bounds)
        {
            float dist = Mathf.Clamp(3f * bounds.size.magnitude, 5f, 60f);
            var pipe = _runtimePipelineClone != null ? _runtimePipelineClone : reviewUrpAsset;
            if (pipe != null)
                pipe.shadowDistance = dist;
        }

        public void RefreshProbe()
        {
            if (reflectionProbe != null && reflectionProbe.enabled)
                reflectionProbe.RenderProbe();
        }

        public int CountTriangles()
        {
            int tris = 0;
            if (TargetInstance == null) return 0;
            foreach (var mf in TargetInstance.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
            foreach (var smr in TargetInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (smr.sharedMesh != null) tris += smr.sharedMesh.triangles.Length / 3;
            return tris;
        }

        public int CountRenderers() =>
            TargetInstance == null ? 0 : TargetInstance.GetComponentsInChildren<Renderer>(true).Length;

        public float CurrentShadowDistance
        {
            get
            {
                var pipe = _runtimePipelineClone != null ? _runtimePipelineClone : reviewUrpAsset;
                return pipe != null ? pipe.shadowDistance : 0f;
            }
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            HandleHotkeys();
        }

        void OnGUI()
        {
            if (!_showLegend) return;
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 12 };
            string pipe = _usingReviewPipeline ? "Review URP" : "HOST pipeline (not tuned)";
            string target = TargetInstance != null ? TargetInstance.name : "(none)";
            string body =
                "Mode: " + _mode + "\n" +
                "Target: " + target + "\n" +
                "Tris: " + CountTriangles() + "  Renderers: " + CountRenderers() + "\n" +
                "Shadow distance: " + CurrentShadowDistance.ToString("0.0") + "\n" +
                "Pipeline: " + pipe + "\n" +
                "1 Delivered  2 Clay  3 Wire  F Frame  I Interior  R Reset\n" +
                "G Grid  C Capsule  T Tonemap  H Legend";
            var prev = GUI.color;
            if (!_usingReviewPipeline) GUI.color = Color.yellow;
            GUI.Box(new Rect(8, 8, 420, 130), body, style);
            GUI.color = prev;
        }

        void HandleHotkeys()
        {
            if (ReviewInput.GetKeyDown(KeyCode.Alpha1)) SetViewMode(ModelReviewViewMode.Delivered);
            if (ReviewInput.GetKeyDown(KeyCode.Alpha2)) SetViewMode(ModelReviewViewMode.Clay);
            if (ReviewInput.GetKeyDown(KeyCode.Alpha3)) SetViewMode(ModelReviewViewMode.Wireframe);
            if (ReviewInput.GetKeyDown(KeyCode.F)) FrameAndAutoscale();
            if (ReviewInput.GetKeyDown(KeyCode.I)) orbitCamera?.ToggleInterior();
            if (ReviewInput.GetKeyDown(KeyCode.R))
            {
                orbitCamera?.ResetExterior();
                FrameAndAutoscale();
            }
            if (ReviewInput.GetKeyDown(KeyCode.G)) SetGridVisible(!_showGrid);
            if (ReviewInput.GetKeyDown(KeyCode.C)) SetCapsuleVisible(!_showCapsule);
            if (ReviewInput.GetKeyDown(KeyCode.T)) SetNeutralTonemap(!_neutralTonemap);
            if (ReviewInput.GetKeyDown(KeyCode.H)) SetLegendVisible(!_showLegend);
        }

        void CacheOriginals()
        {
            _originalShared.Clear();
            if (TargetInstance == null) return;
            foreach (var r in TargetInstance.GetComponentsInChildren<Renderer>(true))
            {
                var shared = r.sharedMaterials;
                var copy = new Material[shared.Length];
                for (int i = 0; i < shared.Length; i++)
                    copy[i] = shared[i];
                _originalShared[r] = copy;
            }
        }

        void RestoreAllFromCache()
        {
            foreach (var kv in _originalShared)
            {
                if (kv.Key == null) continue;
                kv.Key.sharedMaterials = kv.Value;
            }
        }

        void ApplyCurrentMode()
        {
            if (TargetInstance == null) return;
            switch (_mode)
            {
                case ModelReviewViewMode.Delivered:
                    RestoreAllFromCache();
                    break;
                case ModelReviewViewMode.Clay:
                    ApplyOverride(clayMaterial);
                    break;
                case ModelReviewViewMode.Wireframe:
                    ApplyWireframe();
                    break;
            }
        }

        void ApplyWireframe()
        {
            var shader = clayWireMaterial != null ? clayWireMaterial.shader : null;
            bool ok = SystemInfo.supportsGeometryShaders
                      && shader != null
                      && shader.isSupported;
            if (!ok)
            {
                if (!s_LoggedWireFallback)
                {
                    s_LoggedWireFallback = true;
                    Debug.LogWarning("[ModelReview] Wireframe needs geometry shaders; falling back to Clay.");
                }
                ApplyOverride(clayMaterial);
                return;
            }
            ApplyOverride(clayWireMaterial);
        }

        void ApplyOverride(Material mat)
        {
            if (mat == null) return;
            foreach (var kv in _originalShared)
            {
                if (kv.Key == null) continue;
                var arr = new Material[kv.Value.Length];
                for (int i = 0; i < arr.Length; i++)
                    arr[i] = mat;
                kv.Key.sharedMaterials = arr;
            }
        }

        public static Bounds ComputeBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}
