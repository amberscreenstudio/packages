namespace AmberScreen.Settings
{
    /// <summary>Pushes settings into the engine. Swap it out in tests or to add game-specific steps.</summary>
    public interface ISettingsApplier
    {
        /// <summary>How many quality levels exist (QualitySettings.names.Length).</summary>
        int QualityLevelCount { get; }

        /// <summary>Fills a fresh data object with what the engine is using right now.</summary>
        void CaptureCurrent(GameSettingsData into);

        void ApplyGraphics(GameSettingsData data);
        void ApplyAudio(GameSettingsData data, SettingsProfile profile);
    }
}
