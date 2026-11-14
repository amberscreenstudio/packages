using UnityEngine;

namespace AmberScreen.Settings
{
    public static class SettingsMath
    {
        public const float SilentDecibels = -80f;

        /// <summary>0..1 slider value to mixer decibels (0 = -80 dB, 1 = 0 dB).</summary>
        public static float LinearToDecibels(float linear)
        {
            linear = Mathf.Clamp01(linear);
            if (linear <= 0.0001f) return SilentDecibels;
            return Mathf.Max(SilentDecibels, 20f * Mathf.Log10(linear));
        }

        /// <summary>Picks the option closest to <paramref name="value"/>; -1 (unlimited) only matches itself.</summary>
        public static int ClosestIndex(int[] options, int value)
        {
            if (options == null || options.Length == 0) return -1;
            int best = 0;
            int bestDiff = int.MaxValue;
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == value) return i;
                if (options[i] < 0 || value < 0) continue;
                int diff = Mathf.Abs(options[i] - value);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = i;
                }
            }
            return best;
        }
    }
}
