#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AmberScreen.Dev.Tests
{
    public class DevConsoleLayoutTests
    {
        GameObject m_Host;
        DevConsoleSettings m_Settings;

        [SetUp]
        public void SetUp()
        {
            DevConsole.ResetForTests();
            m_Host = new GameObject("DevConsoleLayoutTestHost");
            m_Settings = ScriptableObject.CreateInstance<DevConsoleSettings>();
            m_Settings.ApplyCompactDefaults();
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

        [TestCase(1920f, 8)]
        [TestCase(1280f, 5)]
        [TestCase(400f, 1)]
        public void ComputeAutoColumns_MatchesExpected(float contentWidth, int expected)
        {
            int cols = DevConsoleUiUtil.ComputeAutoColumns(contentWidth, 220f, 4f);
            Assert.AreEqual(expected, cols);
        }

        [TestCase(DevConsoleLayout.Floating)]
        [TestCase(DevConsoleLayout.DockRight)]
        [TestCase(DevConsoleLayout.MobilePortrait)]
        public void Constructor_EachLayout_StartsClosed_NoExceptions(DevConsoleLayout layout)
        {
            m_Settings.layout = layout;
            DevConsoleHost.ReloadCachedSettings(m_Settings);

            Assert.DoesNotThrow(() =>
            {
                _ = new DevConsoleView(m_Host.transform);
            });

            LogAssert.NoUnexpectedReceived();

            var root = m_Host.transform.Find("DevConsoleCanvas");
            Assert.IsNotNull(root);
            Assert.IsFalse(root.gameObject.activeSelf);
        }
    }
}
#endif
