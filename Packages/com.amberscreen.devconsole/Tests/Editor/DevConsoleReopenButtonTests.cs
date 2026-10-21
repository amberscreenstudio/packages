#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;
using UnityEngine;

namespace AmberScreen.Dev.Tests
{
    public class DevConsoleReopenButtonTests
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
            m_Settings.showReopenButton = true;
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
        [TestCase(1080, 1920)]
        public void ReopenButton_DoesNotIntersectDevHud(int width, int height)
        {
            DevConsoleView.SetLayoutScreenOverride(width, height, 160f);
            DevHud.SetLine("Hud", "X", () => "1");
            DevConsoleHost.EnsureExists();
            DevHud.Visible = true;
            DevConsoleHost.HudView?.Tick(1f);

            // Closed console → reopen button visible.
            if (DevConsole.IsOpen)
                DevConsole.Close();
            DevConsoleHost.View?.OnSettingsReloaded();

            Assert.IsTrue(DevConsoleHost.View != null && DevConsoleHost.View.IsDevButtonVisible);
            var btn = DevConsoleHost.View.GetDevButtonScreenRect();
            var hud = DevHud.RailScreenRect;
            Assert.Greater(btn.width, 1f);
            Assert.Greater(hud.width, 1f);
            Assert.IsFalse(RectsIntersect(btn, hud, 1f),
                $"DEV button {btn} intersects DevHud {hud} at {width}x{height}");
        }

        static bool RectsIntersect(Rect a, Rect b, float tol)
        {
            return a.xMin < b.xMax - tol && b.xMin < a.xMax - tol &&
                   a.yMin < b.yMax - tol && b.yMin < a.yMax - tol;
        }
    }
}
#endif
