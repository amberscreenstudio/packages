#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev.Tests
{
    public class DevConsoleCollapseTests
    {
        DevConsoleSettings m_Settings;

        [SetUp]
        public void SetUp()
        {
            DevConsole.ResetForTests();
            m_Settings = ScriptableObject.CreateInstance<DevConsoleSettings>();
            m_Settings.ApplyCompactDefaults();
            m_Settings.layout = DevConsoleLayout.Rail;
            m_Settings.rememberState = false;
            DevConsoleHost.ReloadCachedSettings(m_Settings);
        }

        [TearDown]
        public void TearDown()
        {
            if (DevConsole.IsOpen)
                DevConsole.Close();
            if (DevConsoleHost.Instance != null)
                UnityEngine.Object.DestroyImmediate(DevConsoleHost.Instance.gameObject);
            if (m_Settings != null)
                UnityEngine.Object.DestroyImmediate(m_Settings);
            m_Settings = null;
            DevConsole.ResetForTests();
        }

        [Test]
        public void CollapseButton_ShowsStripWithSectionIcons()
        {
            DevConsoleView.SetLayoutScreenOverride(1920, 1080, 160f);
            DevConsole.Register("Alpha", "One", () => { });
            DevConsole.Register("Beta", "Two", () => { });
            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            Assert.IsTrue(DevConsole.IsOpen);

            GameObject.Find("Collapse").GetComponent<Button>().onClick.Invoke();

            Assert.IsTrue(DevConsole.IsOpen, "− must not close the console");
            Assert.IsTrue(DevConsole.IsCollapsed, "− must collapse");
            Assert.IsFalse(DevConsole.BlocksGame, "collapsed strip must not block");

            var strip = GameObject.Find("RailStrip");
            Assert.IsNotNull(strip);
            Assert.IsTrue(strip.activeInHierarchy);
            var rail = GameObject.Find("Rail");
            Assert.IsNotNull(rail);
            Assert.IsFalse(rail.activeSelf);

            var icons = strip.transform.Find("Icons");
            Assert.IsNotNull(icons);
            Assert.AreEqual(2, icons.childCount, "one strip button per section");
            Assert.IsNotNull(strip.transform.Find("Expand"));
        }

        [Test]
        public void Collapse_StripIcon_OpensSectionWindow()
        {
            DevConsoleView.SetLayoutScreenOverride(1920, 1080, 160f);
            DevConsole.Register("Alpha", "One", () => { });
            DevConsole.Register("Beta", "Two", () => { });
            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            DevConsoleHost.View?.SetCollapsed(true);

            var icon = GameObject.Find("Strip_Alpha");
            Assert.IsNotNull(icon);
            icon.GetComponent<Button>().onClick.Invoke();

            Assert.IsTrue(DevConsole.IsCollapsed);
            Assert.AreEqual("Alpha", DevConsole.OpenSectionName);
            Assert.IsTrue(DevConsole.BlocksGame);
            var window = GameObject.Find("SectionWindow");
            Assert.IsNotNull(window);
            Assert.IsTrue(window.activeInHierarchy);
        }

        [Test]
        public void Collapse_ExpandButton_RestoresFullRail()
        {
            DevConsoleView.SetLayoutScreenOverride(1920, 1080, 160f);
            DevConsole.Register("Sec", "One", () => { });
            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            DevConsoleHost.View?.SetCollapsed(true);

            var expand = GameObject.Find("Expand");
            Assert.IsNotNull(expand);
            expand.GetComponent<Button>().onClick.Invoke();

            Assert.IsFalse(DevConsole.IsCollapsed);
            Assert.IsTrue(GameObject.Find("Rail").activeSelf);
            Assert.IsFalse(GameObject.Find("RailStrip").activeSelf);
        }

        [Test]
        public void CloseButton_ClosesConsole()
        {
            DevConsoleView.SetLayoutScreenOverride(1920, 1080, 160f);
            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            var closeGo = GameObject.Find("Close");
            Assert.IsNotNull(closeGo);
            closeGo.GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(DevConsole.IsOpen);
        }
    }
}
#endif
