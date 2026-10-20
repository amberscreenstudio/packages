#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Dev.Tests
{
    public class DevHudLayoutTests
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
            m_Settings.hudShowOnStart = true;
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

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        [TestCase(1080, 1920)]
        public void OpenSection_DoesNotOverlapHudRail(int width, int height)
        {
            DevConsoleView.SetLayoutScreenOverride(width, height, 160f);
            DevHud.SetLine("Hud", "X", () => "1");
            DevConsole.Register("Sec", "One", () => { });
            DevConsoleHost.EnsureExists();
            DevHud.Visible = true;
            DevConsoleHost.HudView?.Tick(1f);
            DevConsole.Open();
            DevConsole.OpenSection("Sec");

            var hudRail = FindNamedRect("DevHudRail");
            var consoleRail = FindNamedRect("Rail");
            var window = FindNamedRect("SectionWindow");
            Assert.IsNotNull(hudRail);
            Assert.IsNotNull(consoleRail);
            Assert.IsNotNull(window);
            ForceLayout(window);
            ForceLayout(hudRail);
            ForceLayout(consoleRail);

            bool portrait = height > width;
            if (portrait)
            {
                Assert.IsFalse(RectsOverlapVertically(window, hudRail, 1f),
                    "SectionWindow overlaps DevHudRail vertically");
                Assert.IsFalse(RectsOverlapVertically(consoleRail, hudRail, 1f),
                    "console Rail overlaps DevHudRail vertically");

                Assert.GreaterOrEqual(RectMinY(hudRail), RectMaxY(window) - 1f,
                    "HUD should sit above the section window");
                Assert.GreaterOrEqual(RectMinY(hudRail), RectMaxY(consoleRail) - 1f,
                    "HUD should sit above the console rail");

                var canvas = FindNamedRect("DevConsoleCanvas");
                Assert.IsNotNull(canvas);
                Assert.That(RectMinX(consoleRail), Is.EqualTo(RectMinX(canvas)).Within(2f),
                    "portrait console rail left edge should match canvas left");
                Assert.That(RectMaxX(consoleRail),
                    Is.EqualTo(RectMaxX(canvas) - m_Settings.railEdgeMargin).Within(2f),
                    "portrait console rail right edge should sit inset by railEdgeMargin");
            }
            else
            {
                Assert.IsFalse(RectsOverlapHorizontally(window, hudRail, 1f),
                    "SectionWindow overlaps DevHudRail");
                Assert.IsFalse(RectsOverlapHorizontally(consoleRail, hudRail, 1f),
                    "console Rail overlaps DevHudRail");

                Assert.LessOrEqual(RectMaxX(hudRail), RectMinX(window) + 1f,
                    "HUD should sit left of the section window");
                Assert.LessOrEqual(RectMaxX(window), RectMinX(consoleRail) + 1f,
                    "section window should sit left of the console rail");

                float railW = RectMaxX(consoleRail) - RectMinX(consoleRail);
                Assert.GreaterOrEqual(
                    railW,
                    m_Settings.railMinWidth - m_Settings.railEdgeMargin - 1f,
                    "landscape console rail collapsed to a sliver");
            }
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void OverlayRule_GameRectIsFullScreen(int width, int height)
        {
            DevConsoleView.SetLayoutScreenOverride(width, height, 160f);
            DevHud.SetLine("Hud", "X", () => "1");
            DevConsoleHost.EnsureExists();
            DevHud.Visible = true;
            DevConsoleHost.HudView?.Tick(1f);

            var game = DevHud.GameRect;
            Assert.That(game.x, Is.EqualTo(0f).Within(1f));
            Assert.That(game.y, Is.EqualTo(0f).Within(1f));
            Assert.That(game.width, Is.EqualTo(width).Within(1f));
            Assert.That(game.height, Is.EqualTo(height).Within(1f));

            var hud = DevHud.RailScreenRect;
            Assert.Greater(hud.width, 1f);
            Assert.LessOrEqual(hud.xMin, game.xMax + 1f);
            Assert.GreaterOrEqual(hud.xMax, game.xMin - 1f);
        }

        [Test]
        public void WideRule_GameRectCentred_HudInMarginWhenFits()
        {
            DevConsoleView.SetLayoutScreenOverride(2560, 1080, 160f);
            DevHud.SetLine("Hud", "X", () => "1");
            DevConsoleHost.EnsureExists();
            DevHud.Visible = true;
            DevConsoleHost.HudView?.Tick(1f);

            var game = DevHud.GameRect;
            Assert.That(game.width, Is.EqualTo(1920f).Within(1f));
            Assert.That(game.height, Is.EqualTo(1080f).Within(1f));
            Assert.That(game.x, Is.EqualTo(320f).Within(1f));

            float scale = DevHudLayout.SharedUiScaleFactor(m_Settings);
            float hudW = DevHudLayout.HudWidthCanvasUnits(m_Settings, scale) * scale;
            var hud = DevHud.RailScreenRect;
            Assert.LessOrEqual(hud.xMax, game.xMin + 1f);
            Assert.LessOrEqual(hudW, 320f + 1f);
        }

        [Test]
        public void WideRule_NarrowMargin_HudOverlapsGameRect()
        {
            DevConsoleView.SetLayoutScreenOverride(2200, 1080, 160f);
            DevHud.SetLine("Hud", "X", () => "1");
            DevConsoleHost.EnsureExists();
            DevHud.Visible = true;
            DevConsoleHost.HudView?.Tick(1f);

            var game = DevHud.GameRect;
            Assert.That(game.width, Is.EqualTo(1920f).Within(1f));
            Assert.That(game.x, Is.EqualTo((2200f - 1920f) * 0.5f).Within(1f));

            var hud = DevHud.RailScreenRect;
            Assert.Greater(hud.xMax, game.xMin + 1f, "HUD should overlap GameRect when margin is tight");
        }

        [Test]
        public void HudHidden_SectionWindowRegainsWidth()
        {
            DevConsoleView.SetLayoutScreenOverride(1920, 1080, 160f);
            DevHud.SetLine("Hud", "X", () => "1");
            DevConsole.Register("Sec", "One", () => { });
            DevConsoleHost.EnsureExists();
            DevHud.Visible = true;
            DevConsoleHost.HudView?.Tick(1f);
            DevConsole.Open();
            DevConsole.OpenSection("Sec");
            var window = FindNamedRect("SectionWindow");
            ForceLayout(window);
            float withHud = window.rect.width;

            DevHud.Visible = false;
            DevConsoleHost.NotifyHudVisibilityChanged();
            ForceLayout(window);
            float withoutHud = window.rect.width;

            Assert.Greater(withoutHud, withHud + 10f);
        }

        [Test]
        public void SortingOrder_HudBelowConsole()
        {
            DevConsoleView.SetLayoutScreenOverride(1920, 1080, 160f);
            DevHud.SetLine("Hud", "X", () => "1");
            DevConsoleHost.EnsureExists();
            DevConsole.Open();

            var hudCanvas = GameObject.Find("DevHudCanvas")?.GetComponent<Canvas>();
            var consoleCanvas = GameObject.Find("DevConsoleCanvas")?.GetComponent<Canvas>();
            Assert.IsNotNull(hudCanvas);
            Assert.IsNotNull(consoleCanvas);
            Assert.Less(hudCanvas.sortingOrder, consoleCanvas.sortingOrder);
        }

        [Test]
        public void Portrait_WindowHeightLeavesHudTopReserve()
        {
            DevConsoleView.SetLayoutScreenOverride(1080, 1920, 160f);
            DevHud.SetLine("Hud", "X", () => "1");
            DevConsole.Register("Port", "One", () => { });
            DevConsoleHost.EnsureExists();
            DevHud.Visible = true;
            DevConsoleHost.HudView?.Tick(1f);
            DevConsole.Open();
            DevConsole.OpenSection("Port");

            var canvas = FindNamedRect("DevConsoleCanvas");
            var window = FindNamedRect("SectionWindow");
            ForceLayout(window);
            float scale = Mathf.Max(canvas.GetComponent<Canvas>().scaleFactor, 0.01f);
            float reserve = DevHudLayout.HudTopReserveCanvasUnits(m_Settings, scale);
            Assert.That(window.rect.height,
                Is.EqualTo(canvas.rect.height - reserve).Within(2f));
        }

        [Test]
        public void Collapse_ShowsVisibleRailTab()
        {
            DevConsoleView.SetLayoutScreenOverride(1920, 1080, 160f);
            DevConsole.Register("Sec", "One", () => { });
            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            DevConsoleHost.View?.SetCollapsed(true);

            var railTab = FindNamedRect("RailTab");
            var canvas = FindNamedRect("DevConsoleCanvas");
            Assert.IsNotNull(railTab);
            Assert.IsNotNull(canvas);
            Assert.IsTrue(railTab.gameObject.activeInHierarchy, "RailTab should be active when collapsed");
            ForceLayout(railTab);

            Assert.Greater(railTab.rect.width, 0f);
            Assert.Greater(railTab.rect.height, 0f);
            Assert.GreaterOrEqual(RectMinX(railTab), RectMinX(canvas) - 1f);
            Assert.LessOrEqual(RectMaxX(railTab), RectMaxX(canvas) + 1f);
            Assert.GreaterOrEqual(RectMinY(railTab), RectMinY(canvas) - 1f);
            Assert.LessOrEqual(RectMaxY(railTab), RectMaxY(canvas) + 1f);
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

        static float RectMinX(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Mathf.Min(c[0].x, c[1].x, c[2].x, c[3].x);
        }

        static float RectMaxX(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Mathf.Max(c[0].x, c[1].x, c[2].x, c[3].x);
        }

        static float RectMinY(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Mathf.Min(c[0].y, c[1].y, c[2].y, c[3].y);
        }

        static float RectMaxY(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Mathf.Max(c[0].y, c[1].y, c[2].y, c[3].y);
        }

        static bool RectsOverlapHorizontally(RectTransform a, RectTransform b, float tol)
        {
            return RectMinX(a) < RectMaxX(b) - tol && RectMinX(b) < RectMaxX(a) - tol;
        }

        static bool RectsOverlapVertically(RectTransform a, RectTransform b, float tol)
        {
            return RectMinY(a) < RectMaxY(b) - tol && RectMinY(b) < RectMaxY(a) - tol;
        }
    }
}
#endif
