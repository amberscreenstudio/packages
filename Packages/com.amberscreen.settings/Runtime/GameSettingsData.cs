using System;
using UnityEngine;

namespace AmberScreen.Settings
{
    /// <summary>Everything the player can choose. Saved as JSON. Plain values only.</summary>
    [Serializable]
    public sealed class GameSettingsData
    {
        public const int CurrentFormat = 1;

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

        public GameSettingsData Clone() => (GameSettingsData)MemberwiseClone();

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
                return data;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public enum VolumeChannel
    {
        Master,
        Music,
        Sfx
    }
}
