using UnityEngine;

namespace AmberScreen.Settings
{
    /// <summary>
    /// Helpers for <see cref="GameSettings.ImportLegacyOnce"/>: read old PlayerPrefs values only when they exist.
    /// <code>
    /// GameSettings.ImportLegacyOnce(d =&gt;
    /// {
    ///     bool any = false;
    ///     if (LegacyPrefs.TryFloat("game.vol", out var v)) { d.masterVolume = v; any = true; }
    ///     if (LegacyPrefs.TryFloat("game.text", out var t)) { d.textScale = t; any = true; }
    ///     return any;
    /// });
    /// </code>
    /// </summary>
    public static class LegacyPrefs
    {
        public static bool TryFloat(string key, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key)) return false;
            value = PlayerPrefs.GetFloat(key, 0f);
            return true;
        }

        public static bool TryInt(string key, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(key) || !PlayerPrefs.HasKey(key)) return false;
            value = PlayerPrefs.GetInt(key, 0);
            return true;
        }

        /// <summary>An int stored as 0/1.</summary>
        public static bool TryBool(string key, out bool value)
        {
            value = false;
            if (!TryInt(key, out int i)) return false;
            value = i != 0;
            return true;
        }
    }
}
