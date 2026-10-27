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

        public void RestoreAllFromCache()
        {
            foreach (var kv in _originalShared)
            {
                if (kv.Key == null) continue;
                kv.Key.sharedMaterials = kv.Value;
            }
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

        void OnDisable()
        {
            RestoreAllFromCache();
        }

        void OnDestroy()
        {
            RestoreAllFromCache();
            _originalShared.Clear();
            TargetInstance = null;
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            HandleHotkeys();
        }

        void OnGUI()
        {
            if (!_showLegend) return;
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                wordWrap = true,
                padding = new RectOffset(8, 8, 6, 6)
            };
            string pipe = _usingReviewPipeline ? "Review URP" : "HOST pipeline (not tuned)";
            string target = TargetInstance != null ? TargetInstance.name : "(none)";
            string body =
                "Mode: " + _mode + "\n" +
                "Target: " + target + "\n" +
                "Tris: " + CountTriangles() + "  Renderers: " + CountRenderers() + "\n" +
                "Shadow distance: " + CurrentShadowDistance.ToString("0.0") + "\n" +
                "Pipeline: " + pipe + "\n" +
                "Left-drag: orbit.\n" +
                "Right-drag or middle-drag (press the scroll wheel): pan.\n" +
                "Scroll: zoom.\n" +
                "F: frame.\n" +
                "1/2/3: Delivered/Clay/Wire.\n" +
                "I: fly inside (WASD/QE).\n" +
                "R: reset.\n" +
                "G: grid.\n" +
                "C: 1.8 m capsule.\n" +
                "T: neutral tonemap.\n" +
                "H: hide legend.";
            float height = style.CalcHeight(new GUIContent(body), 460f) + 12f;
            var prev = GUI.color;
            if (!_usingReviewPipeline) GUI.color = Color.yellow;
            GUI.Box(new Rect(8, 8, 470, height), body, style);
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
            foreach (var r in EnumeratePreviewRenderers(TargetInstance))
                CacheOne(r);
        }

        void CacheOne(Renderer r)
        {
            if (r == null || _originalShared.ContainsKey(r)) return;
            var shared = r.sharedMaterials;
            var copy = new Material[shared.Length];
            for (int i = 0; i < shared.Length; i++)
                copy[i] = shared[i];
            _originalShared[r] = copy;
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
            if (mat == null || TargetInstance == null) return;
            foreach (var r in EnumeratePreviewRenderers(TargetInstance))
            {
                if (r == null) continue;
                if (!_originalShared.ContainsKey(r))
                    CacheOne(r);
                if (!_originalShared.TryGetValue(r, out var original))
                    continue;

                int overrideLen = original.Length;
                if (overrideLen == 0)
                    overrideLen = Mathf.Max(1, GetSubMeshCount(r));

                var arr = new Material[overrideLen];
                for (int i = 0; i < arr.Length; i++)
                    arr[i] = mat;
                r.sharedMaterials = arr;
            }
        }

        static IEnumerable<Renderer> EnumeratePreviewRenderers(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                    yield return r;
            }
        }

        static int GetSubMeshCount(Renderer r)
        {
            if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                return smr.sharedMesh.subMeshCount;
            if (r is MeshRenderer && r.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
                return mf.sharedMesh.subMeshCount;
            return 0;
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
