using System;
using System.IO;
using UnityEngine;

namespace AmberScreen.Settings
{
    /// <summary>
    /// One set of settings bound to one file. <see cref="GameSettings"/> wraps a single shared session;
    /// create your own only for tests or tools.
    /// </summary>
    public sealed class SettingsSession
    {
        readonly ISettingsApplier m_Applier;
        readonly Func<bool> m_IsSteamDeck;

        public SettingsProfile Profile { get; }
        public string FilePath { get; }
        public GameSettingsData Data { get; private set; } = new GameSettingsData();
        /// <summary>True when the last load found no valid save and created defaults.</summary>
        public bool WasFirstRun { get; private set; }

        /// <summary>Raised after any change is applied.</summary>
        public event Action<GameSettingsData> Changed;
        /// <summary>Raised when mouse sensitivity or invert Y change: (sensitivity, invertY).</summary>
        public event Action<float, bool> ControlsChanged;
        /// <summary>Raised when a game-defined value changes: (key, value).</summary>
        public event Action<string, float> CustomChanged;

        public SettingsSession(SettingsProfile profile, string filePath, ISettingsApplier applier = null,
            Func<bool> isSteamDeck = null)
        {
            Profile = profile;
            FilePath = filePath;
            m_Applier = applier ?? new UnitySettingsApplier();
            m_IsSteamDeck = isSteamDeck ?? SteamDeckDetector.IsSteamDeck;
        }

        public int QualityLevelCount => m_Applier.QualityLevelCount;

        // ---------- load / save ----------

        /// <summary>Loads the file, or builds first-run defaults (Steam Deck preset if detected) and saves them.</summary>
        public void LoadOrCreate()
        {
            GameSettingsData loaded = null;
            try
            {
                if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
                    loaded = GameSettingsData.FromJson(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Settings] Could not read '{FilePath}': {e.Message}. Using defaults.");
            }

            WasFirstRun = loaded == null;
            Data = loaded ?? CreateDefaults();
            if (WasFirstRun) Save();
        }

        public GameSettingsData CreateDefaults()
        {
            var data = new GameSettingsData();
            m_Applier.CaptureCurrent(data);
            if (Profile != null)
            {
                data.targetFrameRate = Profile.defaultTargetFrameRate;
                data.mouseSensitivity = Profile.defaultMouseSensitivity;
            }

            bool deckPreset = Profile == null || Profile.steamDeckPresetOnFirstRun;
            if (deckPreset && m_IsSteamDeck())
                ApplySteamDeckPresetTo(data);
            return data;
        }

        void ApplySteamDeckPresetTo(GameSettingsData data)
        {
            int byName = FindQualityByName(Profile != null ? Profile.steamDeckQualityName : null);
            data.qualityLevel = byName >= 0 ? byName : ClampQuality(Profile != null ? Profile.steamDeckQualityLevel : 0);
            data.targetFrameRate = Profile != null ? Profile.steamDeckTargetFrameRate : 30;
            data.vSync = false; // vSync would override the 30 FPS target.
            data.resolutionWidth = 0;
            data.resolutionHeight = 0;
            data.fullScreenMode = FullScreenMode.FullScreenWindow;
            data.steamDeckPresetApplied = true;
        }

        /// <summary>Writes the JSON (temp file then swap, so a crash never leaves half a file).</summary>
        public bool Save()
        {
            if (string.IsNullOrEmpty(FilePath)) return false;
            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, Data.ToJson());
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temp, FilePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Settings] Could not save '{FilePath}': {e.Message}");
                return false;
            }
        }

        /// <summary>Deletes the save file so the next load is a first run.</summary>
        public void DeleteSave()
        {
            try
            {
                if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Settings] Could not delete '{FilePath}': {e.Message}");
            }
        }

        // ---------- apply ----------

        public void ApplyAll()
        {
            Data.qualityLevel = ClampQuality(Data.qualityLevel);
            m_Applier.ApplyGraphics(Data);
            m_Applier.ApplyAudio(Data, Profile);
            RaiseChanged(true);
        }

        public void ApplyAudio() => m_Applier.ApplyAudio(Data, Profile);

        /// <summary>Picks a quality level (a "preset") and applies graphics.</summary>
        public void ApplyPreset(int qualityLevel)
        {
            Data.qualityLevel = ClampQuality(qualityLevel);
            m_Applier.ApplyGraphics(Data);
            RaiseChanged(false);
        }

        /// <summary>Applies the Steam Deck preset now (quality level, 30 FPS, vSync off).</summary>
        public void ApplySteamDeckPreset()
        {
            ApplySteamDeckPresetTo(Data);
            m_Applier.ApplyGraphics(Data);
            RaiseChanged(false);
        }

        public void SetResolution(int width, int height)
        {
            Data.resolutionWidth = Mathf.Max(0, width);
            Data.resolutionHeight = Mathf.Max(0, height);
            m_Applier.ApplyGraphics(Data);
            RaiseChanged(false);
        }

        public void SetFullScreenMode(FullScreenMode mode)
        {
            Data.fullScreenMode = mode;
            m_Applier.ApplyGraphics(Data);
            RaiseChanged(false);
        }

        public void SetVSync(bool on)
        {
            Data.vSync = on;
            m_Applier.ApplyGraphics(Data);
            RaiseChanged(false);
        }

        public void SetTargetFrameRate(int fps)
        {
            Data.targetFrameRate = fps <= 0 ? -1 : fps;
            m_Applier.ApplyGraphics(Data);
            RaiseChanged(false);
        }

        public float GetVolume(VolumeChannel channel)
        {
            switch (channel)
            {
                case VolumeChannel.Music: return Data.musicVolume;
                case VolumeChannel.Sfx: return Data.sfxVolume;
                default: return Data.masterVolume;
            }
        }

        public void SetVolume(VolumeChannel channel, float linear)
        {
            linear = Mathf.Clamp01(linear);
            switch (channel)
            {
                case VolumeChannel.Music: Data.musicVolume = linear; break;
                case VolumeChannel.Sfx: Data.sfxVolume = linear; break;
                default: Data.masterVolume = linear; break;
            }
            m_Applier.ApplyAudio(Data, Profile);
            RaiseChanged(false);
        }

        public void SetMouseSensitivity(float value)
        {
            float min = Profile != null ? Profile.minMouseSensitivity : 0.1f;
            float max = Profile != null ? Profile.maxMouseSensitivity : 5f;
            Data.mouseSensitivity = Mathf.Clamp(value, min, max);
            RaiseChanged(true);
        }

        public void SetInvertY(bool invert)
        {
            Data.invertY = invert;
            RaiseChanged(true);
        }

        /// <summary>Text size, clamped to 0.75 .. 1.6. Not an engine setting: menus and HUDs read it.</summary>
        public void SetTextScale(float scale)
        {
            Data.textScale = GameSettingsData.ClampTextScale(scale);
            RaiseChanged(false);
        }

        public float GetCustom(string key, float fallback) =>
            Data.TryGetCustom(key, out float v) ? v : fallback;

        public bool HasCustom(string key) => Data.TryGetCustom(key, out _);

        /// <summary>Stores a game-defined value (saved with the rest on <see cref="Save"/>).</summary>
        public void SetCustom(string key, float value)
        {
            if (string.IsNullOrEmpty(key)) return;
            Data.SetCustom(key, value);
            CustomChanged?.Invoke(key, value);
            RaiseChanged(false);
        }

        /// <summary>Index of a quality level by name (case-insensitive), or -1.</summary>
        public int FindQualityByName(string name)
        {
            if (string.IsNullOrEmpty(name) || !(m_Applier is ISettingsQualityNames named)) return -1;
            var names = named.QualityNames;
            if (names == null) return -1;
            for (int i = 0; i < names.Length; i++)
                if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        /// <summary>
        /// Runs <paramref name="import"/> once per save file (for example to read old PlayerPrefs values into
        /// <see cref="Data"/>). Return true from it when something changed: everything is applied and saved.
        /// Returns true when the import ran and changed something.
        /// </summary>
        public bool ImportLegacyOnce(Func<GameSettingsData, bool> import)
        {
            if (import == null || Data.legacyImported) return false;
            bool changed = false;
            try { changed = import(Data); }
            catch (Exception e) { Debug.LogWarning($"[Settings] Legacy import failed: {e.Message}"); }
            Data.legacyImported = true;
            Data.textScale = GameSettingsData.ClampTextScale(Data.textScale);
            if (changed) ApplyAll();
            Save();
            return changed;
        }

        public void SetBindingOverrides(string json)
        {
            Data.bindingOverrides = json ?? "";
            RaiseChanged(false);
        }

        /// <summary>Back to first-run defaults (including the Steam Deck preset on a Deck), applied but not saved.</summary>
        public void ResetToDefaults()
        {
            bool imported = Data != null && Data.legacyImported;
            Data = CreateDefaults();
            Data.legacyImported = imported; // never re-import old values after a reset
            ApplyAll();
        }

        int ClampQuality(int level)
        {
            int count = m_Applier.QualityLevelCount;
            return count <= 0 ? 0 : Mathf.Clamp(level, 0, count - 1);
        }

        void RaiseChanged(bool controls)
        {
            Changed?.Invoke(Data);
            if (controls) ControlsChanged?.Invoke(Data.mouseSensitivity, Data.invertY);
        }
    }
}
