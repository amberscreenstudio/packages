using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AmberScreen.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AmberScreen.GameShell.Tests
{
    public class GameShellTests
    {
        GameShellSettings m_Settings;
        SaveSettings m_SaveSettings;
        string m_SaveFolder;
        GameObject m_Go;
        float m_TimeScale;
        bool m_AudioPause;

        [SetUp]
        public void SetUp()
        {
            m_TimeScale = Time.timeScale;
            m_AudioPause = AudioListener.pause;
            GameShell.ResetStatics();
            PauseFrame.ResetStatics();
            SceneFlow.ResetStatics();
            GameShellVersion.ResetStatics();

            m_Settings = ScriptableObject.CreateInstance<GameShellSettings>();
            m_Settings.fixInputUpdateMode = false;
            GameShell.UseSettings(m_Settings);

            string sub = "AmberScreenGameShellTests/" + Guid.NewGuid().ToString("N");
            m_SaveSettings = ScriptableObject.CreateInstance<SaveSettings>();
            m_SaveSettings.folderName = sub;
            m_SaveSettings.slotCount = 4;
            m_SaveSettings.ApplyAsDefault();
            m_SaveFolder = SaveStore.Folder;
        }

        [TearDown]
        public void TearDown()
        {
            if (PauseFrame.Instance != null && PauseFrame.IsOpen) PauseFrame.Instance.Close();
            if (m_Go != null) UnityEngine.Object.DestroyImmediate(m_Go);
            GameShell.Shutdown();
            SaveRegistry.Unregister("shell.scene");
            GameShell.ResetStatics();
            PauseFrame.ResetStatics();
            SceneFlow.ResetStatics();
            GameShellVersion.ResetStatics();
            SaveSettings.ClearCache();
            UnityEngine.Object.DestroyImmediate(m_Settings);
            UnityEngine.Object.DestroyImmediate(m_SaveSettings);
            try
            {
                if (Directory.Exists(m_SaveFolder)) Directory.Delete(m_SaveFolder, true);
            }
            catch (IOException) { }
            Time.timeScale = m_TimeScale;
            AudioListener.pause = m_AudioPause;
        }

        PauseFrame MakeFrame()
        {
            m_Go = new GameObject("PauseFrameTest");
            var frame = m_Go.AddComponent<PauseFrame>();
            frame.EnsureBuilt();
            return frame;
        }

        static List<string> Captions(RectTransform content)
        {
            var list = new List<string>();
            foreach (var b in content.GetComponentsInChildren<Button>(true))
                list.Add(b.GetComponentInChildren<Text>(true).text);
            return list;
        }

        static Button FindButton(RectTransform content, string startsWith)
        {
            foreach (var b in content.GetComponentsInChildren<Button>(true))
                if (b.GetComponentInChildren<Text>(true).text.StartsWith(startsWith, StringComparison.Ordinal)) return b;
            return null;
        }

        // ---------- version ----------

        [Test]
        public void Version_SameMajor_NoWarning_OtherMajor_Warns()
        {
            Assert.IsFalse(GameShellVersion.NoteLoaded("0.3.1", "0.3.9"));
            Assert.AreEqual("", GameShellVersion.ActiveWarning());

            LogAssert.Expect(LogType.Warning, new Regex("may be incompatible"));
            Assert.IsTrue(GameShellVersion.NoteLoaded("0.2.0", "0.3.0"));
            StringAssert.Contains("v0.2.0", GameShellVersion.ActiveWarning());
            StringAssert.Contains("\n", GameShellVersion.LabelLine());
        }

        [Test]
        public void Version_EmptySaveVersion_IsLegacy()
        {
            Assert.IsFalse(GameShellVersion.NoteLoaded("", "0.1.5"));
        }

        [Test]
        public void Version_WarningExpires()
        {
            LogAssert.Expect(LogType.Warning, new Regex("gone"));
            GameShellVersion.ShowWarning("gone", -1f);
            Assert.AreEqual("", GameShellVersion.ActiveWarning());
            Assert.AreEqual(GameShellVersion.Label, GameShellVersion.LabelLine());
        }

        // ---------- blocked checks ----------

        [Test]
        public void Blockers_AndGuards()
        {
            bool b = false, g = false;
            Func<bool> blocker = () => b;
            Func<bool> guard = () => g;
            GameShell.AddBlocker(blocker);
            GameShell.AddEscapeGuard(guard);
            Assert.IsFalse(GameShell.BlocksGameplay);
            b = true;
            Assert.IsTrue(GameShell.BlocksGameplay);
            Assert.IsTrue(GameShell.BlocksInput);
            Assert.IsFalse(GameShell.EscapeGuarded);
            g = true;
            Assert.IsTrue(GameShell.EscapeGuarded);
            GameShell.RemoveBlocker(blocker);
            GameShell.RemoveEscapeGuard(guard);
            Assert.IsFalse(GameShell.BlocksGameplay);
            Assert.IsFalse(GameShell.EscapeGuarded);
        }

        [Test]
        public void Blocker_Throwing_IsLoggedAndIgnored()
        {
            GameShell.AddBlocker(() => throw new InvalidOperationException("shell-boom"));
            LogAssert.Expect(LogType.Exception, new Regex("shell-boom"));
            Assert.IsFalse(GameShell.BlocksGameplay);
        }

        [Test]
        public void PauseOpen_BlocksGameplay()
        {
            var frame = MakeFrame();
            frame.Open();
            Assert.IsTrue(GameShell.BlocksGameplay);
            frame.Close();
            Assert.IsFalse(GameShell.BlocksGameplay);
        }

        // ---------- scenes ----------

        [Test]
        public void KnownScenes_SetList_SettingsList()
        {
            SceneFlow.SetKnownScenes("A", "B");
            Assert.IsTrue(SceneFlow.Known("A"));
            Assert.IsFalse(SceneFlow.Known("C"));
            Assert.IsFalse(SceneFlow.Known(""));

            SceneFlow.SetKnownScenes(null);
            m_Settings.knownScenes = new[] { "C" };
            Assert.IsTrue(SceneFlow.Known("C"));
            Assert.IsFalse(SceneFlow.Known("A"));
        }

        [Test]
        public void SceneFlow_FadeUsesThemeAndSortOrder()
        {
            m_Go = new GameObject("FlowTest");
            var flow = m_Go.AddComponent<SceneFlow>();
            flow.EnsureBuilt();
            Assert.AreEqual(5000, flow.FadeCanvas.sortingOrder);
            Assert.AreEqual(0f, flow.Shade.color.a);
            flow.SetAlpha(1f);
            var fade = GameShellTheme.BuiltinDefault.fade;
            Assert.AreEqual(fade.r, flow.Shade.color.r, 1e-4f);
            Assert.AreEqual(1f, flow.Shade.color.a, 1e-4f);
        }

        [Test]
        public void FadeThrough_WithoutSceneFlow_RunsAtOnce()
        {
            bool ran = false;
            SceneFlow.FadeThrough(() => ran = true);
            Assert.IsTrue(ran);
        }

        // ---------- boot ----------

        [Test]
        public void Boot_IsIdempotent_RaisesBootedOnce()
        {
            int booted = 0;
            GameShell.Booted += () => booted++;
            var a = GameShell.Boot();
            var b = GameShell.Boot();
            Assert.AreSame(a, b);
            Assert.AreEqual(1, booted);
            Assert.IsTrue(GameShell.IsBooted);
            Assert.IsNotNull(a.GetComponent<SceneFlow>());
            Assert.IsNotNull(a.GetComponent<PauseFrame>());
            Assert.IsTrue(SaveRegistry.TryGet("shell.scene", out _, out _), "scene provider registered");
            GameShell.Shutdown();
            Assert.IsFalse(GameShell.IsBooted);
        }

        // ---------- pause frame ----------

        [Test]
        public void Open_PausesTimeAndAudio_Close_Restores()
        {
            var frame = MakeFrame();
            Time.timeScale = 0.5f;
            AudioListener.pause = false;
            int opened = 0, closed = 0;
            PauseFrame.Opened += () => opened++;
            PauseFrame.Closed += () => closed++;

            frame.Open();
            Assert.IsTrue(PauseFrame.IsOpen);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);

            frame.Close();
            Assert.IsFalse(PauseFrame.IsOpen);
            Assert.AreEqual(0.5f, Time.timeScale, 1e-5f);
            Assert.IsFalse(AudioListener.pause);
            Assert.AreEqual(1, opened);
            Assert.AreEqual(1, closed);
        }

        [Test]
        public void MainPage_DefaultButtons_SettingsOnlyWithHandler_ExtraButtons()
        {
            var frame = MakeFrame();
            frame.Open();
            CollectionAssert.AreEqual(new[] { "Resume", "Save", "Load", "Quit" }, Captions(frame.Content));
            frame.Close();

            int extra = 0;
            GameShell.OpenSettings = () => { };
            PauseFrame.AddMainButton("Controls", () => extra++);
            frame.Open();
            CollectionAssert.AreEqual(new[] { "Resume", "Save", "Load", "Controls", "Settings", "Quit" }, Captions(frame.Content));
            LogAssert.Expect(LogType.Log, new Regex("Pause click: Controls"));
            FindButton(frame.Content, "Controls").onClick.Invoke();
            Assert.AreEqual(1, extra);
            Assert.IsTrue(PauseFrame.IsOpen, "closesMenu false keeps it open");
        }

        [Test]
        public void MainPage_HidesSaveLoadAndQuit_WhenTurnedOff()
        {
            m_Settings.showSaveLoad = false;
            m_Settings.showQuit = false;
            var frame = MakeFrame();
            frame.Open();
            CollectionAssert.AreEqual(new[] { "Resume" }, Captions(frame.Content));
        }

        [Test]
        public void Back_OpensStepsBackAndCloses_GuardBlocks()
        {
            var frame = MakeFrame();
            bool guarded = true;
            GameShell.AddEscapeGuard(() => guarded);
            frame.HandleBack();
            Assert.IsFalse(PauseFrame.IsOpen, "guard true: Esc belongs to another menu");

            guarded = false;
            frame.HandleBack();
            Assert.IsTrue(PauseFrame.IsOpen);
            frame.ShowPage(PauseFrame.Page.Save);
            frame.HandleBack();
            Assert.AreEqual(PauseFrame.Page.Main, frame.CurrentPage);
            frame.HandleBack();
            Assert.IsFalse(PauseFrame.IsOpen);
        }

        [Test]
        public void SavePage_EmptySlot_SavesThroughHook_FullSlot_AsksFirst()
        {
            var frame = MakeFrame();
            var saved = new List<int>();
            GameShell.SaveSlot = s => { saved.Add(s); return true; };
            frame.Open();
            frame.ShowPage(PauseFrame.Page.Save);
            var captions = Captions(frame.Content);
            CollectionAssert.AreEqual(new[] { "Slot 1  —  empty", "Slot 2  —  empty", "Slot 3  —  empty", "Back" }, captions);

            LogAssert.Expect(LogType.Log, new Regex("Save slot 1"));
            FindButton(frame.Content, "Slot 1").onClick.Invoke();
            CollectionAssert.AreEqual(new[] { 1 }, saved);
            Assert.AreEqual(PauseFrame.Page.Main, frame.CurrentPage);
            Assert.AreEqual("Saved to slot 1", frame.Status);

            // A real file in slot 2 makes the page ask before overwriting.
            Assert.IsTrue(GameShell.DefaultSave(2));
            frame.ShowPage(PauseFrame.Page.Save);
            LogAssert.Expect(LogType.Log, new Regex("Save slot 2"));
            FindButton(frame.Content, "Slot 2").onClick.Invoke();
            Assert.IsNotNull(FindButton(frame.Content, "Overwrite"));
            frame.HandleBack();
            Assert.IsNull(FindButton(frame.Content, "Overwrite"), "Esc leaves the confirm");
            Assert.AreEqual(PauseFrame.Page.Save, frame.CurrentPage);
        }

        [Test]
        public void LoadPage_ListsAutosave_LoadsThroughHook()
        {
            var frame = MakeFrame();
            Assert.IsTrue(GameShell.DefaultSave(0));
            int loaded = -1;
            GameShell.LoadSlot = s => { loaded = s; return new LoadResult { ok = true }; };
            frame.Open();
            frame.ShowPage(PauseFrame.Page.Load);
            Assert.IsNotNull(FindButton(frame.Content, "Autosave"));
            LogAssert.Expect(LogType.Log, new Regex("Load slot 0"));
            FindButton(frame.Content, "Autosave").onClick.Invoke();
            Assert.AreEqual(0, loaded);
            Assert.IsFalse(PauseFrame.IsOpen, "loading closes the pause");
        }

        [Test]
        public void DefaultSaveAndLoad_RoundTrip_SceneProvider()
        {
            GameShell.Boot();
            Assert.IsTrue(GameShell.Save(1));
            StringAssert.DoesNotContain("empty", GameShell.Summary(1));
            var result = GameShell.Load(1);
            Assert.IsTrue(result.ok, result.message);
            Assert.AreEqual(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, GameShell.LastSavedScene);
        }

        [Test]
        public void Summary_UsesHook()
        {
            GameShell.SlotSummary = s => "custom " + s;
            Assert.AreEqual("custom 2", GameShell.Summary(2));
            GameShell.SlotSummary = null;
            Assert.AreEqual("empty", GameShell.Summary(2));
        }

        [Test]
        public void TextScale_ResizesLive()
        {
            var frame = MakeFrame();
            float scale = 1f;
            GameShell.TextScale = () => scale;
            frame.Open();
            Assert.AreEqual(PauseFrame.TitleSize, frame.TitleText.fontSize);
            scale = 1.5f;
            frame.ApplyTextScale();
            Assert.AreEqual(Mathf.RoundToInt(PauseFrame.TitleSize * 1.5f), frame.TitleText.fontSize);
            var resume = FindButton(frame.Content, "Resume").GetComponentInChildren<Text>();
            Assert.AreEqual(Mathf.RoundToInt(PauseFrame.BodySize * 1.5f), resume.fontSize);
        }

        [Test]
        public void ScaledFont_Clamps()
        {
            GameShell.TextScale = () => 100f;
            Assert.AreEqual(64, GameShell.ScaledFont(22));
            GameShell.TextScale = () => float.NaN;
            Assert.AreEqual(22, GameShell.ScaledFont(22));
        }

        [Test]
        public void ManyButtons_PanelFitsAndScrolls()
        {
            for (int i = 0; i < 40; i++) PauseFrame.AddMainButton("Extra " + i, null);
            var frame = MakeFrame();
            frame.Open();
            frame.FitPanel();
            Assert.LessOrEqual(frame.PanelRect.rect.height, frame.MaxPanelHeight() + 1f);
            Assert.IsTrue(frame.IsScrolling);
        }

        [Test]
        public void Theme_SortOrder_Title_Version()
        {
            m_Settings.pauseSortingOrder = 1234;
            m_Settings.pauseTitle = "Menu";
            var theme = ScriptableObject.CreateInstance<GameShellTheme>();
            theme.panel = Color.green;
            m_Settings.theme = theme;
            try
            {
                var frame = MakeFrame();
                frame.Open();
                Assert.AreEqual(1234, frame.MenuCanvas.sortingOrder);
                Assert.AreEqual("Menu", frame.TitleText.text);
                Assert.AreEqual(Color.green, frame.PanelImage.color);
                frame.RefreshVersionLabel();
                Assert.AreEqual(GameShellVersion.Label, frame.VersionText.text);

                var other = ScriptableObject.CreateInstance<GameShellTheme>();
                other.panel = Color.blue;
                frame.ApplyTheme(other);
                Assert.AreEqual(Color.blue, frame.PanelImage.color);
                Assert.AreSame(theme, m_Settings.theme, "ApplyTheme does not change the settings asset");
                UnityEngine.Object.DestroyImmediate(other);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(theme);
            }
        }

        [Test]
        public void MissingReferences_FindsMissingScriptFreeObjectsClean()
        {
            var go = new GameObject("CleanObject");
            try
            {
                var hits = new List<string>();
                Editor.FindMissingReferences.ScanGameObject(go, "test", hits);
                Assert.AreEqual(0, hits.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
