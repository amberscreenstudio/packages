#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AmberScreen.Dev.Tests
{
    public class DevConsoleViewBuildTests
    {
        GameObject m_Host;
        DevConsoleSettings m_Settings;

        [SetUp]
        public void SetUp()
        {
            DevConsole.ResetForTests();
            m_Host = new GameObject("DevConsoleViewTestHost");
            m_Settings = ScriptableObject.CreateInstance<DevConsoleSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Host != null)
                Object.DestroyImmediate(m_Host);
            m_Host = null;

            if (m_Settings != null)
                Object.DestroyImmediate(m_Settings);
            m_Settings = null;

            DevConsole.ResetForTests();
        }

        [TestCase(DevConsoleLayout.Floating)]
        [TestCase(DevConsoleLayout.DockRight)]
        [TestCase(DevConsoleLayout.MobilePortrait)]
        [TestCase(DevConsoleLayout.Rail)]
        public void Constructor_StartsClosed_NoExceptions(DevConsoleLayout layout)
        {
            m_Settings.layout = layout;
            DevConsoleHost.ReloadCachedSettings(m_Settings);

            Assert.DoesNotThrow(() =>
            {
                _ = new DevConsoleView(m_Host.transform);
            });

            LogAssert.NoUnexpectedReceived();

            var root = m_Host.transform.Find("DevConsoleCanvas");
            Assert.IsNotNull(root, "DevConsoleCanvas missing after construct");
            Assert.IsFalse(root.gameObject.activeSelf, "console root canvas should start inactive");
        }
    }
}
#endif
