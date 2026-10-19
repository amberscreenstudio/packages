#if AMBERSCREEN_DEVCONSOLE
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev.Tests
{
    public class DevConsoleRailTests
    {
        GameObject m_Host;
        DevConsoleSettings m_Settings;
        readonly List<bool> m_BlocksEvents = new List<bool>();

        [SetUp]
        public void SetUp()
        {
            DevConsole.ResetForTests();
            m_BlocksEvents.Clear();
            m_Host = new GameObject("DevConsoleRailTestHost");
            m_Settings = ScriptableObject.CreateInstance<DevConsoleSettings>();
            m_Settings.ApplyCompactDefaults();
            m_Settings.layout = DevConsoleLayout.Rail;
            m_Settings.rememberState = false;
            DevConsoleHost.ReloadCachedSettings(m_Settings);
            DevConsole.BlocksGameChanged += OnBlocks;
        }

        [TearDown]
        public void TearDown()
        {
            DevConsole.BlocksGameChanged -= OnBlocks;
            if (DevConsole.IsOpen)
                DevConsole.Close();
            if (DevConsoleHost.Instance != null)
                Object.DestroyImmediate(DevConsoleHost.Instance.gameObject);
            if (m_Host != null)
                Object.DestroyImmediate(m_Host);
            m_Host = null;
            if (m_Settings != null)
                Object.DestroyImmediate(m_Settings);
            m_Settings = null;
            DevConsole.ResetForTests();
        }

        void OnBlocks(bool v) => m_BlocksEvents.Add(v);

        [Test]
        public void Rail_Open_DoesNotBlock_InLandscape()
        {
            if (Screen.height > Screen.width)
                Assert.Ignore("Landscape required");

            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            Assert.IsTrue(DevConsole.IsOpen);
            Assert.IsTrue(DevConsole.IsRailOpen);
            Assert.IsFalse(DevConsole.BlocksGame);

            var rail = Object.FindAnyObjectByType<Canvas>();
            Assert.IsNotNull(rail);
            var railRt = GameObject.Find("Rail")?.GetComponent<RectTransform>();
            Assert.IsNotNull(railRt);
            LayoutRebuilder.ForceRebuildLayoutImmediate(railRt);
            float h = railRt.rect.height;
            Assert.Greater(h, 100f);
            Assert.That(railRt.rect.width,
                Is.InRange(m_Settings.railMinWidth - 0.5f, m_Settings.railMaxWidth + 0.5f));
        }

        [Test]
        public void OpenSection_BlocksAndFiresOnce_CloseSection_Unblocks()
        {
            if (Screen.height > Screen.width)
                Assert.Ignore("Landscape required");

            DevConsole.Register("Alpha", "One", () => { });
            DevConsole.Open();
            m_BlocksEvents.Clear();
            Assert.IsFalse(DevConsole.BlocksGame);

            DevConsole.OpenSection("Alpha");
            Assert.IsTrue(DevConsole.BlocksGame);
            Assert.AreEqual("Alpha", DevConsole.OpenSectionName);
            Assert.AreEqual(1, m_BlocksEvents.Count);
            Assert.IsTrue(m_BlocksEvents[0]);

            DevConsole.CloseSection();
            Assert.IsFalse(DevConsole.BlocksGame);
            Assert.IsNull(DevConsole.OpenSectionName);
            Assert.AreEqual(2, m_BlocksEvents.Count);
            Assert.IsFalse(m_BlocksEvents[1]);
        }

        [Test]
        public void OpenSection_AThenB_ShowsOnlyB()
        {
            DevConsole.Register("A", "A1", () => { });
            DevConsole.Register("B", "B1", () => { });
            DevConsole.Open();
            DevConsole.OpenSection("A");
            DevConsole.OpenSection("B");
            Assert.AreEqual("B", DevConsole.OpenSectionName);
            Assert.IsTrue(DevConsole.BlocksGame || DevConsole.IsPortraitRail());
        }

        [Test]
        public void Filter_HidesAndRestoresEntries()
        {
            DevConsole.Register("Filt", "Apple", () => { });
            DevConsole.Register("Filt", "Banana", () => { });
            DevConsole.Open();
            DevConsole.OpenSection("Filt");

            var content = GameObject.Find("Content");
            Assert.IsNotNull(content);
            // Find window content under SectionWindow
            var win = GameObject.Find("SectionWindow");
            Assert.IsNotNull(win);
            var winContent = win.transform.Find("WindowBody/Viewport/Content");
            Assert.IsNotNull(winContent);
            int before = CountActiveChildren(winContent);
            Assert.GreaterOrEqual(before, 2);

            var filter = win.GetComponentInChildren<TMPro.TMP_InputField>();
            Assert.IsNotNull(filter);
            filter.onValueChanged.Invoke("zz");
            Assert.AreEqual(0, CountActiveChildren(winContent));

            filter.onValueChanged.Invoke(string.Empty);
            Assert.GreaterOrEqual(CountActiveChildren(winContent), 2);
        }

        [Test]
        public void EscOrder_WindowThenRail()
        {
            if (Screen.height > Screen.width)
                Assert.Ignore("Landscape required");

            DevConsole.Register("EscSec", "X", () => { });
            DevConsole.Open();
            DevConsole.OpenSection("EscSec");
            Assert.IsTrue(DevConsole.BlocksGame);

            DevConsole.CloseSection();
            Assert.IsTrue(DevConsole.IsOpen);
            Assert.IsFalse(DevConsole.BlocksGame);

            DevConsole.Close();
            Assert.IsFalse(DevConsole.IsOpen);
        }

        [Test]
        public void RegisterQuick_AddsAndRemoves()
        {
            DevConsole.RegisterQuick("Q1", () => { });
            Assert.AreEqual(1, DevConsoleRegistry.Quicks.Count);
            DevConsole.UnregisterQuick("Q1");
            Assert.AreEqual(0, DevConsoleRegistry.Quicks.Count);

            DevConsole.Open();
            DevConsoleHost.NotifyRegistryChanged();
            if (DevConsoleHost.Instance != null)
            {
                // Force rebuild path
                DevConsole.RegisterQuick("Q2", () => { });
                DevConsoleHost.NotifyRegistryChanged();
            }
        }

        [Test]
        public void Portrait_RailBlocksWhenOpen()
        {
            if (Screen.height <= Screen.width)
                Assert.Ignore("Portrait required");

            DevConsole.Open();
            Assert.IsTrue(DevConsole.BlocksGame);
        }

        static int CountActiveChildren(Transform t)
        {
            int n = 0;
            for (int i = 0; i < t.childCount; i++)
            {
                if (t.GetChild(i).gameObject.activeSelf)
                    n++;
            }

            return n;
        }
    }
}
#endif
