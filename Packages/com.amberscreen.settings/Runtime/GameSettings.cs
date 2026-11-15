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
        /// <summary>(key, value) when a game-defined option changes.</summary>
        public static event Action<string, float> CustomChanged;
        /// <summary>Raised when a game-defined option is added or removed (the built-in menu rebuilds).</summary>
        public static event Action CustomOptionsChanged;

        static readonly System.Collections.Generic.List<SettingsCustomOption> s_Options =
            new System.Collections.Generic.List<SettingsCustomOption>();
        static int s_OptionsVersion;

        /// <summary>Folder the file is saved in. Null = Application.persistentDataPath.</summary>
        public static string DirectoryOverride { get; set; }

        public static bool IsInitialized => s_Session != null;
        public static SettingsSession Session => s_Session;
        public static SettingsProfile Profile => s_Session?.Profile;
        public static GameSettingsData Current => s_Session?.Data;
        public static string FilePath => s_Session?.FilePath;

        public static float MouseSensitivity => s_Session != null ? s_Session.Data.mouseSensitivity : 1f;
        public static bool InvertY => s_Session != null && s_Session.Data.invertY;
        /// <summary>Text size, 0.75 .. 1.6 (1 = normal). Multiply your font sizes by it, or use <see cref="ScaledFontSize"/>.</summary>
        public static float TextScale => s_Session != null ? s_Session.Data.textScale : 1f;

        /// <summary>A font size scaled by <see cref="TextScale"/>, rounded and kept between 8 and 64.</summary>
        public static int ScaledFontSize(int baseSize) => ScaledFontSize(baseSize, TextScale);

        public static int ScaledFontSize(int baseSize, float scale) =>
            Mathf.Clamp(Mathf.RoundToInt(baseSize * GameSettingsData.ClampTextScale(scale)), 8, 64);

        /// <summary>Game-defined options, in the order they were added.</summary>
        public static System.Collections.Generic.IReadOnlyList<SettingsCustomOption> CustomOptions => s_Options;
        /// <summary>Goes up by one each time an option is added or removed.</summary>
        public static int CustomOptionsVersion => s_OptionsVersion;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Session = null;
            Changed = null;
            ControlsChanged = null;
            CustomChanged = null;
            CustomOptionsChanged = null;
            s_Options.Clear();
            s_OptionsVersion++;
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
            s_Session.CustomChanged += OnCustomChanged;
            s_Session.LoadOrCreate();
            s_Session.ApplyAll();
            NotifyAllCustom();

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
            s_Session.CustomChanged -= OnCustomChanged;
        }

        static void OnChanged(GameSettingsData data) => Changed?.Invoke(data);
        static void OnControlsChanged(float sensitivity, bool invert) => ControlsChanged?.Invoke(sensitivity, invert);

        static void OnCustomChanged(string key, float value)
        {
            var option = FindOption(key);
            if (option?.OnChanged != null)
            {
                try { option.OnChanged(value); }
                catch (Exception e) { Debug.LogException(e); }
            }
            CustomChanged?.Invoke(key, value);
        }

        static void NotifyAllCustom()
        {
            if (s_Session == null) return;
            for (int i = 0; i < s_Options.Count; i++)
            {
                var o = s_Options[i];
                if (o?.OnChanged == null) continue;
                try { o.OnChanged(GetCustom(o.Key)); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

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
        public static void ResetToDefaults()
        {
            Require().ResetToDefaults();
            NotifyAllCustom();
        }

        public static void SetTextScale(float scale) => Require().SetTextScale(scale);

        /// <summary>Runs <paramref name="import"/> once per save file, for example to bring over old PlayerPrefs. See <see cref="LegacyPrefs"/>.</summary>
        public static bool ImportLegacyOnce(Func<GameSettingsData, bool> import)
        {
            bool changed = Require().ImportLegacyOnce(import);
            if (changed) NotifyAllCustom();
            return changed;
        }

        // ---------- game-defined options ----------

        /// <summary>Adds (or replaces) a slider row. <paramref name="onChanged"/> runs now with the saved value and on every change.</summary>
        public static SettingsCustomOption AddCustomSlider(string key, string label, float min, float max, float defaultValue,
            Action<float> onChanged = null, Func<float, string> format = null, string section = "Game") =>
            AddOption(new SettingsCustomOption
            {
                Key = key, Label = label, Section = section, Kind = SettingsCustomKind.Slider,
                Min = min, Max = max, DefaultValue = defaultValue, Format = format, OnChanged = onChanged
            });

        /// <summary>Adds (or replaces) an On/Off row.</summary>
        public static SettingsCustomOption AddCustomToggle(string key, string label, bool defaultValue,
            Action<bool> onChanged = null, string section = "Game") =>
            AddOption(new SettingsCustomOption
            {
                Key = key, Label = label, Section = section, Kind = SettingsCustomKind.Toggle,
                Min = 0f, Max = 1f, DefaultValue = defaultValue ? 1f : 0f,
                OnChanged = onChanged != null ? v => onChanged(v >= 0.5f) : (Action<float>)null
            });

        /// <summary>Adds (or replaces) a &lt; choice &gt; row. The saved value is the choice index.</summary>
        public static SettingsCustomOption AddCustomStepper(string key, string label, string[] choices, int defaultIndex,
            Action<int> onChanged = null, string section = "Game") =>
            AddOption(new SettingsCustomOption
            {
                Key = key, Label = label, Section = section, Kind = SettingsCustomKind.Stepper,
                Choices = choices ?? Array.Empty<string>(), Min = 0f,
                Max = Mathf.Max(0, (choices?.Length ?? 1) - 1), DefaultValue = defaultIndex,
                OnChanged = onChanged != null ? v => onChanged(Mathf.RoundToInt(v)) : (Action<float>)null
            });

        public static bool RemoveCustomOption(string key)
        {
            int removed = s_Options.RemoveAll(o => o.Key == key);
            if (removed == 0) return false;
            s_OptionsVersion++;
            CustomOptionsChanged?.Invoke();
            return true;
        }

        public static void ClearCustomOptions()
        {
            if (s_Options.Count == 0) return;
            s_Options.Clear();
            s_OptionsVersion++;
            CustomOptionsChanged?.Invoke();
        }

        public static SettingsCustomOption FindOption(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < s_Options.Count; i++)
                if (s_Options[i].Key == key) return s_Options[i];
            return null;
        }

        /// <summary>Saved value, or the option's default, or <paramref name="fallback"/> for an unknown key.</summary>
        public static float GetCustom(string key, float fallback = 0f)
        {
            var option = FindOption(key);
            float def = option != null ? option.DefaultValue : fallback;
            if (s_Session == null) return def;
            float v = s_Session.GetCustom(key, def);
            return option != null ? option.Clamp(v) : v;
        }

        public static bool GetCustomBool(string key, bool fallback = false) => GetCustom(key, fallback ? 1f : 0f) >= 0.5f;
        public static int GetCustomInt(string key, int fallback = 0) => Mathf.RoundToInt(GetCustom(key, fallback));

        /// <summary>Sets a game-defined value (clamped to its option), applies it and raises the events. Call Save to keep it.</summary>
        public static void SetCustom(string key, float value)
        {
            var option = FindOption(key);
            Require().SetCustom(key, option != null ? option.Clamp(value) : value);
        }

        public static void SetCustom(string key, bool value) => SetCustom(key, value ? 1f : 0f);
        public static void SetCustom(string key, int value) => SetCustom(key, (float)value);

        static SettingsCustomOption AddOption(SettingsCustomOption option)
        {
            if (string.IsNullOrEmpty(option.Key))
            {
                Debug.LogWarning("[Settings] A custom option needs a key.");
                return null;
            }
            if (string.IsNullOrEmpty(option.Label)) option.Label = option.Key;
            if (string.IsNullOrEmpty(option.Section)) option.Section = "Game";
            option.DefaultValue = option.Clamp(option.DefaultValue);
            int existing = s_Options.FindIndex(o => o.Key == option.Key);
            if (existing >= 0) s_Options[existing] = option;
            else s_Options.Add(option);
            s_OptionsVersion++;
            CustomOptionsChanged?.Invoke();
            if (s_Session != null && option.OnChanged != null)
            {
                try { option.OnChanged(GetCustom(option.Key)); }
                catch (Exception e) { Debug.LogException(e); }
            }
            return option;
        }

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
