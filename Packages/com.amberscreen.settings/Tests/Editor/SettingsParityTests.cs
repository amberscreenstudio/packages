using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Settings.Tests
{
    /// <summary>0.2.0: text size, game-defined options, theme, sort order, Esc signal, legacy import, Deck by name.</summary>
    public sealed class SettingsParityTests
    {
        sealed class NamedApplier : ISettingsApplier, ISettingsQualityNames
        {
            public string[] names = { "High Fidelity", "Balanced", "Performant" };
            public int QualityLevelCount => names.Length;
            public string[] QualityNames => names;
            public void CaptureCurrent(GameSettingsData into) => into.qualityLevel = 0;
            public void ApplyGraphics(GameSettingsData data) { }
            public void ApplyAudio(GameSettingsData data, SettingsProfile profile) { }
        }

        string m_Dir;
        SettingsProfile m_Profile;
        GameObject m_MenuGo;

        [SetUp]
        public void SetUp()
        {
            m_Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "AmberScreenSettingsParityTests",
                Guid.NewGuid().ToString("N")));
            m_Profile = ScriptableObject.CreateInstance<SettingsProfile>();
            GameSettings.ClearCustomOptions();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_MenuGo != null)
            {
                var menu = m_MenuGo.GetComponent<SettingsMenu>();
                if (menu != null && menu.IsOpen) menu.Close();
                UnityEngine.Object.DestroyImmediate(m_MenuGo);
            }
            GameSettings.ClearCustomOptions();
            GameSettings.DirectoryOverride = null;
            SteamDeckDetector.Override = null;
            if (Directory.Exists(m_Dir)) Directory.Delete(m_Dir, true);
            UnityEngine.Object.DestroyImmediate(m_Profile);
        }

        SettingsSession NewSession(bool deck = false, ISettingsApplier applier = null) =>
            new SettingsSession(m_Profile, Path.Combine(m_Dir, "settings.json"), applier ?? new NamedApplier(), () => deck);

        void InitGlobal()
        {
            GameSettings.DirectoryOverride = m_Dir;
            SteamDeckDetector.Override = false;
            GameSettings.Initialize(m_Profile, new NamedApplier());
        }

        [Test]
        public void TextScaleIsClampedAndSaved()
        {
            var s = NewSession();
            s.LoadOrCreate();
            Assert.That(s.Data.textScale, Is.EqualTo(1f));
            s.SetTextScale(5f);
            Assert.That(s.Data.textScale, Is.EqualTo(1.6f));
            s.SetTextScale(0.1f);
            Assert.That(s.Data.textScale, Is.EqualTo(0.75f));
            s.SetTextScale(1.25f);
            s.Save();

            var again = NewSession();
            again.LoadOrCreate();
            Assert.That(again.Data.textScale, Is.EqualTo(1.25f).Within(1e-5f));
        }

        [Test]
        public void CustomValuesAreSavedWithTheRest()
        {
            var s = NewSession();
            s.LoadOrCreate();
            string seenKey = null;
            s.CustomChanged += (k, v) => seenKey = k;
            s.SetCustom("game.loupeHold", 1f);
            s.SetCustom("game.colourBlind", 2f);
            Assert.That(seenKey, Is.EqualTo("game.colourBlind"));
            s.Save();

            var again = NewSession();
            again.LoadOrCreate();
            Assert.That(again.GetCustom("game.loupeHold", 0f), Is.EqualTo(1f));
            Assert.That(again.GetCustom("game.colourBlind", 0f), Is.EqualTo(2f));
            Assert.That(again.GetCustom("missing", 7f), Is.EqualTo(7f));

            var copy = again.Data.Clone();
            copy.SetCustom("game.loupeHold", 0f);
            Assert.That(again.GetCustom("game.loupeHold", 0f), Is.EqualTo(1f), "Clone copies the custom list");
        }

        [Test]
        public void OldFilesWithoutTheNewFieldsStillLoad()
        {
            Directory.CreateDirectory(m_Dir);
            File.WriteAllText(Path.Combine(m_Dir, "settings.json"),
                "{\"format\":1,\"qualityLevel\":1,\"masterVolume\":0.5,\"mouseSensitivity\":2}");
            var s = NewSession();
            s.LoadOrCreate();
            Assert.That(s.WasFirstRun, Is.False);
            Assert.That(s.Data.qualityLevel, Is.EqualTo(1));
            Assert.That(s.Data.textScale, Is.EqualTo(1f));
            Assert.That(s.Data.custom, Is.Not.Null);
            Assert.That(s.Data.legacyImported, Is.False);
        }

        [Test]
        public void SteamDeckLevelIsPickedByName()
        {
            m_Profile.steamDeckQualityName = "Performant";
            m_Profile.steamDeckQualityLevel = 0;
            var s = NewSession(true);
            s.LoadOrCreate();
            Assert.That(s.Data.steamDeckPresetApplied, Is.True);
            Assert.That(s.Data.qualityLevel, Is.EqualTo(2), "\"Performant\" is level 3 of 3");
        }

        [Test]
        public void SteamDeckFallsBackToTheIndexWhenTheNameIsMissing()
        {
            m_Profile.steamDeckQualityName = "No Such Level";
            m_Profile.steamDeckQualityLevel = 1;
            var s = NewSession(true);
            s.LoadOrCreate();
            Assert.That(s.Data.qualityLevel, Is.EqualTo(1));
            Assert.That(m_Profile.steamDeckQualityName, Is.Not.Empty);
            var fresh = ScriptableObject.CreateInstance<SettingsProfile>();
            Assert.That(fresh.steamDeckQualityName, Is.EqualTo("Performant"), "default name");
            UnityEngine.Object.DestroyImmediate(fresh);
        }

        [Test]
        public void LegacyImportRunsOnlyOnce()
        {
            var s = NewSession();
            s.LoadOrCreate();
            int runs = 0;
            bool changed = s.ImportLegacyOnce(d =>
            {
                runs++;
                d.masterVolume = 0.3f;
                d.textScale = 1.4f;
                return true;
            });
            Assert.That(changed, Is.True);
            Assert.That(s.Data.masterVolume, Is.EqualTo(0.3f));
            Assert.That(s.Data.legacyImported, Is.True);

            var again = NewSession();
            again.LoadOrCreate();
            Assert.That(again.ImportLegacyOnce(d => { runs++; return true; }), Is.False);
            Assert.That(runs, Is.EqualTo(1));
            Assert.That(again.Data.textScale, Is.EqualTo(1.4f).Within(1e-5f));

            again.ResetToDefaults();
            Assert.That(again.Data.legacyImported, Is.True, "a reset never re-imports old values");
        }

        [Test]
        public void LegacyPrefsOnlyReadsKeysThatExist()
        {
            const string key = "amberscreen.settings.tests.legacy";
            PlayerPrefs.DeleteKey(key);
            Assert.That(LegacyPrefs.TryFloat(key, out _), Is.False);
            PlayerPrefs.SetFloat(key, 0.4f);
            Assert.That(LegacyPrefs.TryFloat(key, out float v), Is.True);
            Assert.That(v, Is.EqualTo(0.4f).Within(1e-5f));
            PlayerPrefs.DeleteKey(key);
        }

        [Test]
        public void CustomOptionsApplyNowOnChangeAndOnReset()
        {
            InitGlobal();
            float applied = -1f;
            bool toggled = false;
            int step = -1;
            GameSettings.AddCustomSlider("demo.slider", "Demo slider", 0f, 10f, 4f, v => applied = v);
            GameSettings.AddCustomToggle("demo.toggle", "Demo toggle", true, v => toggled = v);
            GameSettings.AddCustomStepper("demo.stepper", "Demo stepper", new[] { "A", "B", "C" }, 1, v => step = v);
            Assert.That(applied, Is.EqualTo(4f), "applied with the default when added");
            Assert.That(toggled, Is.True);
            Assert.That(step, Is.EqualTo(1));

            GameSettings.SetCustom("demo.slider", 25f);
            Assert.That(applied, Is.EqualTo(10f), "clamped to the slider range");
            Assert.That(GameSettings.GetCustom("demo.slider"), Is.EqualTo(10f));
            GameSettings.SetCustom("demo.toggle", false);
            Assert.That(toggled, Is.False);
            Assert.That(GameSettings.GetCustomBool("demo.toggle"), Is.False);
            GameSettings.SetCustom("demo.stepper", 9);
            Assert.That(step, Is.EqualTo(2));

            GameSettings.ResetToDefaults();
            Assert.That(applied, Is.EqualTo(4f));
            Assert.That(toggled, Is.True);
            Assert.That(step, Is.EqualTo(1));
        }

        [Test]
        public void ScaledFontSizeFollowsTextScale()
        {
            Assert.That(GameSettings.ScaledFontSize(20, 1.5f), Is.EqualTo(30));
            Assert.That(GameSettings.ScaledFontSize(20, 0.75f), Is.EqualTo(15));
            Assert.That(GameSettings.ScaledFontSize(20, 9f), Is.EqualTo(32), "clamped to 160%");
            Assert.That(GameSettings.ScaledFontSize(4, 1f), Is.EqualTo(8), "never below 8");
        }

        SettingsMenu NewMenu()
        {
            m_MenuGo = new GameObject("SettingsParityMenu");
            return m_MenuGo.AddComponent<SettingsMenu>();
        }

        static Text FindText(SettingsMenu menu, string value)
        {
            foreach (var t in menu.GetComponentsInChildren<Text>(true))
                if (t.text == value) return t;
            return null;
        }

        [Test]
        public void MenuDrawsAbovePauseMenusByDefault()
        {
            InitGlobal();
            var menu = NewMenu();
            menu.Build();
            Assert.That(menu.sortingOrder, Is.EqualTo(1010));
            menu.sortingOrder = 1200;
            menu.Open();
            Assert.That(menu.MenuCanvas.sortingOrder, Is.EqualTo(1200), "sort order is applied on open");
        }

        [Test]
        public void OpenAndConsumedFlags()
        {
            InitGlobal();
            var menu = NewMenu();
            menu.Build();
            Assert.That(SettingsMenu.AnyOpen, Is.False);
            menu.Open();
            Assert.That(SettingsMenu.AnyOpen, Is.True);
            menu.Close();
            Assert.That(SettingsMenu.AnyOpen, Is.False);
            Assert.That(SettingsMenu.ConsumedCloseThisFrame, Is.False, "only set when Esc closes it");
        }

        [Test]
        public void CustomRowsShowUpAndTheMenuRebuildsForNewOptions()
        {
            InitGlobal();
            GameSettings.AddCustomToggle("demo.first", "First game option", false, section: "Game");
            var menu = NewMenu();
            menu.Build();
            Assert.That(FindText(menu, "First game option"), Is.Not.Null);
            Assert.That(FindText(menu, "Game"), Is.Not.Null, "section heading");

            GameSettings.AddCustomSlider("demo.later", "Added later", 0f, 1f, 0.5f, section: "Accessibility");
            Assert.That(FindText(menu, "Added later"), Is.Null);
            menu.Open();
            Assert.That(FindText(menu, "Added later"), Is.Not.Null, "Open rebuilds when options changed");
            Assert.That(FindText(menu, "Accessibility"), Is.Not.Null);
        }

        [Test]
        public void ThemeColoursAndTitleAreUsed()
        {
            InitGlobal();
            var theme = ScriptableObject.CreateInstance<SettingsMenuTheme>();
            theme.panel = Color.red;
            theme.title = "Options";
            try
            {
                var menu = NewMenu();
                menu.theme = theme;
                menu.Build();
                Assert.That(menu.PanelRect.GetComponent<Image>().color, Is.EqualTo(Color.red));
                Assert.That(FindText(menu, "Options"), Is.Not.Null);

                menu.ApplyTheme(null);
                Assert.That(FindText(menu, "Settings"), Is.Not.Null, "back to the built-in title");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(theme);
            }
        }

        [Test]
        public void MenuTextFollowsTextSize()
        {
            InitGlobal();
            GameSettings.SetTextScale(1.5f);
            var menu = NewMenu();
            menu.Build();
            menu.Open();
            var title = FindText(menu, "Settings");
            Assert.That(title, Is.Not.Null);
            Assert.That(title.fontSize, Is.EqualTo(60), "40 x 150%");
            GameSettings.SetTextScale(1f);
            menu.ApplyTextScale(false); // in Play mode the menu does this itself from GameSettings.Changed
            Assert.That(title.fontSize, Is.EqualTo(40), "live while open");
        }
    }
}
