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
        public void CollapseButton_FoldsToRailTab_DoesNotClose()
        {
            DevConsoleView.SetLayoutScreenOverride(1920, 1080, 160f);
            DevConsole.Register("Sec", "One", () => { });
            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            Assert.IsTrue(DevConsole.IsOpen);

            var collapseGo = GameObject.Find("Collapse");
            Assert.IsNotNull(collapseGo, "Collapse (−) button missing");
            var btn = collapseGo.GetComponent<Button>();
            Assert.IsNotNull(btn);
            btn.onClick.Invoke();

            Assert.IsTrue(DevConsole.IsOpen, "− must not close the console");
            Assert.IsTrue(DevConsole.IsCollapsed, "− must collapse");
            var railTab = GameObject.Find("RailTab");
            Assert.IsNotNull(railTab);
            Assert.IsTrue(railTab.activeInHierarchy);
            var rail = GameObject.Find("Rail");
            Assert.IsNotNull(rail);
            Assert.IsFalse(rail.activeSelf);
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
