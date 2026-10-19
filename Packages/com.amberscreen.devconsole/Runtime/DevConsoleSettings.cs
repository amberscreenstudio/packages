using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberScreen.Dev
{
    public enum DevConsoleLayout
    {
        Floating = 0,
        DockRight = 1,
        MobilePortrait = 2,
        Rail = 3
    }

    public enum DevConsoleScalePreset
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }

    [CreateAssetMenu(
        fileName = "AmberScreenDevConsoleSettings",
        menuName = "Amber Screen/Dev Console Settings")]
    public sealed class DevConsoleSettings : ScriptableObject
    {
        public const string ResourcesName = "AmberScreenDevConsoleSettings";

        [Header("Compile / discovery")]
        [Tooltip("Editor: add AMBERSCREEN_DEVCONSOLE to the active build target if missing.")]
        public bool autoAddDefine = true;

        [Tooltip("If off (default), AMBERSCREEN_DEVCONSOLE is stripped from non-development player builds.")]
        public bool keepInReleaseBuilds = false;

        public bool enableAttributeDiscovery = true;
        public bool enableProviderDiscovery = true;

        [Header("Layout")]
        public DevConsoleLayout layout = DevConsoleLayout.Rail;
        public DevConsoleScalePreset scale = DevConsoleScalePreset.Medium;
        [Min(0.25f)] public float scaleMultiplier = 1f;
        [Min(1)] public int columns = 1;
        public bool autoColumns = true;
        [Min(80f)] public float minEntryWidth = 220f;
        [Min(24f)] public float buttonHeight = 30f;
        [Min(10f)] public float fontSize = 16f;
        [Min(0f)] public float spacing = 4f;
        public int sortingOrder = 1000;

        [Tooltip("Fraction of the safe area filled by the Floating panel (centred).")]
        public Vector2 floatingScreenFraction = new Vector2(0.9f, 0.88f);

        [Tooltip("Ignored for Floating layout (use floatingScreenFraction). Kept for older assets.")]
        [Min(200f)] public float floatingPanelWidth = 560f;

        [Tooltip("DockRight width as a fraction of the safe-area width.")]
        [Min(0.1f)] public float dockRightScreenFraction = 0.28f;

        [Tooltip("Minimum DockRight width (canvas units).")]
        [Min(120f)] public float dockRightWidth = 320f;

        [Header("Rail")]
        [Min(0.05f)] public float railScreenFraction = 0.17f;
        [Min(80f)] public float railMinWidth = 220f;
        [Min(80f)] public float railMaxWidth = 340f;
        [Min(0f)] public float railEdgeMargin = 6f;
        [Min(0f)] public float windowMargin = 24f;
        public bool rememberState = true;

        [Tooltip("Extra multiplier for portrait / phone UI scale (dpi-based ConstantPixelSize).")]
        [Range(0.75f, 2f)] public float portraitUiScale = 1f;

        [Header("Font")]
        [Tooltip("Optional TMP override. If set, used instead of bundled Inter.")]
        public TMP_FontAsset fontOverride;

        [Header("Open bindings")]
        public Key openKey = Key.F1;
        public bool enableGamepadChord = true;
        public bool enableThreeFingerTap = true;
        public bool enableCornerHold = true;
        [Tooltip("Editor only: allow mouse left-button for the top-left corner hold. Off by default.")]
        public bool cornerHoldWithMouseInEditor = false;
        public bool showOnScreenDevButton = false;

        [Header("While open")]
        public bool showCursorWhileOpen = true;
        [Tooltip("For Rail: pauses only while a section window is open. For other layouts: while the console is open and expanded.")]
        public bool pauseTimescaleWhileOpen = false;

        static DevConsoleSettings s_RuntimeDefault;

        public float ResolvedScaleFactor
        {
            get
            {
                float preset = scale switch
                {
                    DevConsoleScalePreset.Small => 0.85f,
                    DevConsoleScalePreset.Large => 1.25f,
                    _ => 1f
                };
                return preset * scaleMultiplier;
            }
        }

        public Vector2 ReferenceResolution =>
            layout == DevConsoleLayout.MobilePortrait
                ? new Vector2(1080f, 1920f)
                : new Vector2(1920f, 1080f);

        public static DevConsoleSettings LoadOrDefault()
        {
            var loaded = Resources.Load<DevConsoleSettings>(ResourcesName);
            if (loaded != null)
                return loaded;

            if (s_RuntimeDefault == null)
            {
                s_RuntimeDefault = CreateInstance<DevConsoleSettings>();
                s_RuntimeDefault.hideFlags = HideFlags.HideAndDontSave;
            }

            return s_RuntimeDefault;
        }

        public void ApplyCompactDefaults()
        {
            layout = DevConsoleLayout.Rail;
            buttonHeight = 30f;
            fontSize = 16f;
            spacing = 4f;
            autoColumns = true;
            minEntryWidth = 220f;
            floatingScreenFraction = new Vector2(0.9f, 0.88f);
            dockRightScreenFraction = 0.28f;
            columns = 1;
            railScreenFraction = 0.17f;
            railMinWidth = 220f;
            railMaxWidth = 340f;
            railEdgeMargin = 6f;
            windowMargin = 24f;
            rememberState = true;
            portraitUiScale = 1f;
        }

        internal static void ResetRuntimeDefault()
        {
            if (s_RuntimeDefault == null)
                return;

            if (Application.isPlaying)
                Destroy(s_RuntimeDefault);
            else
                DestroyImmediate(s_RuntimeDefault);

            s_RuntimeDefault = null;
        }

#if UNITY_EDITOR
        void Reset()
        {
            ApplyCompactDefaults();
        }

        void OnValidate()
        {
#if AMBERSCREEN_DEVCONSOLE
            if (Application.isPlaying)
                DevConsoleHost.ReloadCachedSettings(this);
#endif
        }
#endif
    }
}
