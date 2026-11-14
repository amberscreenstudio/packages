using System.IO;
using UnityEngine;

namespace AmberScreen.Settings.Demo
{
    /// <summary>
    /// Sandbox demo: buttons to open the menu, fake a Steam Deck and replay the first run, plus a readout.
    /// Saves into the project's Temp folder in the Editor and puts quality, vSync, frame rate and volume back on exit.
    /// </summary>
    public sealed class SettingsDemo : MonoBehaviour
    {
        public SettingsMenu menu;
        [Tooltip("Pretend this machine is a Steam Deck.")]
        public bool fakeSteamDeck;

        int m_PrevQuality;
        int m_PrevVSync;
        int m_PrevFrameRate;
        float m_PrevListenerVolume;
        float m_Fps;
        GUIStyle m_Style;

        void OnEnable()
        {
            m_PrevQuality = QualitySettings.GetQualityLevel();
            m_PrevVSync = QualitySettings.vSyncCount;
            m_PrevFrameRate = Application.targetFrameRate;
            m_PrevListenerVolume = AudioListener.volume;

            if (Application.isEditor)
                GameSettings.DirectoryOverride = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "AmberScreenSettingsDemo"));
            SteamDeckDetector.Override = fakeSteamDeck ? true : (bool?)null;
            GameSettings.Initialize();
            if (menu == null) menu = SettingsMenu.Create(transform);
        }

        void OnDisable()
        {
            if (menu != null && menu.IsOpen) menu.Close();
            SteamDeckDetector.Override = null;
            GameSettings.DirectoryOverride = null;
            if (QualitySettings.GetQualityLevel() != m_PrevQuality) QualitySettings.SetQualityLevel(m_PrevQuality, true);
            QualitySettings.vSyncCount = m_PrevVSync;
            Application.targetFrameRate = m_PrevFrameRate;
            AudioListener.volume = m_PrevListenerVolume;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) m_Fps = Mathf.Lerp(m_Fps, 1f / dt, 0.05f);
        }

        void OnGUI()
        {
            if (m_Style == null)
                m_Style = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };

            GUILayout.BeginArea(new Rect(12, 12, 330, 420), GUI.skin.box);
            GUILayout.Label("<b>Settings demo</b>", m_Style);
            if (GUILayout.Button(menu != null && menu.IsOpen ? "Close settings" : "Open settings", GUILayout.Height(30)))
                menu.Toggle();

            if (GUILayout.Button("Fake Steam Deck: " + (fakeSteamDeck ? "ON" : "OFF"), GUILayout.Height(30)))
            {
                fakeSteamDeck = !fakeSteamDeck;
                SteamDeckDetector.Override = fakeSteamDeck ? true : (bool?)null;
            }

            if (GUILayout.Button("Simulate first run (forget saved settings)", GUILayout.Height(30)))
                GameSettings.ResetAsFirstRun();

            var d = GameSettings.Current;
            if (d != null)
            {
                var names = GameSettings.QualityNames;
                string quality = names.Length > 0 ? names[Mathf.Clamp(d.qualityLevel, 0, names.Length - 1)] : "-";
                GUILayout.Label(
                    $"Steam Deck detected: {(SteamDeckDetector.IsSteamDeck() ? "YES" : "no")}{(fakeSteamDeck ? " (faked)" : "")}\n" +
                    $"Deck preset used on first run: {(d.steamDeckPresetApplied ? "YES" : "no")}\n" +
                    $"Quality: {quality} (level {d.qualityLevel + 1} of {names.Length})\n" +
                    $"VSync: {(d.vSync ? "On" : "Off")}   Frame limit: {(d.targetFrameRate <= 0 ? "Unlimited" : d.targetFrameRate + " FPS")}\n" +
                    $"Running at about {m_Fps:0} FPS\n" +
                    $"Master {d.masterVolume:P0}  Music {d.musicVolume:P0}  Effects {d.sfxVolume:P0}\n" +
                    $"Mouse sensitivity {d.mouseSensitivity:0.00}   Invert Y {(d.invertY ? "On" : "Off")}\n" +
                    $"Saved to: {GameSettings.FilePath}", m_Style);
            }
            GUILayout.EndArea();
        }
    }
}
