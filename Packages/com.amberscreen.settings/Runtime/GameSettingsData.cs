using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Settings
{
    /// <summary>Everything the player can choose. Saved as JSON. Plain values only.</summary>
    [Serializable]
    public sealed class GameSettingsData
    {
        public const int CurrentFormat = 2;
        public const float MinTextScale = 0.75f;
        public const float MaxTextScale = 1.6f;

        public int format = CurrentFormat;

        /// <summary>Index into QualitySettings.names.</summary>
        public int qualityLevel;
        /// <summary>0 = keep the screen's current resolution.</summary>
        public int resolutionWidth;
        public int resolutionHeight;
        public FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
        public bool vSync = true;
        /// <summary>-1 = unlimited (platform default).</summary>
        public int targetFrameRate = -1;

        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;

        public float mouseSensitivity = 1f;
        public bool invertY;
        /// <summary>Input System binding overrides JSON, empty when not used.</summary>
        public string bindingOverrides = "";

        /// <summary>True when the first-run Steam Deck preset was picked.</summary>
        public bool steamDeckPresetApplied;

        /// <summary>Text size for menus and HUD text, 0.75 (75%) to 1.6 (160%). 1 = normal.</summary>
        public float textScale = 1f;

        /// <summary>Values of game-defined options (sliders, toggles, steppers), saved with the rest.</summary>
        public List<SettingsCustomValue> custom = new List<SettingsCustomValue>();

        /// <summary>True once <see cref="GameSettings.ImportLegacyOnce"/> has run, so old values are imported only once.</summary>
        public bool legacyImported;

        public GameSettingsData Clone()
        {
            var copy = (GameSettingsData)MemberwiseClone();
            copy.custom = new List<SettingsCustomValue>();
            if (custom != null)
                foreach (var c in custom)
                    if (c != null) copy.custom.Add(new SettingsCustomValue { key = c.key, value = c.value });
            return copy;
        }

        public bool TryGetCustom(string key, out float value)
        {
            value = 0f;
            if (custom == null || string.IsNullOrEmpty(key)) return false;
            for (int i = 0; i < custom.Count; i++)
            {
                if (custom[i] != null && custom[i].key == key)
                {
                    value = custom[i].value;
                    return true;
                }
            }
            return false;
        }

        public void SetCustom(string key, float value)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (custom == null) custom = new List<SettingsCustomValue>();
            for (int i = 0; i < custom.Count; i++)
            {
                if (custom[i] != null && custom[i].key == key)
                {
                    custom[i].value = value;
                    return;
                }
            }
            custom.Add(new SettingsCustomValue { key = key, value = value });
        }

        public bool RemoveCustom(string key)
        {
            if (custom == null) return false;
            return custom.RemoveAll(c => c != null && c.key == key) > 0;
        }

        public static float ClampTextScale(float scale) =>
            float.IsNaN(scale) || scale <= 0f ? 1f : Mathf.Clamp(scale, MinTextScale, MaxTextScale);

        public string ToJson(bool pretty = true) => JsonUtility.ToJson(this, pretty);

        /// <summary>Returns null when the text is empty or not valid settings JSON.</summary>
        public static GameSettingsData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var data = JsonUtility.FromJson<GameSettingsData>(json);
                if (data == null) return null;
                if (data.bindingOverrides == null) data.bindingOverrides = "";
                data.masterVolume = Mathf.Clamp01(data.masterVolume);
                data.musicVolume = Mathf.Clamp01(data.musicVolume);
                data.sfxVolume = Mathf.Clamp01(data.sfxVolume);
                data.textScale = ClampTextScale(data.textScale);
                if (data.custom == null) data.custom = new List<SettingsCustomValue>();
                return data;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    /// <summary>One saved value of a game-defined option. Toggles are 0/1, steppers the choice index.</summary>
    [Serializable]
    public sealed class SettingsCustomValue
    {
        public string key;
        public float value;
    }

    public enum VolumeChannel
    {
        Master,
        Music,
        Sfx
    }
}
