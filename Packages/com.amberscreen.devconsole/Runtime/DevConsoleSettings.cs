using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberScreen.DevConsole
{
    public enum DevConsoleLayout
    {
        Floating = 0,
        DockRight = 1,
        MobilePortrait = 2
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
        public DevConsoleLayout layout = DevConsoleLayout.Floating;
        public DevConsoleScalePreset scale = DevConsoleScalePreset.Medium;
        [Min(0.25f)] public float scaleMultiplier = 1f;
        [Min(1)] public int columns = 1;
        [Min(24f)] public float buttonHeight = 44f;
        [Min(10f)] public float fontSize = 22f;
        [Min(0f)] public float spacing = 8f;
        public int sortingOrder = 1000;
        [Min(200f)] public float floatingPanelWidth = 560f;
        [Min(200f)] public float dockRightWidth = 320f;

        [Header("Font")]
        [Tooltip("Optional override. If null, TMP defaults / Resources fallback are used.")]
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
    }
}
