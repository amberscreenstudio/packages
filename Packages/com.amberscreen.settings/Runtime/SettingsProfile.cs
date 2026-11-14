using UnityEngine;
using UnityEngine.Audio;

namespace AmberScreen.Settings
{
    /// <summary>
    /// Per-game settings setup. Put one at <c>Resources/AmberScreenSettingsProfile</c> to have the saved
    /// choices applied automatically on startup; otherwise call <see cref="GameSettings.Initialize"/> yourself.
    /// </summary>
    [CreateAssetMenu(menuName = "AmberScreen/Settings Profile", fileName = "AmberScreenSettingsProfile")]
    public sealed class SettingsProfile : ScriptableObject
    {
        public const string ResourcesName = "AmberScreenSettingsProfile";

        [Header("Startup")]
        [Tooltip("Load and apply the saved choices before the first scene loads (only when this asset is in a Resources folder).")]
        public bool applyOnStartup = true;
        [Tooltip("File name inside Application.persistentDataPath.")]
        public string fileName = "settings.json";

        [Header("Audio mixer (optional)")]
        [Tooltip("Leave empty to drive AudioListener.volume with the master volume only.")]
        public AudioMixer mixer;
        public string masterParameter = "MasterVolume";
        public string musicParameter = "MusicVolume";
        public string sfxParameter = "SfxVolume";

        [Header("Defaults")]
        [Tooltip("-1 = unlimited.")]
        public int defaultTargetFrameRate = -1;
        public float defaultMouseSensitivity = 1f;
        public float minMouseSensitivity = 0.1f;
        public float maxMouseSensitivity = 5f;
        [Tooltip("Choices shown in the frame-limit option. -1 = unlimited.")]
        public int[] frameRateOptions = { 30, 60, 120, -1 };

        [Header("Steam Deck first run")]
        public bool steamDeckPresetOnFirstRun = true;
        [Tooltip("Quality level index used on a Steam Deck's first run (clamped to the levels that exist).")]
        public int steamDeckQualityLevel;
        public int steamDeckTargetFrameRate = 30;

        public string GetParameter(VolumeChannel channel)
        {
            switch (channel)
            {
                case VolumeChannel.Music: return musicParameter;
                case VolumeChannel.Sfx: return sfxParameter;
                default: return masterParameter;
            }
        }
    }
}
