using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

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
        /// <summary>+1 next, -1 previous, or <see cref="int.MinValue"/> + index for a direct pick.</summary>
        public static event Action<int> BrowseRequested;

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
        [SerializeField] float waterlineHeight;

        [Header("Materials (package only)")]
        [SerializeField] Material clayMaterial;
        [SerializeField] Material clayWireMaterial;

        [Header("Pipeline")]
        [SerializeField] UniversalRenderPipelineAsset reviewUrpAsset;

        [NonSerialized] public GameObject TargetInstance;

        ReviewOrbitCamera _wiredOrbit;
        Light _wiredKey;
        Light _wiredFill;
        Light _wiredRim;
        GameObject _wiredFloor;
        GameObject _wiredGrid;
        GameObject _wiredCapsule;
        Volume _wiredVolume;
        ReflectionProbe _wiredProbe;
        Material _wiredClay;
        Material _wiredWire;
        UniversalRenderPipelineAsset _wiredUrp;

        ModelReviewViewMode _mode = ModelReviewViewMode.Delivered;
        readonly Dictionary<Renderer, Material[]> _originalShared = new();
        bool _keyOnly;
        bool _showGrid;
        bool _showCapsule;
        bool _showWaterline;
        ModelReviewTone _tone = ModelReviewTone.Off;
        bool _showLegend = true;
        bool _usingReviewPipeline = true;
        UniversalRenderPipelineAsset _runtimePipelineClone;
        VolumeProfile _runtimeVolumeProfile;
        Tonemapping _runtimeTonemap;
        ColorAdjustments _runtimeColorAdjust;
        GameObject _waterlinePlane;
        Material _waterlineMat;
        static bool s_LoggedWireFallback;

        readonly List<string> _browserNames = new();
        int _browserIndex = -1;
        string _browserFilter = "";
        Vector2 _browserScroll;
        int _searchControlId = -1;

#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
        InputActionMap _gamepadMap;
        InputAction _gpLook;
        InputAction _gpMove;
        InputAction _gpZoom;
        InputAction _gpReset;
        InputAction _gpFrame;
        InputAction _gpView;
        InputAction _gpInterior;
        InputAction _gpLegend;
        InputAction _gpPrev;
        InputAction _gpNext;
#endif

        public ModelReviewViewMode ViewMode => _mode;
        public ModelReviewTone Tone => _tone;
        public bool KeyOnly => _keyOnly;
        public bool ShowGrid => _showGrid;
        public bool ShowCapsule => _showCapsule;
        public bool ShowWaterline => _showWaterline;
        public float WaterlineHeight
        {
            get => waterlineHeight;
            set => waterlineHeight = value;
        }

        [Obsolete("Use Tone / SetTone / CycleTone.")]
        public bool NeutralTonemap => _tone == ModelReviewTone.Neutral;

        public bool ShowLegend => _showLegend;
        public bool UsingReviewPipeline
        {
            get => _usingReviewPipeline;
            set => _usingReviewPipeline = value;
        }
        public UniversalRenderPipelineAsset ReviewUrpAsset => _wiredUrp != null ? _wiredUrp : reviewUrpAsset;
        public UniversalRenderPipelineAsset RuntimePipelineClone => _runtimePipelineClone;
        public Material ClayMaterial => _wiredClay != null ? _wiredClay : clayMaterial;
        public Material ClayWireMaterial => _wiredWire != null ? _wiredWire : clayWireMaterial;
        public ReviewOrbitCamera Orbit => _wiredOrbit != null ? _wiredOrbit : orbitCamera;

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
            orbitCamera = _wiredOrbit = cam;
            keyLight = _wiredKey = key;
            fillLight = _wiredFill = fill;
            rimLight = _wiredRim = rim;
            floor = _wiredFloor = floorGo;
            grid = _wiredGrid = gridGo;
            scaleCapsule = _wiredCapsule = capsuleGo;
            volume = _wiredVolume = vol;
            reflectionProbe = _wiredProbe = probe;
            clayMaterial = _wiredClay = clay;
            clayWireMaterial = _wiredWire = wire;
            reviewUrpAsset = _wiredUrp = urp;
        }

        public void SetRuntimePipelineClone(UniversalRenderPipelineAsset clone) =>
            _runtimePipelineClone = clone;

        public void SetBrowserItems(IReadOnlyList<string> displayNames, int index)
        {
            _browserNames.Clear();
            if (displayNames != null)
            {
                for (int i = 0; i < displayNames.Count; i++)
                    _browserNames.Add(displayNames[i] ?? "");
            }
            _browserIndex = index;
        }

        public void SetTarget(GameObject instance)
        {
            RestoreAllFromCache();
            _originalShared.Clear();
            TargetInstance = instance;
            CacheOriginals();
            ApplyCurrentMode();
            FrameAndAutoscale();
            RefreshProbe();
            RefreshWaterlineDraft();
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

        public void CycleViewMode()
        {
            int next = ((int)_mode + 1) % 3;
            SetViewMode((ModelReviewViewMode)next);
        }

        public void SetKeyOnly(bool on)
        {
            _keyOnly = on;
            var fill = _wiredFill != null ? _wiredFill : fillLight;
            var rim = _wiredRim != null ? _wiredRim : rimLight;
            if (fill != null) fill.enabled = !on;
            if (rim != null) rim.enabled = !on;
        }

        public void SetGridVisible(bool on)
        {
            _showGrid = on;
            var g = _wiredGrid != null ? _wiredGrid : grid;
            if (g != null) g.SetActive(on);
        }

        public void SetCapsuleVisible(bool on)
        {
            _showCapsule = on;
            var c = _wiredCapsule != null ? _wiredCapsule : scaleCapsule;
            if (c != null) c.SetActive(on);
        }

        public void SetTone(ModelReviewTone tone)
        {
            _tone = tone;
            ApplyTone();
        }

        public void CycleTone()
        {
            _tone = _tone switch
            {
                ModelReviewTone.Off => ModelReviewTone.Neutral,
                ModelReviewTone.Neutral => ModelReviewTone.Aces,
                _ => ModelReviewTone.Off
            };
            ApplyTone();
        }

        [Obsolete("Use SetTone / CycleTone.")]
        public void SetNeutralTonemap(bool on) =>
            SetTone(on ? ModelReviewTone.Neutral : ModelReviewTone.Off);

        public void SetWaterlineVisible(bool on)
        {
            _showWaterline = on;
            EnsureWaterline();
            if (_waterlinePlane != null)
                _waterlinePlane.SetActive(on && !(Orbit != null && Orbit.InteriorMode));
        }

        public void SetLegendVisible(bool on) => _showLegend = on;

        public void FrameAndAutoscale()
        {
            if (TargetInstance == null) return;
            var bounds = ComputeBounds(TargetInstance);
            Orbit?.Frame(bounds);
            AutoScaleShadows(bounds);
        }

        public void AutoScaleShadows(Bounds bounds)
        {
            if (_runtimePipelineClone == null) return;
            float dist = Mathf.Clamp(3f * bounds.size.magnitude, 5f, 60f);
            _runtimePipelineClone.shadowDistance = dist;
        }

        public void RefreshProbe()
        {
            var probe = _wiredProbe != null ? _wiredProbe : reflectionProbe;
            if (probe != null && probe.enabled)
                probe.RenderProbe();
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

        public float DraftBelowWaterlineMetres
        {
            get
            {
                if (TargetInstance == null) return 0f;
                var b = ComputeBounds(TargetInstance);
                return waterlineHeight - b.min.y;
            }
        }

        /// <summary>Plain-word legend body used by OnGUI and tests.</summary>
        public static string BuildLegend(
            string modelLine,
            string sizeLine,
            string statusLine,
            string toneName,
            bool interior,
            bool hostPipeline)
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(modelLine);
            lines.AppendLine(sizeLine);
            lines.AppendLine(statusLine);
            if (hostPipeline)
                lines.AppendLine("HOST pipeline (not tuned)");
            lines.AppendLine("--");
            lines.AppendLine("Left/Right arrow keys (or [ and ]): previous/next model");
            lines.AppendLine("Drag with the left mouse button: orbit");
            lines.AppendLine("Drag with the right mouse button, or hold the scroll wheel and drag: pan");
            lines.AppendLine("Scroll wheel: zoom");
            lines.AppendLine("F: frame the model     R: reset camera");
            lines.AppendLine("1 / 2 / 3: delivered look / clay / wireframe");
            lines.AppendLine("I: fly inside (W A S D to move, Q E down/up)");
            lines.AppendLine(interior
                ? "G: grid   C: 1.8 m person capsule   (W moves forward while inside)"
                : "G: grid   C: 1.8 m person capsule   W: waterline");
            lines.AppendLine("T: tone (now: " + toneName + ")");
            lines.AppendLine("H: hide this help");
            lines.Append("Gamepad: right stick orbit, left stick pan, triggers zoom, shoulders previous/next");
            return lines.ToString();
        }

        void OnEnable()
        {
            EnsureGamepadActions();
        }

        void OnDisable()
        {
            RestoreAllFromCache();
            DisableGamepadActions();
        }

        void OnDestroy()
        {
            RestoreAllFromCache();
            _originalShared.Clear();
            TargetInstance = null;
            DisposeGamepadActions();
            if (_runtimeVolumeProfile != null)
            {
                SafeDestroy(_runtimeVolumeProfile);
                _runtimeVolumeProfile = null;
            }
            if (_waterlinePlane != null)
            {
                SafeDestroy(_waterlinePlane);
                _waterlinePlane = null;
            }
            if (_waterlineMat != null)
            {
                SafeDestroy(_waterlineMat);
                _waterlineMat = null;
            }
        }

        static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }

        void Update()
        {
            HandleBrowseHotkeys();
            if (Application.isPlaying)
            {
                HandleHotkeys();
                HandleGamepad();
            }
        }

        void OnGUI()
        {
            DrawBrowserPanel();
            if (!_showLegend) return;

            string pipe = _usingReviewPipeline ? "Review URP" : "HOST pipeline (not tuned)";
            string target = TargetInstance != null ? TargetInstance.name : "(none)";
            string modelLine = _browserNames.Count > 1
                ? "Model " + (_browserIndex + 1) + " / " + _browserNames.Count + ": " + target + "   (Pipeline: " + pipe + ")"
                : "Target: " + target + "   (Pipeline: " + pipe + ")";

            Bounds bounds = TargetInstance != null ? ComputeBounds(TargetInstance) : default;
            string sizeLine = TargetInstance != null
                ? string.Format(CultureInfo.InvariantCulture,
                    "Size {0:0.00} x {1:0.00} x {2:0.00} m · {3:n0} triangles · {4} renderers",
                    bounds.size.x, bounds.size.y, bounds.size.z, CountTriangles(), CountRenderers())
                : "Size —";

            string statusLine = "Tone: " + ToneName(_tone) + " · View: " + _mode;
            if (_showWaterline && TargetInstance != null && !(Orbit != null && Orbit.InteriorMode))
            {
                statusLine += string.Format(CultureInfo.InvariantCulture,
                    " · Draft: {0:0.00} m below waterline", DraftBelowWaterlineMetres);
            }

            string body = BuildLegend(
                modelLine,
                sizeLine,
                statusLine,
                ToneName(_tone),
                Orbit != null && Orbit.InteriorMode,
                !_usingReviewPipeline);

            if (TargetInstance != null)
            {
                string axisHint = UpAxisHint(bounds);
                if (!string.IsNullOrEmpty(axisHint))
                    body = axisHint + "\n" + body;
                string scaleWarn = ScaleWarning(bounds);
                if (!string.IsNullOrEmpty(scaleWarn))
                    body = scaleWarn + "\n" + body;
            }

            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                wordWrap = true,
                padding = new RectOffset(8, 8, 6, 6)
            };
            float height = style.CalcHeight(new GUIContent(body), 520f) + 12f;
            var prev = GUI.color;
            if (!_usingReviewPipeline) GUI.color = Color.yellow;
            GUI.Box(new Rect(8, 8, 530, height), body, style);
            GUI.color = prev;
        }

        void DrawBrowserPanel()
        {
            if (_browserNames.Count <= 1) return;
            float width = 300f;
            float x = Screen.width - width - 8f;
            var box = new Rect(x, 8f, width, Mathf.Min(Screen.height - 16f, 420f));
            GUILayout.BeginArea(box, GUI.skin.box);
            GUILayout.Label("Models  " + (_browserIndex + 1) + " / " + _browserNames.Count);
            GUI.SetNextControlName("ModelReviewBrowserSearch");
            _browserFilter = GUILayout.TextField(_browserFilter ?? "");
            if (GUI.GetNameOfFocusedControl() == "ModelReviewBrowserSearch")
                _searchControlId = GUIUtility.keyboardControl;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous")) BrowseRequested?.Invoke(-1);
            if (GUILayout.Button("Next")) BrowseRequested?.Invoke(1);
            GUILayout.EndHorizontal();

            _browserScroll = GUILayout.BeginScrollView(_browserScroll, GUILayout.ExpandHeight(true));
            string filter = (_browserFilter ?? "").Trim();
            for (int i = 0; i < _browserNames.Count; i++)
            {
                string name = _browserNames[i];
                if (filter.Length > 0
                    && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                bool selected = i == _browserIndex;
                var style = selected ? GUI.skin.button : GUI.skin.label;
                if (GUILayout.Button(name, style))
                    BrowseRequested?.Invoke(int.MinValue + i);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        bool SearchFieldFocused =>
            GUIUtility.keyboardControl != 0
            && GUIUtility.keyboardControl == _searchControlId;

        void HandleBrowseHotkeys()
        {
            if (SearchFieldFocused) return;
            if (_browserNames.Count <= 1) return;
            if (ReviewInput.GetKeyDown(KeyCode.LeftArrow)
                || ReviewInput.GetKeyDown(KeyCode.LeftBracket)
                || ReviewInput.GetKeyDown(KeyCode.PageUp))
                BrowseRequested?.Invoke(-1);
            if (ReviewInput.GetKeyDown(KeyCode.RightArrow)
                || ReviewInput.GetKeyDown(KeyCode.RightBracket)
                || ReviewInput.GetKeyDown(KeyCode.PageDown))
                BrowseRequested?.Invoke(1);
        }

        void HandleHotkeys()
        {
            if (SearchFieldFocused) return;
            if (ReviewInput.GetKeyDown(KeyCode.Alpha1)) SetViewMode(ModelReviewViewMode.Delivered);
            if (ReviewInput.GetKeyDown(KeyCode.Alpha2)) SetViewMode(ModelReviewViewMode.Clay);
            if (ReviewInput.GetKeyDown(KeyCode.Alpha3)) SetViewMode(ModelReviewViewMode.Wireframe);
            if (ReviewInput.GetKeyDown(KeyCode.F)) FrameAndAutoscale();
            if (ReviewInput.GetKeyDown(KeyCode.I)) Orbit?.ToggleInterior();
            if (ReviewInput.GetKeyDown(KeyCode.R))
            {
                Orbit?.ResetExterior();
                FrameAndAutoscale();
            }
            if (ReviewInput.GetKeyDown(KeyCode.G)) SetGridVisible(!_showGrid);
            if (ReviewInput.GetKeyDown(KeyCode.C)) SetCapsuleVisible(!_showCapsule);
            if (ReviewInput.GetKeyDown(KeyCode.T)) CycleTone();
            if (ReviewInput.GetKeyDown(KeyCode.H)) SetLegendVisible(!_showLegend);
            if (ReviewInput.GetKeyDown(KeyCode.W)
                && (Orbit == null || !Orbit.InteriorMode))
                SetWaterlineVisible(!_showWaterline);
        }

        void HandleGamepad()
        {
#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
            if (_gamepadMap == null || !_gamepadMap.enabled) return;
            var orbit = Orbit;
            if (orbit == null) return;

            Vector2 look = _gpLook.ReadValue<Vector2>();
            Vector2 move = _gpMove.ReadValue<Vector2>();
            float zoom = _gpZoom.ReadValue<float>();

            if (orbit.InteriorMode)
            {
                if (look.sqrMagnitude > 0.01f)
                    orbit.ApplyInteriorLook(look.x * 2f, look.y * 2f);
                if (move.sqrMagnitude > 0.01f)
                {
                    var cam = orbit.Cam;
                    if (cam != null)
                    {
                        Vector3 dir = cam.transform.right * move.x + cam.transform.forward * move.y;
                        orbit.ApplyInteriorMove(dir, Time.deltaTime, false);
                    }
                }
            }
            else
            {
                if (look.sqrMagnitude > 0.01f)
                    orbit.ApplyOrbitDelta(look.x * 2f, look.y * 2f);
                if (move.sqrMagnitude > 0.01f)
                    orbit.ApplyPanDelta(move * 40f);
                if (Mathf.Abs(zoom) > 0.01f)
                    orbit.ApplyZoomScroll(zoom * 0.15f);
            }

            if (_gpReset.WasPressedThisFrame())
            {
                orbit.ResetExterior();
                FrameAndAutoscale();
            }
            if (_gpFrame.WasPressedThisFrame()) FrameAndAutoscale();
            if (_gpView.WasPressedThisFrame()) CycleViewMode();
            if (_gpInterior.WasPressedThisFrame()) orbit.ToggleInterior();
            if (_gpLegend.WasPressedThisFrame()) SetLegendVisible(!_showLegend);
            if (_gpPrev.WasPressedThisFrame()) BrowseRequested?.Invoke(-1);
            if (_gpNext.WasPressedThisFrame()) BrowseRequested?.Invoke(1);
#endif
        }

        void EnsureGamepadActions()
        {
#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
            if (_gamepadMap != null) return;
            _gamepadMap = new InputActionMap("ModelReviewGamepad");
            _gpLook = _gamepadMap.AddAction("Look", InputActionType.Value, "<Gamepad>/rightStick");
            _gpMove = _gamepadMap.AddAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
            _gpZoom = _gamepadMap.AddAction("Zoom", InputActionType.Value);
            _gpZoom.AddCompositeBinding("1DAxis")
                .With("Positive", "<Gamepad>/rightTrigger")
                .With("Negative", "<Gamepad>/leftTrigger");
            _gpReset = _gamepadMap.AddAction("Reset", InputActionType.Button, "<Gamepad>/buttonSouth");
            _gpFrame = _gamepadMap.AddAction("Frame", InputActionType.Button, "<Gamepad>/buttonNorth");
            _gpView = _gamepadMap.AddAction("View", InputActionType.Button, "<Gamepad>/buttonWest");
            _gpInterior = _gamepadMap.AddAction("Interior", InputActionType.Button, "<Gamepad>/buttonEast");
            _gpLegend = _gamepadMap.AddAction("Legend", InputActionType.Button, "<Gamepad>/start");
            _gpPrev = _gamepadMap.AddAction("Prev", InputActionType.Button, "<Gamepad>/leftShoulder");
            _gpNext = _gamepadMap.AddAction("Next", InputActionType.Button, "<Gamepad>/rightShoulder");
            _gamepadMap.Enable();
#endif
        }

        void DisableGamepadActions()
        {
#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
            _gamepadMap?.Disable();
#endif
        }

        void DisposeGamepadActions()
        {
#if ENABLE_INPUT_SYSTEM && AMBERSCREEN_MODELREVIEW_INPUTSYSTEM
            if (_gamepadMap != null)
            {
                _gamepadMap.Disable();
                _gamepadMap.Dispose();
                _gamepadMap = null;
            }
#endif
        }

        void ApplyTone()
        {
            var vol = _wiredVolume != null ? _wiredVolume : volume;
            var orbit = Orbit;
            var cam = orbit != null ? orbit.Cam : null;
            UniversalAdditionalCameraData urpCam = null;
            if (cam != null) cam.TryGetComponent(out urpCam);

            if (_tone == ModelReviewTone.Off)
            {
                if (vol != null) vol.enabled = false;
                if (urpCam != null) urpCam.renderPostProcessing = false;
                return;
            }

            EnsureRuntimeVolumeProfile();
            if (vol != null) vol.enabled = true;
            if (urpCam != null) urpCam.renderPostProcessing = true;

            if (_runtimeTonemap != null)
            {
                _runtimeTonemap.mode.overrideState = true;
                _runtimeTonemap.mode.value = _tone == ModelReviewTone.Aces
                    ? TonemappingMode.ACES
                    : TonemappingMode.Neutral;
            }

            // Slight contrast so ACES is visibly different from Neutral in the grey studio.
            if (_runtimeColorAdjust != null)
            {
                bool aces = _tone == ModelReviewTone.Aces;
                _runtimeColorAdjust.contrast.overrideState = aces;
                _runtimeColorAdjust.contrast.value = aces ? 12f : 0f;
                _runtimeColorAdjust.postExposure.overrideState = aces;
                _runtimeColorAdjust.postExposure.value = aces ? 0.15f : 0f;
            }
        }

        void EnsureRuntimeVolumeProfile()
        {
            var vol = _wiredVolume != null ? _wiredVolume : volume;
            if (vol == null) return;
            if (_runtimeVolumeProfile != null)
            {
                vol.sharedProfile = _runtimeVolumeProfile;
                return;
            }

            var source = vol.sharedProfile;
            if (source == null) return;
            _runtimeVolumeProfile = Instantiate(source);
            _runtimeVolumeProfile.name = source.name + " (Review Tone Clone)";
            _runtimeVolumeProfile.hideFlags = HideFlags.DontSave;
            vol.sharedProfile = _runtimeVolumeProfile;

            if (!_runtimeVolumeProfile.TryGet(out _runtimeTonemap))
                _runtimeTonemap = _runtimeVolumeProfile.Add<Tonemapping>(true);
            if (!_runtimeVolumeProfile.TryGet(out _runtimeColorAdjust))
                _runtimeColorAdjust = _runtimeVolumeProfile.Add<ColorAdjustments>(true);
        }

        void EnsureWaterline()
        {
            if (_waterlinePlane != null) return;
            _waterlinePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _waterlinePlane.name = "Waterline";
            _waterlinePlane.hideFlags = HideFlags.DontSave;
            DestroyImmediate(_waterlinePlane.GetComponent<Collider>());
            _waterlinePlane.transform.SetParent(transform, false);
            _waterlinePlane.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
            _waterlineMat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                         ?? Shader.Find("Sprites/Default")
                                         ?? Shader.Find("Hidden/InternalErrorShader"))
            {
                name = "ModelReview_Waterline_Runtime",
                hideFlags = HideFlags.DontSave,
                color = new Color(0.2f, 0.45f, 0.7f, 0.35f)
            };
            if (_waterlineMat.HasProperty("_BaseColor"))
                _waterlineMat.SetColor("_BaseColor", new Color(0.2f, 0.45f, 0.7f, 0.35f));
            if (_waterlineMat.HasProperty("_Surface"))
                _waterlineMat.SetFloat("_Surface", 1f);
            var r = _waterlinePlane.GetComponent<MeshRenderer>();
            r.sharedMaterial = _waterlineMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _waterlinePlane.SetActive(false);
            RefreshWaterlineDraft();
        }

        void RefreshWaterlineDraft()
        {
            if (_waterlinePlane == null) return;
            _waterlinePlane.transform.position = new Vector3(0f, waterlineHeight, 0f);
        }

        static string ToneName(ModelReviewTone t) => t switch
        {
            ModelReviewTone.Neutral => "Neutral",
            ModelReviewTone.Aces => "ACES",
            _ => "Off"
        };

        static string UpAxisHint(Bounds bounds)
        {
            float x = bounds.size.x, y = bounds.size.y, z = bounds.size.z;
            float max = Mathf.Max(x, Mathf.Max(y, z));
            float min = Mathf.Max(0.0001f, Mathf.Min(x, Mathf.Min(y, z)));
            if (max / min < 2f) return null;
            if (y + 0.0001f >= x && y + 0.0001f >= z) return null;
            return "Up axis looks wrong?";
        }

        static string ScaleWarning(Bounds bounds)
        {
            float m = bounds.size.magnitude;
            if (m < 0.01f) return "Scale warning: model is smaller than 0.01 m";
            if (m > 100f) return "Scale warning: model is larger than 100 m";
            return null;
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
                    ApplyOverride(ClayMaterial);
                    break;
                case ModelReviewViewMode.Wireframe:
                    ApplyWireframe();
                    break;
            }
        }

        void ApplyWireframe()
        {
            var mat = ClayWireMaterial;
            var shader = mat != null ? mat.shader : null;
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
                ApplyOverride(ClayMaterial);
                return;
            }
            ApplyOverride(mat);
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
