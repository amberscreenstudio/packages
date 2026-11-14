using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Settings.Tests
{
    public sealed class SettingsMenuLayoutTests
    {
        GameObject m_Go;

        [TearDown]
        public void TearDown()
        {
            if (m_Go != null) Object.DestroyImmediate(m_Go);
        }

        SettingsMenu BuildAt(Vector2 screen)
        {
            m_Go = new GameObject("SettingsMenuTest");
            var menu = m_Go.AddComponent<SettingsMenu>();
            menu.Build();

            // Stand in for the screen: give the canvas the size its CanvasScaler would produce.
            var canvas = menu.MenuCanvas;
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = menu.CanvasRect;
            rect.localScale = Vector3.one;
            rect.position = Vector3.zero;
            rect.sizeDelta = SettingsMenu.ScaledCanvasSize(screen);

            menu.FitToCanvas();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(menu.PanelRect);
            return menu;
        }

        static Rect WorldRect(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        static void AssertInside(Rect inner, Rect outer, string what)
        {
            const float eps = 0.01f;
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - eps), what + " left");
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - eps), what + " bottom");
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + eps), what + " right");
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + eps), what + " top");
        }

        [TestCase(1280, 800)]
        [TestCase(3840, 2160)]
        [TestCase(1920, 1080)]
        [TestCase(1280, 480)]
        [TestCase(800, 1280)]
        [TestCase(640, 360)]
        public void PanelAndCloseButtonFitInsideTheScreen(int width, int height)
        {
            var menu = BuildAt(new Vector2(width, height));
            Rect canvas = WorldRect(menu.CanvasRect);
            Rect panel = WorldRect(menu.PanelRect);
            Rect close = WorldRect((RectTransform)menu.CloseButton.transform);

            Assert.That(panel.width, Is.GreaterThan(0f));
            Assert.That(panel.height, Is.GreaterThan(0f));
            AssertInside(panel, canvas, "panel");
            AssertInside(close, panel, "close button");
            Assert.That(panel.width, Is.LessThanOrEqualTo(SettingsMenu.ReferencePanelSize.x + 0.01f));
            Assert.That(panel.height, Is.LessThanOrEqualTo(SettingsMenu.ReferencePanelSize.y + 0.01f));
        }

        [Test]
        public void ShortScreenScrollsInsteadOfOverflowing()
        {
            var menu = BuildAt(new Vector2(1600, 300));
            var scroll = menu.Scroll;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height),
                "options taller than the visible area, so the list scrolls");
            AssertInside(WorldRect(menu.PanelRect), WorldRect(menu.CanvasRect), "panel");
        }

        [Test]
        public void ReferenceScreenShowsEverythingWithoutScrolling()
        {
            var menu = BuildAt(new Vector2(1920, 1080));
            var scroll = menu.Scroll;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            Assert.That(scroll.content.rect.height, Is.LessThanOrEqualTo(scroll.viewport.rect.height + 0.5f));
        }

        [Test]
        public void ScaledCanvasSizeMatchesReferenceAtReferenceScreen()
        {
            var size = SettingsMenu.ScaledCanvasSize(new Vector2(3840, 2160));
            Assert.That(size.x, Is.EqualTo(1920f).Within(0.01f));
            Assert.That(size.y, Is.EqualTo(1080f).Within(0.01f));
        }
    }
}
