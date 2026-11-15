#if AMBERSCREEN_DEVCONSOLE
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace AmberScreen.Dev.Tests
{
    /// <summary>0.5.0: open blocking, multi-line readouts, remembered dropdowns, key and sort overrides.</summary>
    public class DevConsoleParityTests
    {
        GameObject m_Host;
        GameObject m_CanvasGo;
        DevConsoleSettings m_Settings;

        [SetUp]
        public void SetUp()
        {
            DevConsole.ResetForTests();
            m_Host = new GameObject("DevConsoleParityTestHost");
            m_Settings = ScriptableObject.CreateInstance<DevConsoleSettings>();
            m_Settings.ApplyCompactDefaults();
            m_Settings.layout = DevConsoleLayout.Rail;
            m_Settings.rememberState = false;
            DevConsoleHost.ReloadCachedSettings(m_Settings);
        }

        [TearDown]
        public void TearDown()
        {
            DevConsole.OpenBlocked = null;
            if (DevConsole.IsOpen)
                DevConsole.Close();
            if (DevConsoleHost.Instance != null)
                Object.DestroyImmediate(DevConsoleHost.Instance.gameObject);
            if (m_CanvasGo != null)
                Object.DestroyImmediate(m_CanvasGo);
            if (m_Host != null)
                Object.DestroyImmediate(m_Host);
            if (m_Settings != null)
                Object.DestroyImmediate(m_Settings);
            m_CanvasGo = null;
            m_Host = null;
            m_Settings = null;
            DevConsole.ResetForTests();
        }

        Canvas MakeCanvas()
        {
            m_CanvasGo = new GameObject("ParityCanvas", typeof(RectTransform));
            var canvas = m_CanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }

        // ---------- open blocking ----------

        [Test]
        public void OpenBlocked_StopsOpening_ButNotClosing()
        {
            bool blocked = true;
            DevConsole.OpenBlocked = () => blocked;
            DevConsole.Register("A", "A1", () => { });

            Assert.IsTrue(DevConsole.IsOpenBlocked);
            DevConsole.Open();
            Assert.IsFalse(DevConsole.IsOpen, "Open");
            DevConsole.Toggle();
            Assert.IsFalse(DevConsole.IsOpen, "Toggle");
            DevConsole.OpenSection("A");
            Assert.IsFalse(DevConsole.IsOpen, "OpenSection");

            blocked = false;
            DevConsole.Open();
            Assert.IsTrue(DevConsole.IsOpen, "opens once unblocked");

            blocked = true;
            DevConsole.OpenSection("A");
            Assert.AreEqual("A", DevConsole.OpenSectionName, "an open console still works");
            DevConsole.Toggle();
            Assert.IsFalse(DevConsole.IsOpen, "closing is never blocked");
        }

        [Test]
        public void OpenBlocked_DefaultNull_NeverBlocks()
        {
            Assert.IsNull(DevConsole.OpenBlocked);
            Assert.IsFalse(DevConsole.IsOpenBlocked);
            DevConsole.Open();
            Assert.IsTrue(DevConsole.IsOpen);
        }

        [Test]
        public void OpenBlocked_Throwing_LogsAndDoesNotBlock()
        {
            DevConsole.OpenBlocked = () => throw new System.InvalidOperationException("parity-boom");
            LogAssert.Expect(LogType.Exception, new Regex("parity-boom"));
            Assert.IsFalse(DevConsole.IsOpenBlocked);
        }

        // ---------- multi-line readouts ----------

        [Test]
        public void MultilineReadout_GrowsWithLines_SingleLineDoesNot()
        {
            var canvas = MakeCanvas();
            DevConsole.RegisterReadout("Info", "One", () => "1");
            DevConsole.RegisterMultilineReadout("Info", "Many", () => "a\nb\nc\nd", 0.5f, 12);
            var one = DevConsoleRegistry.Find("Info", "One");
            var many = DevConsoleRegistry.Find("Info", "Many");
            Assert.IsTrue(many.ReadoutMultiline);
            Assert.IsFalse(one.ReadoutMultiline);

            var wOne = new DevConsoleReadoutWidget(canvas.transform, one, m_Settings, canvas);
            var wMany = new DevConsoleReadoutWidget(canvas.transform, many, m_Settings, canvas);

            Assert.AreEqual(1, wOne.LineCount);
            Assert.AreEqual(5, wMany.LineCount, "label line + 4 value lines");
            Assert.Greater(wMany.PreferredHeight, wOne.PreferredHeight * 2f);
            StringAssert.Contains("a\nb\nc\nd", wMany.Text);
        }

        [Test]
        public void MultilineReadout_CutsAtMaxLines()
        {
            var canvas = MakeCanvas();
            DevConsole.RegisterMultilineReadout("Info", "Cut", () => "1\n2\n3\n4\n5", 0.5f, 2);
            var w = new DevConsoleReadoutWidget(canvas.transform, DevConsoleRegistry.Find("Info", "Cut"), m_Settings, canvas);
            Assert.AreEqual(3, w.LineCount, "label + 2 lines");
            StringAssert.EndsWith("2 ...", w.Text);

            Assert.AreEqual("x\ny ...", DevConsoleReadoutWidget.ClampLines("x\ny\nz", 2, out int lines));
            Assert.AreEqual(2, lines);
            Assert.AreEqual("x", DevConsoleReadoutWidget.ClampLines("x", 3, out lines));
            Assert.AreEqual(1, lines);
        }

        [Test]
        public void MultilineReadout_ReRegisterAsSingleLine_ClearsFlag()
        {
            DevConsole.RegisterMultilineReadout("Info", "R", () => "a\nb");
            DevConsole.RegisterReadout("Info", "R", () => "a");
            Assert.IsFalse(DevConsoleRegistry.Find("Info", "R").ReadoutMultiline);
        }

        // ---------- remembered dropdowns ----------

        [Test]
        public void Choice_RemembersPick_AcrossRebuild_AndCallsBack()
        {
            var canvas = MakeCanvas();
            int picked = -1;
            DevConsole.RegisterChoice("Pick", "Mode", () => new[] { "A", "B", "C" }, i => picked = i);
            var entry = DevConsoleRegistry.Find("Pick", "Mode");

            var w = new DevConsoleDropdownWidget(canvas.transform, entry, m_Settings, canvas);
            Assert.AreEqual(0, w.Value);
            w.Pick(2);
            Assert.AreEqual(2, picked);
            Assert.AreEqual(2, DevConsole.GetChoice("Pick", "Mode"));

            // A rebuild makes a new widget for the same entry: it shows the last pick.
            var again = new DevConsoleDropdownWidget(canvas.transform, entry, m_Settings, canvas);
            Assert.AreEqual(2, again.Value);

            // Re-registering (e.g. a scene reload) keeps the pick too.
            DevConsole.RegisterChoice("Pick", "Mode", () => new[] { "A", "B", "C" });
            Assert.AreEqual(2, DevConsole.GetChoice("Pick", "Mode"));
        }

        [Test]
        public void Choice_HasNoGoButton_DropdownStillHasOne()
        {
            var canvas = MakeCanvas();
            DevConsole.RegisterChoice("Pick", "Choice", () => new[] { "A" });
            DevConsole.RegisterDropdown("Pick", "Run", () => new[] { "A" }, _ => { });
            _ = new DevConsoleDropdownWidget(canvas.transform, DevConsoleRegistry.Find("Pick", "Choice"), m_Settings, canvas);
            _ = new DevConsoleDropdownWidget(canvas.transform, DevConsoleRegistry.Find("Pick", "Run"), m_Settings, canvas);
            Assert.IsNull(canvas.transform.Find("Dropdown_Choice/Controls/Go"));
            Assert.IsNotNull(canvas.transform.Find("Dropdown_Run/Controls/Go"));
        }

        [Test]
        public void Dropdown_KeepsPick_AcrossRebuild_ClampsToOptions()
        {
            var canvas = MakeCanvas();
            string[] options = { "A", "B", "C" };
            DevConsole.RegisterDropdown("Pick", "Run", () => options, _ => { });
            var entry = DevConsoleRegistry.Find("Pick", "Run");
            var w = new DevConsoleDropdownWidget(canvas.transform, entry, m_Settings, canvas);
            w.Pick(1);
            Assert.AreEqual(1, new DevConsoleDropdownWidget(canvas.transform, entry, m_Settings, canvas).Value);

            options = new[] { "Only" };
            entry.DropdownSelected = 2;
            Assert.AreEqual(0, new DevConsoleDropdownWidget(canvas.transform, entry, m_Settings, canvas).Value,
                "pick past the end falls back to the last item");
        }

        [Test]
        public void Choice_DefaultIndex_GetSet_Unknown()
        {
            DevConsole.RegisterChoice("Pick", "D", () => new[] { "A", "B" }, null, 1);
            Assert.AreEqual(1, DevConsole.GetChoice("Pick", "D"));
            Assert.IsTrue(DevConsole.SetChoice("Pick", "D", 0));
            Assert.AreEqual(0, DevConsole.GetChoice("Pick", "D"));
            Assert.AreEqual(-1, DevConsole.GetChoice("Pick", "Nope"));
            Assert.IsFalse(DevConsole.SetChoice("Pick", "Nope", 1));
        }

        [Test]
        public void Choice_InConsole_PickIsReadable()
        {
            DevConsole.RegisterChoice("Pick", "Live", () => new[] { "A", "B" });
            DevConsole.Open();
            DevConsole.OpenSection("Pick");
            var view = DevConsoleHost.View;
            Assert.IsNotNull(view);
            Assert.GreaterOrEqual(view.DropdownWidgets.Count, 1);
            view.DropdownWidgets[view.DropdownWidgets.Count - 1].Pick(1);
            Assert.AreEqual(1, DevConsole.GetChoice("Pick", "Live"));
        }

        // ---------- keys and sort order ----------

        [Test]
        public void KeyOverrides_DefaultToSettings_AndClear()
        {
            Assert.AreEqual(m_Settings.openKey, DevConsole.OpenKey);
            Assert.AreEqual(m_Settings.hudToggleKey, DevConsole.HudToggleKey);
            DevConsole.SetOpenKey(Key.F5);
            DevConsole.SetHudToggleKey(Key.None);
            Assert.AreEqual(Key.F5, DevConsole.OpenKey);
            Assert.AreEqual(Key.None, DevConsole.HudToggleKey);
            DevConsole.ClearOverrides();
            Assert.AreEqual(m_Settings.openKey, DevConsole.OpenKey);
            Assert.AreEqual(m_Settings.hudToggleKey, DevConsole.HudToggleKey);
        }

        [Test]
        public void SortingOrder_DefaultIsSettings_OverrideMovesCanvas()
        {
            Assert.AreEqual(m_Settings.sortingOrder, DevConsole.SortingOrder);
            DevConsole.Open();
            var canvas = DevConsoleHost.Instance.transform.Find("DevConsoleCanvas").GetComponent<Canvas>();
            Assert.AreEqual(m_Settings.sortingOrder, canvas.sortingOrder);

            DevConsole.SetSortingOrder(1050);
            Assert.AreEqual(1050, DevConsole.SortingOrder);
            Assert.AreEqual(1050, canvas.sortingOrder);

            DevConsole.ClearOverrides();
            Assert.AreEqual(m_Settings.sortingOrder, canvas.sortingOrder);
        }

        [Test]
        public void ResetForTests_DropsOverridesAndBlock()
        {
            DevConsole.OpenBlocked = () => true;
            DevConsole.SetOpenKey(Key.F9);
            DevConsole.SetSortingOrder(7);
            DevConsole.ResetForTests();
            DevConsoleHost.ReloadCachedSettings(m_Settings);
            Assert.IsNull(DevConsole.OpenBlocked);
            Assert.AreEqual(m_Settings.openKey, DevConsole.OpenKey);
            Assert.AreEqual(m_Settings.sortingOrder, DevConsole.SortingOrder);
        }
    }
}
#endif
