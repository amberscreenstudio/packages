#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            m_Settings.fontOverride = null;
            m_Settings.layout = DevConsoleLayout.Floating;
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

        [Test]
        public void ResolveRegular_LoadsBundledInter()
        {
            DevConsoleHost.ReloadCachedSettings(m_Settings);
            DevConsoleUiUtil.ResetFontCache();

            var font = DevConsoleUiUtil.ResolveRegular(m_Settings);
            Assert.IsNotNull(font);
            Assert.IsNotNull(font.sourceFontFile, "dynamic Inter TMP asset should keep sourceFontFile");
            StringAssert.Contains("Inter", font.sourceFontFile.name);
        }

        [Test]
        public void FloatingHeader_HeightMatchesTitleBar_ChildrenNotTaller()
        {
            DevConsoleHost.ReloadCachedSettings(m_Settings);

            Assert.DoesNotThrow(() =>
            {
                _ = new DevConsoleView(m_Host.transform);
            });

            var canvasTf = m_Host.transform.Find("DevConsoleCanvas");
            Assert.IsNotNull(canvasTf);
            canvasTf.gameObject.SetActive(true);

            var canvas = canvasTf.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var panel = canvasTf.Find("Panel") as RectTransform;
            Assert.IsNotNull(panel);
            panel.gameObject.SetActive(true);

            var header = panel.Find("Header") as RectTransform;
            Assert.IsNotNull(header);

            float expected = DevConsoleUiUtil.TitleBarHeight(m_Settings, canvas);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

            Assert.That(header.rect.height, Is.EqualTo(expected).Within(0.5f));

            for (int i = 0; i < header.childCount; i++)
            {
                var child = header.GetChild(i) as RectTransform;
                if (child == null)
                    continue;
                Assert.That(
                    child.rect.height,
                    Is.LessThanOrEqualTo(header.rect.height + 0.5f),
                    child.name + " taller than header");
            }
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
