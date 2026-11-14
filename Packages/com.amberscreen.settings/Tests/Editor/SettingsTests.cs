using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace AmberScreen.Settings.Tests
{
    public sealed class SettingsTests
    {
        sealed class FakeApplier : ISettingsApplier
        {
            public int levels = 3;
            public int graphicsApplied;
            public int audioApplied;
            public int QualityLevelCount => levels;

            public void CaptureCurrent(GameSettingsData into)
            {
                into.qualityLevel = 2;
                into.vSync = true;
                into.fullScreenMode = FullScreenMode.FullScreenWindow;
            }

            public void ApplyGraphics(GameSettingsData data) => graphicsApplied++;
            public void ApplyAudio(GameSettingsData data, SettingsProfile profile) => audioApplied++;
        }

        string m_Dir;
        SettingsProfile m_Profile;

        [SetUp]
        public void SetUp()
        {
            m_Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "AmberScreenSettingsTests",
                Guid.NewGuid().ToString("N")));
            m_Profile = ScriptableObject.CreateInstance<SettingsProfile>();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_Dir)) Directory.Delete(m_Dir, true);
            UnityEngine.Object.DestroyImmediate(m_Profile);
            SteamDeckDetector.Override = null;
        }

        SettingsSession NewSession(FakeApplier applier, bool deck) =>
            new SettingsSession(m_Profile, Path.Combine(m_Dir, "settings.json"), applier, () => deck);

        [Test]
        public void SaveAndLoadRoundTrip()
        {
            var first = NewSession(new FakeApplier(), false);
            first.LoadOrCreate();
            Assert.That(first.WasFirstRun, Is.True);
            Assert.That(File.Exists(first.FilePath), Is.True, "first run should save defaults");

            first.ApplyPreset(1);
            first.SetResolution(1600, 900);
            first.SetFullScreenMode(FullScreenMode.Windowed);
            first.SetVSync(false);
            first.SetTargetFrameRate(60);
            first.SetVolume(VolumeChannel.Master, 0.8f);
            first.SetVolume(VolumeChannel.Music, 0.25f);
            first.SetVolume(VolumeChannel.Sfx, 0.5f);
            first.SetMouseSensitivity(2.5f);
            first.SetInvertY(true);
            first.SetBindingOverrides("{\"bindings\":[]}");
            Assert.That(first.Save(), Is.True);

            var second = NewSession(new FakeApplier(), false);
            second.LoadOrCreate();
            var d = second.Data;
            Assert.That(second.WasFirstRun, Is.False);
            Assert.That(d.qualityLevel, Is.EqualTo(1));
            Assert.That(d.resolutionWidth, Is.EqualTo(1600));
            Assert.That(d.resolutionHeight, Is.EqualTo(900));
            Assert.That(d.fullScreenMode, Is.EqualTo(FullScreenMode.Windowed));
            Assert.That(d.vSync, Is.False);
            Assert.That(d.targetFrameRate, Is.EqualTo(60));
            Assert.That(d.masterVolume, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(d.musicVolume, Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(d.sfxVolume, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(d.mouseSensitivity, Is.EqualTo(2.5f).Within(1e-5f));
            Assert.That(d.invertY, Is.True);
            Assert.That(d.bindingOverrides, Is.EqualTo("{\"bindings\":[]}"));
        }

        [Test]
        public void BrokenFileFallsBackToDefaults()
        {
            Directory.CreateDirectory(m_Dir);
            File.WriteAllText(Path.Combine(m_Dir, "settings.json"), "not json {");
            var s = NewSession(new FakeApplier(), false);
            s.LoadOrCreate();
            Assert.That(s.WasFirstRun, Is.True);
            Assert.That(s.Data.qualityLevel, Is.EqualTo(2));
        }

        [TestCase("SteamOS 3.5", "AMD Custom GPU 0405 (RADV VANGOGH)", 1280, 800, null, true)]
        [TestCase("Linux 6.1 SteamOS", "AMD Custom GPU 0932", 1280, 800, null, true)]
        [TestCase("Windows 10 (10.0.19041) 64bit", "AMD Custom GPU 0405", 1280, 800, null, true)] // Proton
        [TestCase("Linux 6.5 Ubuntu", "llvmpipe", 1280, 800, null, true)] // Linux + Deck screen
        [TestCase("Linux 6.5 Ubuntu", "llvmpipe", 800, 1280, null, true)] // portrait panel
        [TestCase("Windows 11 (10.0.22631) 64bit", "NVIDIA GeForce RTX 4080", 2560, 1440, "1", true)] // env var
        [TestCase("Windows 11 (10.0.22631) 64bit", "NVIDIA GeForce RTX 4080", 1280, 800, null, false)]
        [TestCase("Linux 6.5 Ubuntu", "NVIDIA GeForce RTX 3060", 1920, 1080, null, false)]
        [TestCase("Mac OS X 14.0", "Apple M2", 1280, 800, "0", false)]
        [TestCase(null, null, 0, 0, null, false)]
        public void SteamDeckHeuristic(string os, string gpu, int w, int h, string env, bool expected) =>
            Assert.That(SteamDeckDetector.IsSteamDeck(os, gpu, w, h, env), Is.EqualTo(expected));

        [Test]
        public void SteamDeckOverrideWins()
        {
            SteamDeckDetector.Override = true;
            Assert.That(SteamDeckDetector.IsSteamDeck(), Is.True);
            SteamDeckDetector.Override = false;
            Assert.That(SteamDeckDetector.IsSteamDeck(), Is.False);
        }

        [Test]
        public void FirstRunOnSteamDeckPicksLowPresetAnd30Fps()
        {
            m_Profile.steamDeckQualityLevel = 0;
            m_Profile.steamDeckTargetFrameRate = 30;
            var s = NewSession(new FakeApplier(), true);
            s.LoadOrCreate();
            Assert.That(s.Data.qualityLevel, Is.EqualTo(0));
            Assert.That(s.Data.targetFrameRate, Is.EqualTo(30));
            Assert.That(s.Data.vSync, Is.False);
            Assert.That(s.Data.steamDeckPresetApplied, Is.True);
        }

        [Test]
        public void FirstRunOffDeckKeepsCurrentQuality()
        {
            var s = NewSession(new FakeApplier(), false);
            s.LoadOrCreate();
            Assert.That(s.Data.qualityLevel, Is.EqualTo(2));
            Assert.That(s.Data.targetFrameRate, Is.EqualTo(-1));
            Assert.That(s.Data.steamDeckPresetApplied, Is.False);
        }

        [Test]
        public void SteamDeckPresetIndexIsClampedToExistingLevels()
        {
            m_Profile.steamDeckQualityLevel = 7;
            var s = NewSession(new FakeApplier { levels = 1 }, true);
            s.LoadOrCreate();
            Assert.That(s.Data.qualityLevel, Is.EqualTo(0));
        }

        [Test]
        public void ApplyingAPresetSetsLevelAndAppliesGraphics()
        {
            var applier = new FakeApplier();
            var s = NewSession(applier, false);
            s.LoadOrCreate();
            int changed = 0;
            s.Changed += _ => changed++;
            int before = applier.graphicsApplied;

            s.ApplyPreset(1);
            Assert.That(s.Data.qualityLevel, Is.EqualTo(1));
            Assert.That(applier.graphicsApplied, Is.EqualTo(before + 1));
            Assert.That(changed, Is.EqualTo(1));

            s.ApplyPreset(99);
            Assert.That(s.Data.qualityLevel, Is.EqualTo(2), "clamped to the last level");
            s.ApplyPreset(-5);
            Assert.That(s.Data.qualityLevel, Is.EqualTo(0), "clamped to the first level");
        }

        [Test]
        public void ApplySteamDeckPresetLater()
        {
            var s = NewSession(new FakeApplier(), false);
            s.LoadOrCreate();
            s.ApplySteamDeckPreset();
            Assert.That(s.Data.qualityLevel, Is.EqualTo(0));
            Assert.That(s.Data.targetFrameRate, Is.EqualTo(30));
            Assert.That(s.Data.vSync, Is.False);
        }

        [Test]
        public void ControlsChangedCarriesValues()
        {
            var s = NewSession(new FakeApplier(), false);
            s.LoadOrCreate();
            float sens = 0f;
            bool inv = false;
            s.ControlsChanged += (a, b) => { sens = a; inv = b; };
            s.SetMouseSensitivity(100f);
            Assert.That(sens, Is.EqualTo(m_Profile.maxMouseSensitivity));
            s.SetInvertY(true);
            Assert.That(inv, Is.True);
        }

        [Test]
        public void VolumeToDecibels()
        {
            Assert.That(SettingsMath.LinearToDecibels(1f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(SettingsMath.LinearToDecibels(0f), Is.EqualTo(-80f));
            Assert.That(SettingsMath.LinearToDecibels(0.5f), Is.EqualTo(-6.0206f).Within(1e-3f));
        }
    }
}
