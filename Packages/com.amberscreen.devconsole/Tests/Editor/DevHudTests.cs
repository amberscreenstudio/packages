#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;
using UnityEngine;

namespace AmberScreen.Dev.Tests
{
    public class DevHudTests
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
            m_Settings.hudUpdateHz = 6f;
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
        public void SetLine_Remove_Clear_AndReplace()
        {
            DevHud.SetLine("A", "One", () => "1");
            DevHud.SetLine("A", "Two", () => "2");
            Assert.AreEqual(2, DevHudRegistry.TotalLineCount());

            DevHud.SetLine("A", "One", () => "1b");
            Assert.AreEqual(2, DevHudRegistry.TotalLineCount());

            DevHud.RemoveLine("A", "Two");
            Assert.AreEqual(1, DevHudRegistry.TotalLineCount());

            DevHud.ClearSection("A");
            Assert.AreEqual(0, DevHudRegistry.TotalLineCount());
        }

        [Test]
        public void Ordering_FollowsSectionOrderAndLineOrder()
        {
            DevHud.SetLine("B", "b0", () => "b", order: 0);
            DevHud.SetLine("A", "a1", () => "a", order: 1);
            DevHud.SetLine("A", "a0", () => "a", order: 0);
            DevHud.SetSectionOrder("A", "B");
            DevHudRegistry.EnsureSorted();

            Assert.AreEqual("A", DevHudRegistry.Sections[0].Name);
            Assert.AreEqual("B", DevHudRegistry.Sections[1].Name);
            Assert.AreEqual("a0", DevHudRegistry.Sections[0].Lines[0].Label);
            Assert.AreEqual("a1", DevHudRegistry.Sections[0].Lines[1].Label);
        }

        [Test]
        public void Throttle_RespectsHudUpdateHz()
        {
            m_Settings.hudUpdateHz = 4f;
            DevConsoleHost.ReloadCachedSettings(m_Settings);
            DevHud.SetLine("T", "n", () => "x");
            DevConsoleHost.EnsureExists();
            var hud = DevConsoleHost.HudView;
            Assert.IsNotNull(hud);
            hud.ResetTestCounters();
            hud.OnSettingsOrVisibilityChanged();

            for (int i = 0; i < 20; i++)
                hud.Tick(0.05f);

            Assert.That(hud.ProviderInvokeCount, Is.InRange(3, 5));
        }

        [Test]
        public void SetText_SkippedWhenUnchanged()
        {
            DevHud.SetLine("T", "n", () => "same");
            DevConsoleHost.EnsureExists();
            var hud = DevConsoleHost.HudView;
            Assert.IsNotNull(hud);
            hud.Tick(1f);
            hud.ResetTestCounters();
            hud.Tick(1f);
            Assert.AreEqual(0, hud.SetTextCallCount);
        }

        [Test]
        public void ProviderThrow_ShowsErr()
        {
            DevHud.SetLine("T", "bad", () => throw new System.InvalidOperationException("boom"));
            DevConsoleHost.EnsureExists();
            var hud = DevConsoleHost.HudView;
            Assert.IsNotNull(hud);
            hud.Tick(1f);

            var section = GameObject.Find("HudSection");
            Assert.IsNotNull(section);
            var tmp = section.GetComponent<TMPro.TextMeshProUGUI>();
            Assert.IsNotNull(tmp);
            StringAssert.Contains("err", tmp.text);
        }
    }
}
#endif
