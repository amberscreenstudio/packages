using System;
using System.IO;
using UnityEngine;

namespace AmberScreen.Settings
{
    /// <summary>
    /// The game-wide settings. Use this API to build your own menu, or use <see cref="SettingsMenu"/>.
    /// Initialised automatically on startup when a <see cref="SettingsProfile"/> named
    /// <c>AmberScreenSettingsProfile</c> is in a Resources folder; otherwise call <see cref="Initialize"/>.
    /// </summary>
    public static class GameSettings
    {
        static SettingsSession s_Session;

        /// <summary>Raised after any change is applied.</summary>
        public static event Action<GameSettingsData> Changed;
        /// <summary>(mouse sensitivity, invert Y). Read these in your camera/look code.</summary>
        public static event Action<float, bool> ControlsChanged;

        /// <summary>Folder the file is saved in. Null = Application.persistentDataPath.</summary>
        public static string DirectoryOverride { get; set; }

        public static bool IsInitialized => s_Session != null;
        public static SettingsSession Session => s_Session;
        public static SettingsProfile Profile => s_Session?.Profile;
        public static GameSettingsData Current => s_Session?.Data;
        public static string FilePath => s_Session?.FilePath;

        public static float MouseSensitivity => s_Session != null ? s_Session.Data.mouseSensitivity : 1f;
        public static bool InvertY => s_Session != null && s_Session.Data.invertY;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Session = null;
            Changed = null;
            ControlsChanged = null;
            DirectoryOverride = null;
            SettingsAudioLateApply.ResetPending();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoInitialize()
        {
            var profile = Resources.Load<SettingsProfile>(SettingsProfile.ResourcesName);
            if (profile == null || !profile.applyOnStartup) return;
            Initialize(profile);
        }

        /// <summary>Loads (or creates on first run) and applies everything. Safe to call again with another profile.</summary>
        public static SettingsSession Initialize(SettingsProfile profile = null, ISettingsApplier applier = null)
        {
            if (profile == null) profile = Resources.Load<SettingsProfile>(SettingsProfile.ResourcesName);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<SettingsProfile>();
                profile.hideFlags = HideFlags.DontSave;
            }

            string dir = string.IsNullOrEmpty(DirectoryOverride) ? Application.persistentDataPath : DirectoryOverride;
            string file = string.IsNullOrEmpty(profile.fileName) ? "settings.json" : profile.fileName;

            Detach();
            s_Session = new SettingsSession(profile, Path.Combine(dir, file), applier);
            s_Session.Changed += OnChanged;
            s_Session.ControlsChanged += OnControlsChanged;
            s_Session.LoadOrCreate();
            s_Session.ApplyAll();

            // AudioMixer.SetFloat is ignored before the first frame, so apply audio again once running.
            if (Application.isPlaying) SettingsAudioLateApply.Schedule();
            return s_Session;
        }

        static SettingsSession Require()
        {
            if (s_Session == null) Initialize();
            return s_Session;
        }

        static void Detach()
        {
            if (s_Session == null) return;
            s_Session.Changed -= OnChanged;
            s_Session.ControlsChanged -= OnControlsChanged;
        }

        static void OnChanged(GameSettingsData data) => Changed?.Invoke(data);
        static void OnControlsChanged(float sensitivity, bool invert) => ControlsChanged?.Invoke(sensitivity, invert);

        // ---------- options for menus ----------

        public static string[] QualityNames => QualitySettings.names;

        /// <summary>Distinct width x height pairs the screen supports, smallest first.</summary>
        public static Vector2Int[] GetResolutionOptions()
        {
            var list = new System.Collections.Generic.List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (!list.Contains(v)) list.Add(v);
            }
            if (list.Count == 0) list.Add(new Vector2Int(Screen.width, Screen.height));
            return list.ToArray();
        }

        public static int[] FrameRateOptions =>
            Profile != null && Profile.frameRateOptions != null && Profile.frameRateOptions.Length > 0
                ? Profile.frameRateOptions
                : new[] { 30, 60, 120, -1 };

        // ---------- changes (each one applies immediately; call Save to keep it) ----------

        public static void Save() => Require().Save();
        public static void ApplyAll() => Require().ApplyAll();
        public static void ApplyPreset(int qualityLevel) => Require().ApplyPreset(qualityLevel);
        public static void ApplySteamDeckPreset() => Require().ApplySteamDeckPreset();
        public static void SetResolution(int width, int height) => Require().SetResolution(width, height);
        public static void SetFullScreenMode(FullScreenMode mode) => Require().SetFullScreenMode(mode);
        public static void SetVSync(bool on) => Require().SetVSync(on);
        public static void SetTargetFrameRate(int fps) => Require().SetTargetFrameRate(fps);
        public static float GetVolume(VolumeChannel channel) => Require().GetVolume(channel);
        public static void SetVolume(VolumeChannel channel, float linear) => Require().SetVolume(channel, linear);
        public static void SetMouseSensitivity(float value) => Require().SetMouseSensitivity(value);
        public static void SetInvertY(bool invert) => Require().SetInvertY(invert);
        public static void SetBindingOverrides(string json) => Require().SetBindingOverrides(json);
        public static void ResetToDefaults() => Require().ResetToDefaults();

        /// <summary>Deletes the save and runs first-run setup again (detects the Steam Deck again).</summary>
        public static void ResetAsFirstRun()
        {
            var session = Require();
            session.DeleteSave();
            Initialize(session.Profile);
        }
    }

    /// <summary>Re-applies mixer volumes on the first frame, then removes itself.</summary>
    sealed class SettingsAudioLateApply : MonoBehaviour
    {
        static SettingsAudioLateApply s_Pending;

        internal static void ResetPending() => s_Pending = null;

        internal static void Schedule()
        {
            if (s_Pending != null) return;
            var go = new GameObject("AmberScreenSettingsAudioApply") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            s_Pending = go.AddComponent<SettingsAudioLateApply>();
        }

        void Update()
        {
            GameSettings.Session?.ApplyAudio();
            s_Pending = null;
            Destroy(gameObject);
        }
    }
}
