using System;
using UnityEngine;

namespace AmberScreen.Settings
{
    /// <summary>Best-guess Steam Deck detection. A heuristic, not a guarantee.</summary>
    public static class SteamDeckDetector
    {
        /// <summary>Set to true/false to fake the answer (demo, tests, QA). Null = really detect.</summary>
        public static bool? Override;

        static readonly string[] k_DeckGpuNames =
        {
            "AMD Custom GPU 0405", // LCD model (Van Gogh APU)
            "AMD Custom GPU 0932", // OLED model (Sephiroth APU)
            "VanGogh",
            "Van Gogh",
            "Sephiroth"
        };

        /// <summary>True when this looks like a Steam Deck (honours <see cref="Override"/>).</summary>
        public static bool IsSteamDeck()
        {
            if (Override.HasValue) return Override.Value;
            int w = 0, h = 0;
            try
            {
                if (Display.main != null)
                {
                    w = Display.main.systemWidth;
                    h = Display.main.systemHeight;
                }
            }
            catch (Exception)
            {
                // Display may be unavailable in batch mode.
            }

            if (w <= 0 || h <= 0)
            {
                w = Screen.currentResolution.width;
                h = Screen.currentResolution.height;
            }

            string env = null;
            try { env = Environment.GetEnvironmentVariable("SteamDeck"); }
            catch (Exception) { }

            return IsSteamDeck(SystemInfo.operatingSystem, SystemInfo.graphicsDeviceName, w, h, env);
        }

        /// <summary>
        /// The heuristic, testable on its own:
        /// <list type="bullet">
        /// <item>Steam sets the <c>SteamDeck=1</c> environment variable on a Deck (also under Proton): yes.</item>
        /// <item>The Deck's own APU GPU name (any OS, so Proton/Windows builds count): yes.</item>
        /// <item>SteamOS or Linux plus a 1280x800 native screen: yes.</item>
        /// </list>
        /// </summary>
        public static bool IsSteamDeck(string operatingSystem, string gpuName, int nativeWidth, int nativeHeight,
            string steamDeckEnvironment = null)
        {
            if (!string.IsNullOrEmpty(steamDeckEnvironment) && steamDeckEnvironment.Trim() == "1")
                return true;

            if (IsDeckGpu(gpuName))
                return true;

            string os = operatingSystem ?? "";
            bool linuxFamily = Contains(os, "steamos") || Contains(os, "linux");
            bool deckScreen = (nativeWidth == 1280 && nativeHeight == 800) || (nativeWidth == 800 && nativeHeight == 1280);
            return linuxFamily && deckScreen;
        }

        public static bool IsDeckGpu(string gpuName)
        {
            if (string.IsNullOrEmpty(gpuName)) return false;
            for (int i = 0; i < k_DeckGpuNames.Length; i++)
                if (Contains(gpuName, k_DeckGpuNames[i]))
                    return true;
            return false;
        }

        static bool Contains(string text, string part) =>
            text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
