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

            Assert.IsFalse(RectsOverlapHorizontally(window, hudRail, 1f),
                "SectionWindow overlaps DevHudRail");
            Assert.IsFalse(RectsOverlapHorizontally(consoleRail, hudRail, 1f),
                "console Rail overlaps DevHudRail");
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
            // Margin is 320px; HUD should sit with left >= game.xMax - 1 when it fits.
            Assert.GreaterOrEqual(hud.xMin, game.xMax - 1f);
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
            Assert.Less(hud.xMin, game.xMax - 1f, "HUD should overlap GameRect when margin is tight");
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
        public void Portrait_WindowWidthLeavesHudReserve()
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
            float reserve = DevHudLayout.HudReserveCanvasUnits(m_Settings, scale);
            Assert.That(window.rect.width,
                Is.EqualTo(canvas.rect.width - reserve).Within(2f));
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

        static bool RectsOverlapHorizontally(RectTransform a, RectTransform b, float tol)
        {
            var ac = new Vector3[4];
            var bc = new Vector3[4];
            a.GetWorldCorners(ac);
            b.GetWorldCorners(bc);
            float aMin = Mathf.Min(ac[0].x, ac[1].x, ac[2].x, ac[3].x);
            float aMax = Mathf.Max(ac[0].x, ac[1].x, ac[2].x, ac[3].x);
            float bMin = Mathf.Min(bc[0].x, bc[1].x, bc[2].x, bc[3].x);
            float bMax = Mathf.Max(bc[0].x, bc[1].x, bc[2].x, bc[3].x);
            return aMin < bMax - tol && bMin < aMax - tol;
        }
    }
}
#endif
