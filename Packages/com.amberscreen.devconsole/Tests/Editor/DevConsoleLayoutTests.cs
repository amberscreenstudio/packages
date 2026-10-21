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
        [TestCase(DevConsoleLayout.Rail)]
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

        [Test]
        public void CollapsedStrip_SectionWindowDoesNotOverlapStrip()
        {
            m_Settings.layout = DevConsoleLayout.Rail;
            m_Settings.rememberState = false;
            DevConsoleHost.ReloadCachedSettings(m_Settings);
            DevConsoleView.SetLayoutScreenOverride(1920, 1080, 160f);
            DevConsole.Register("Alpha", "One", () => { });
            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            DevConsoleHost.View?.SetCollapsed(true);
            DevConsole.OpenSection("Alpha");

            var strip = GameObject.Find("RailStrip")?.GetComponent<RectTransform>();
            var window = GameObject.Find("SectionWindow")?.GetComponent<RectTransform>();
            Assert.IsNotNull(strip);
            Assert.IsNotNull(window);
            LayoutRebuilder.ForceRebuildLayoutImmediate(strip);
            LayoutRebuilder.ForceRebuildLayoutImmediate(window);
            Canvas.ForceUpdateCanvases();

            var sc = new Vector3[4];
            var wc = new Vector3[4];
            strip.GetWorldCorners(sc);
            window.GetWorldCorners(wc);
            float stripMinX = Mathf.Min(sc[0].x, sc[1].x, sc[2].x, sc[3].x);
            float winMaxX = Mathf.Max(wc[0].x, wc[1].x, wc[2].x, wc[3].x);
            Assert.LessOrEqual(winMaxX, stripMinX + 1f,
                "section window must sit left of the collapsed strip");
        }
    }
}
#endif
