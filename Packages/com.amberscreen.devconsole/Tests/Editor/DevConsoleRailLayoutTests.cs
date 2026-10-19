#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev.Tests
{
    public class DevConsoleRailLayoutTests
    {
        GameObject m_Host;
        DevConsoleSettings m_Settings;

        [SetUp]
        public void SetUp()
        {
            DevConsole.ResetForTests();
            m_Host = new GameObject("DevConsoleRailLayoutTestHost");
            m_Settings = ScriptableObject.CreateInstance<DevConsoleSettings>();
            m_Settings.ApplyCompactDefaults();
            m_Settings.layout = DevConsoleLayout.Rail;
            m_Settings.rememberState = false;
            m_Settings.autoColumns = true;
            m_Settings.minEntryWidth = 220f;
            m_Settings.spacing = 4f;
            DevConsoleHost.ReloadCachedSettings(m_Settings);
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

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void Rail_ActiveChildren_StayInsideRail_AndSectionListMatchesViewport(int width, int height)
        {
            DevConsoleView.SetLayoutScreenOverride(width, height);
            DevConsole.Register("LayoutSec", "One", () => { });
            DevConsole.Register("LayoutSec", "Two", () => { });
            DevConsole.RegisterQuick("Q", () => { });

            DevConsoleHost.EnsureExists();
            DevConsole.Open();

            var rail = FindNamedRect("Rail");
            Assert.IsNotNull(rail);
            ForceLayout(rail);

            string offender = FirstChildOutsideHorizontally(rail);
            Assert.IsNull(offender, "child outside rail horizontally: " + offender);

            var sectionScroll = rail.Find("SectionScroll") as RectTransform;
            Assert.IsNotNull(sectionScroll);
            var viewport = sectionScroll.Find("Viewport") as RectTransform;
            var content = viewport != null ? viewport.Find("Content") as RectTransform : null;
            Assert.IsNotNull(viewport);
            Assert.IsNotNull(content);
            Assert.That(content.rect.width, Is.EqualTo(viewport.rect.width).Within(1f));
        }

        [Test]
        public void OpenSection_Landscape_WindowSizedAndEntriesInsideViewport()
        {
            DevConsoleView.SetLayoutScreenOverride(1920, 1080);
            for (int i = 0; i < 12; i++)
                DevConsole.Register("WinSec", "Entry" + i, () => { });

            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            DevConsole.OpenSection("WinSec");

            var canvas = FindNamedRect("DevConsoleCanvas");
            var rail = FindNamedRect("Rail");
            var window = FindNamedRect("SectionWindow");
            Assert.IsNotNull(canvas);
            Assert.IsNotNull(rail);
            Assert.IsNotNull(window);
            Assert.IsTrue(window.gameObject.activeSelf);

            ForceLayout(window);
            ForceLayout(rail);

            float canvasW = canvas.rect.width;
            float canvasH = canvas.rect.height;
            Assert.That(window.rect.width, Is.GreaterThanOrEqualTo(canvasW * 0.6f));
            Assert.That(window.rect.height, Is.GreaterThanOrEqualTo(canvasH * 0.85f));

            var winCorners = new Vector3[4];
            var railCorners = new Vector3[4];
            window.GetWorldCorners(winCorners);
            rail.GetWorldCorners(railCorners);
            Assert.Less(winCorners[2].x, railCorners[0].x + 1f,
                "section window right edge should sit left of the rail");

            var viewport = window.Find("WindowBody/Viewport") as RectTransform;
            var content = viewport != null ? viewport.Find("Content") as RectTransform : null;
            Assert.IsNotNull(viewport);
            Assert.IsNotNull(content);

            string offender = FirstChildOutsideHorizontally(viewport, content);
            Assert.IsNull(offender, "entry outside window viewport: " + offender);

            var grid = content.GetComponent<GridLayoutGroup>();
            Assert.IsNotNull(grid);
            Assert.GreaterOrEqual(grid.constraintCount, 4, "expected multi-column grid at 1920x1080");
        }

        [TestCase(1920, 1080)]
        [TestCase(1080, 1920)]
        public void OpenSection_SectionWindowDrawsAboveRail(int width, int height)
        {
            DevConsoleView.SetLayoutScreenOverride(width, height, 160f);
            DevConsole.Register("DrawOrder", "One", () => { });

            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            DevConsole.OpenSection("DrawOrder");

            var blocker = FindNamedRect("Blocker");
            var rail = FindNamedRect("Rail");
            var railTab = FindNamedRect("RailTab");
            var window = FindNamedRect("SectionWindow");
            Assert.IsNotNull(blocker);
            Assert.IsNotNull(rail);
            Assert.IsNotNull(railTab);
            Assert.IsNotNull(window);

            Assert.Greater(window.GetSiblingIndex(), rail.GetSiblingIndex(),
                "SectionWindow must draw above Rail");
            Assert.Greater(window.GetSiblingIndex(), railTab.GetSiblingIndex(),
                "SectionWindow must draw above RailTab");
            Assert.Less(blocker.GetSiblingIndex(), rail.GetSiblingIndex(),
                "Blocker must stay below Rail so rail stays clickable in landscape");
        }

        [Test]
        public void OpenSection_Portrait_WindowCoversCanvasWithinSafeArea()
        {
            DevConsoleView.SetLayoutScreenOverride(1080, 1920, 160f);
            DevConsole.Register("PortSec", "One", () => { });

            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            DevConsole.OpenSection("PortSec");

            var canvas = FindNamedRect("DevConsoleCanvas");
            var window = FindNamedRect("SectionWindow");
            Assert.IsNotNull(canvas);
            Assert.IsNotNull(window);
            Assert.IsTrue(window.gameObject.activeSelf);

            ForceLayout(window);

            Assert.That(window.rect.width, Is.EqualTo(canvas.rect.width).Within(1f));
            Assert.That(window.rect.height, Is.EqualTo(canvas.rect.height).Within(1f));
        }

        [TestCase(160f)]
        [TestCase(320f)]
        [TestCase(480f)]
        public void Portrait_DpiScale_SheetFillsAndButtonsMeet44Dp(float dpi)
        {
            DevConsoleView.SetLayoutScreenOverride(1080, 1920, dpi);
            for (int i = 0; i < 4; i++)
                DevConsole.Register("DpiSec", "Btn" + i, () => { });

            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            DevConsole.OpenSection("DpiSec");

            var canvasRt = FindNamedRect("DevConsoleCanvas");
            var window = FindNamedRect("SectionWindow");
            Assert.IsNotNull(canvasRt);
            Assert.IsNotNull(window);

            var canvas = canvasRt.GetComponent<Canvas>();
            Assert.IsNotNull(canvas);
            ForceLayout(window);

            Assert.That(window.rect.width, Is.EqualTo(canvasRt.rect.width).Within(1f));
            Assert.That(window.rect.height, Is.EqualTo(canvasRt.rect.height).Within(1f));

            float expectedScale = DevConsoleUiUtil.PortraitScaleFactor(m_Settings);
            Assert.That(canvas.scaleFactor, Is.EqualTo(expectedScale).Within(0.01f));

            float cellH = DevConsoleUiUtil.MinTouch(m_Settings, canvas);
            float heightPx = cellH * canvas.scaleFactor;
            float density = dpi / 160f;
            float minPx = 44f * density;
            // ScaleFactor clamps at 4; MinTouch compensates so 44dp is still met in pixels.
            Assert.GreaterOrEqual(heightPx, minPx - 0.5f,
                $"button height {heightPx}px < 44dp ({minPx}px) at dpi {dpi}");
        }

        static void ForceLayout(RectTransform rt)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            Canvas.ForceUpdateCanvases();
        }

        static RectTransform FindNamedRect(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<RectTransform>() : null;
        }

        static string FirstChildOutsideHorizontally(RectTransform rail)
        {
            var railCorners = new Vector3[4];
            rail.GetWorldCorners(railCorners);
            float left = Mathf.Min(railCorners[0].x, railCorners[1].x);
            float right = Mathf.Max(railCorners[2].x, railCorners[3].x);

            var rects = rail.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                var rt = rects[i];
                if (rt == null || rt == rail || !rt.gameObject.activeInHierarchy)
                    continue;

                var c = new Vector3[4];
                rt.GetWorldCorners(c);
                float cLeft = Mathf.Min(c[0].x, c[1].x);
                float cRight = Mathf.Max(c[2].x, c[3].x);
                if (cLeft < left - 1f || cRight > right + 1f)
                    return rt.name;
            }

            return null;
        }

        static string FirstChildOutsideHorizontally(RectTransform viewport, RectTransform contentRoot)
        {
            var vpCorners = new Vector3[4];
            viewport.GetWorldCorners(vpCorners);
            float left = Mathf.Min(vpCorners[0].x, vpCorners[1].x);
            float right = Mathf.Max(vpCorners[2].x, vpCorners[3].x);

            for (int i = 0; i < contentRoot.childCount; i++)
            {
                var child = contentRoot.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy)
                    continue;

                var c = new Vector3[4];
                child.GetWorldCorners(c);
                float cLeft = Mathf.Min(c[0].x, c[1].x);
                float cRight = Mathf.Max(c[2].x, c[3].x);
                if (cLeft < left - 1f || cRight > right + 1f)
                    return child.name;
            }

            return null;
        }
    }
}
#endif
