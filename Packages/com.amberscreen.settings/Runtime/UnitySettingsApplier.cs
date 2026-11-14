using UnityEngine;

namespace AmberScreen.Settings
{
    /// <summary>Default applier: QualitySettings, Screen, Application.targetFrameRate and an optional AudioMixer.</summary>
    public class UnitySettingsApplier : ISettingsApplier
    {
        bool m_WarnedMissingParameter;

        public virtual int QualityLevelCount => QualitySettings.names.Length;

        public virtual void CaptureCurrent(GameSettingsData into)
        {
            into.qualityLevel = QualitySettings.GetQualityLevel();
            into.vSync = QualitySettings.vSyncCount > 0;
            into.fullScreenMode = Screen.fullScreenMode;
            into.resolutionWidth = 0;
            into.resolutionHeight = 0;
        }

        public virtual void ApplyGraphics(GameSettingsData data)
        {
            int count = QualityLevelCount;
            if (count > 0)
            {
                int level = Mathf.Clamp(data.qualityLevel, 0, count - 1);
                if (QualitySettings.GetQualityLevel() != level)
                    QualitySettings.SetQualityLevel(level, true);
            }

            // Quality levels carry their own vSync, so set it after the level.
            QualitySettings.vSyncCount = data.vSync ? 1 : 0;
            Application.targetFrameRate = data.targetFrameRate;

            if (Application.isEditor) return; // The Game view ignores resolution and window mode.

            if (data.resolutionWidth > 0 && data.resolutionHeight > 0)
                Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, data.fullScreenMode);
            else if (Screen.fullScreenMode != data.fullScreenMode)
                Screen.fullScreenMode = data.fullScreenMode;
        }

        public virtual void ApplyAudio(GameSettingsData data, SettingsProfile profile)
        {
            if (profile == null || profile.mixer == null)
            {
                // No mixer: master still works through the listener; music/SFX need a mixer.
                AudioListener.volume = Mathf.Clamp01(data.masterVolume);
                return;
            }

            SetMixer(profile, VolumeChannel.Master, data.masterVolume);
            SetMixer(profile, VolumeChannel.Music, data.musicVolume);
            SetMixer(profile, VolumeChannel.Sfx, data.sfxVolume);
        }

        void SetMixer(SettingsProfile profile, VolumeChannel channel, float linear)
        {
            string parameter = profile.GetParameter(channel);
            if (string.IsNullOrEmpty(parameter)) return;
            if (!profile.mixer.SetFloat(parameter, SettingsMath.LinearToDecibels(linear)) && !m_WarnedMissingParameter)
            {
                m_WarnedMissingParameter = true;
                Debug.LogWarning($"[Settings] Mixer '{profile.mixer.name}' has no exposed parameter '{parameter}'. " +
                                 "Expose it in the Audio Mixer window or change the name in the Settings Profile.");
            }
        }
    }
}
