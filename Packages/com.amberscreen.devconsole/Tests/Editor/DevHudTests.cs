#if AMBERSCREEN_DEVCONSOLE
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AmberScreen.Dev.Tests
{
    public class DevHudTests
    {
        DevConsoleSettings m_Settings;
        readonly List<string> m_PrefKeys = new List<string>();

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
            for (int i = 0; i < m_PrefKeys.Count; i++)
                PlayerPrefs.DeleteKey(m_PrefKeys[i]);
            m_PrefKeys.Clear();
            DevConsole.ResetForTests();
        }

        // ----- helpers --------------------------------------------------------------------

        string TrackPref(string id)
        {
            string key = DevHudRegistry.CollapsedKey(id);
            PlayerPrefs.DeleteKey(key);
            m_PrefKeys.Add(key);
            return key;
        }

        static DevHudView StartHud()
        {
            DevConsoleHost.EnsureExists();
            var hud = DevConsoleHost.HudView;
            Assert.IsNotNull(hud);
            hud.Tick(1f);
            return hud;
        }

        static List<TextMeshProUGUI> ActiveTexts(DevHudView hud) =>
            hud.Canvas.GetComponentsInChildren<TextMeshProUGUI>(false).ToList();

        static bool HasText(IEnumerable<TextMeshProUGUI> texts, string exact) =>
            texts.Any(t => t.text == exact);

        static TextMeshProUGUI[] TextsUnder(DevHudView hud, string childPath) =>
            hud.Rail.Find(childPath).GetComponentsInChildren<TextMeshProUGUI>(false);

        static Button FirstHeaderButton(DevHudView hud) =>
            hud.Canvas.GetComponentsInChildren<Button>(false).First();

        // ----- legacy API -----------------------------------------------------------------

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
        public void SetLine_MapsOntoSection_IdEqualsName()
        {
            DevHud.SetLine("Legacy", "FPS", () => "60");
            var sec = DevHudRegistry.Sections.Single(s => s.Id == "Legacy");
            Assert.AreEqual("Legacy", sec.Title);
            Assert.AreEqual(1, sec.Rows.Count);
            Assert.AreEqual(1, sec.Rows[0].Cells.Count);
            Assert.AreEqual("FPS", sec.Rows[0].Cells[0].Label);
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
            var hud = StartHud();

            var value = ActiveTexts(hud).FirstOrDefault(t => t.gameObject.name == "HudCellValue");
            Assert.IsNotNull(value);
            StringAssert.Contains("err", value.text);
        }

        [Test]
        public void EmptyLine_NotDrawn_AndEmptySectionHidden()
        {
            DevHud.SetLine("Hide", "Gone", () => string.Empty);
            DevHud.SetLine("Show", "Keep", () => "yes");
            var hud = StartHud();

            var texts = ActiveTexts(hud);
            Assert.IsTrue(HasText(texts, "Show"), "section header drawn");
            Assert.IsTrue(HasText(texts, "Keep:"));
            Assert.IsTrue(HasText(texts, "yes"));
            Assert.IsFalse(HasText(texts, "Hide"), "section with only empty values must not draw");
            Assert.IsFalse(HasText(texts, "Gone:"));
        }

        [Test]
        public void NullGetter_HidesCell()
        {
            DevHud.AddSection("NullVal", "NullVal")
                .AddRow(DevHudCell.Create("Nope", () => null), DevHudCell.Create("Yep", () => "1"));
            var hud = StartHud();

            var texts = ActiveTexts(hud);
            Assert.IsFalse(HasText(texts, "Nope:"));
            Assert.IsTrue(HasText(texts, "Yep:"));
        }

        // ----- sections / rows ------------------------------------------------------------

        [Test]
        public void AddSection_ReturnsHandle_WithIdAndTitle()
        {
            TrackPref("H1");
            var handle = DevHud.AddSection("H1", "Handle One");
            Assert.IsNotNull(handle);
            Assert.AreEqual("H1", handle.Id);
            Assert.AreEqual("Handle One", handle.Title);

            var again = DevHud.AddSection("H1", "Renamed");
            Assert.AreEqual("Renamed", again.Title);
            Assert.AreEqual(1, DevHudRegistry.SectionCount);
        }

        [Test]
        public void AddSection_AddRow_CountsAndRenders()
        {
            TrackPref("Rows");
            var section = DevHud.AddSection("Rows", "Rows Title");
            Assert.AreEqual(0, DevHudRegistry.TotalLineCount());
            Assert.IsFalse(DevHudRegistry.HasHudContent());

            section.AddRow(
                DevHudCell.Create("A", () => "1"),
                DevHudCell.Create("B", () => "2", 2f));
            Assert.AreEqual(1, DevHudRegistry.TotalLineCount());
            Assert.IsTrue(DevHudRegistry.HasHudContent());

            var hud = StartHud();
            var texts = ActiveTexts(hud);
            Assert.IsTrue(HasText(texts, "Rows Title"));
            Assert.IsTrue(HasText(texts, "A:"));
            Assert.IsTrue(HasText(texts, "1"));
            Assert.IsTrue(HasText(texts, "B:"));
            Assert.IsTrue(HasText(texts, "2"));
        }

        [Test]
        public void Cells_UseWeightAsFlexibleWidth()
        {
            TrackPref("Weights");
            DevHud.AddSection("Weights", "Weights")
                .AddRow(DevHudCell.Create("A", () => "1", 1f), DevHudCell.Create("B", () => "2", 3f));
            var hud = StartHud();

            var cells = hud.Canvas.GetComponentsInChildren<LayoutElement>(false)
                .Where(le => le.gameObject.name == "HudCell")
                .Select(le => le.flexibleWidth)
                .ToArray();
            Assert.AreEqual(new[] { 1f, 3f }, cells);
        }

        [Test]
        public void SetLine_IntoExistingSection_ReusesAddSectionOptions()
        {
            TrackPref("Mixed");
            DevHud.AddSection("Mixed", "Mixed Title", Color.red);
            DevHud.SetLine("Mixed", "Legacy", () => "L");
            var sec = DevHudRegistry.Sections.Single(s => s.Id == "Mixed");
            Assert.AreEqual("Mixed Title", sec.Title);
            Assert.AreEqual(Color.red, sec.Tint.Value);
            Assert.AreEqual(1, sec.Rows.Count);

            DevHud.RemoveLine("Mixed", "Legacy");
            Assert.AreEqual(1, DevHudRegistry.SectionCount, "explicit section survives being emptied");
        }

        [Test]
        public void SectionTint_TintsHeader()
        {
            TrackPref("Tint");
            DevHud.AddSection("Tint", "Tint", new Color(1f, 0f, 0f, 1f))
                .AddRow(DevHudCell.Create("A", () => "1"));
            var hud = StartHud();

            var header = hud.Canvas.GetComponentsInChildren<Image>(false)
                .First(i => i.gameObject.name == "HudSectionHeader");
            Assert.Greater(header.color.r, header.color.g, "red tint pulls header toward red");
        }

        [Test]
        public void SectionIcon_AddsIconImage()
        {
            TrackPref("Icon");
            var tex = new Texture2D(2, 2);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.zero);
            try
            {
                DevHud.AddSection("Icon", "Icon", icon: sprite)
                    .AddRow(DevHudCell.Create("A", () => "1"));
                var hud = StartHud();

                var icon = hud.Canvas.GetComponentsInChildren<Image>(false)
                    .FirstOrDefault(i => i.gameObject.name == "HudSectionIcon");
                Assert.IsNotNull(icon);
                Assert.AreSame(sprite, icon.sprite);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void Table_DrawsHeaderAndRows_WithEqualColumns()
        {
            TrackPref("Tbl");
            DevHud.AddSection("Tbl", "Tbl").AddTable(
                new[] { "Name", "HP" },
                () => new[] { new[] { "Orc", "10" }, new[] { "Elf", "7" } });
            Assert.AreEqual(1, DevHudRegistry.TotalLineCount());

            var hud = StartHud();
            var texts = ActiveTexts(hud);
            Assert.IsTrue(HasText(texts, "Name"));
            Assert.IsTrue(HasText(texts, "HP"));
            Assert.IsTrue(HasText(texts, "Orc"));
            Assert.IsTrue(HasText(texts, "10"));
            Assert.IsTrue(HasText(texts, "Elf"));
            Assert.IsTrue(HasText(texts, "7"));

            var headerCells = hud.Canvas.GetComponentsInChildren<LayoutElement>(false)
                .Where(le => le.gameObject.name == "HudTableHeaderCell").ToArray();
            Assert.AreEqual(2, headerCells.Length);
            Assert.AreEqual(headerCells[0].flexibleWidth, headerCells[1].flexibleWidth);
            Assert.AreEqual(headerCells[0].preferredWidth, headerCells[1].preferredWidth);
        }

        [Test]
        public void Table_WithNoRows_IsHidden()
        {
            TrackPref("TblEmpty");
            DevHud.AddSection("TblEmpty", "TblEmpty").AddTable(
                new[] { "Name" }, () => new string[0][]);
            var hud = StartHud();
            Assert.IsFalse(HasText(ActiveTexts(hud), "TblEmpty"), "empty section stays hidden");
        }

        // ----- collapse -------------------------------------------------------------------

        [Test]
        public void HeaderClick_TogglesCollapse_AndPersistsPref()
        {
            string key = TrackPref("Coll");
            DevHud.AddSection("Coll", "Coll").AddRow(DevHudCell.Create("A", () => "val"));
            var hud = StartHud();
            Assert.IsTrue(HasText(ActiveTexts(hud), "val"));

            FirstHeaderButton(hud).onClick.Invoke();
            hud.Tick(1f);

            var texts = ActiveTexts(hud);
            Assert.IsFalse(HasText(texts, "val"), "body hidden when collapsed");
            Assert.IsTrue(HasText(texts, "Coll"), "header stays");
            Assert.AreEqual(1, PlayerPrefs.GetInt(key, 0));
            Assert.AreEqual("AmberScreen.DevHud.Collapsed.Coll", key);

            FirstHeaderButton(hud).onClick.Invoke();
            hud.Tick(1f);
            Assert.IsTrue(HasText(ActiveTexts(hud), "val"));
            Assert.AreEqual(0, PlayerPrefs.GetInt(key, -1));
        }

        [Test]
        public void RememberCollapsed_RestoresFromPrefs()
        {
            string key = TrackPref("Remember");
            PlayerPrefs.SetInt(key, 1);
            DevHud.AddSection("Remember", "Remember", startCollapsed: false);
            Assert.IsTrue(DevHudRegistry.Sections.Single(s => s.Id == "Remember").Collapsed);
        }

        [Test]
        public void RememberCollapsedFalse_IgnoresAndDoesNotWritePrefs()
        {
            string key = TrackPref("NoRemember");
            PlayerPrefs.SetInt(key, 1);
            var handle = DevHud.AddSection(
                "NoRemember", "NoRemember", startCollapsed: false, rememberCollapsed: false);
            Assert.IsFalse(handle.IsCollapsed);

            PlayerPrefs.DeleteKey(key);
            handle.SetCollapsed(true);
            Assert.IsTrue(handle.IsCollapsed);
            Assert.IsFalse(PlayerPrefs.HasKey(key));
        }

        [Test]
        public void StartCollapsed_AppliesWhenNoPref()
        {
            TrackPref("StartColl");
            var handle = DevHud.AddSection("StartColl", "StartColl", startCollapsed: true);
            Assert.IsTrue(handle.IsCollapsed);
        }

        // ----- pinning --------------------------------------------------------------------

        [Test]
        public void PinnedCell_ShowsInStrip_EvenWhenSectionCollapsed()
        {
            TrackPref("Pin");
            var pinned = DevHudCell.Create("P", () => "pinval").Pin();
            DevHud.AddSection("Pin", "Pin").AddRow(pinned, DevHudCell.Create("N", () => "normalval"));
            var hud = StartHud();

            Assert.IsTrue(HasText(TextsUnder(hud, "HudPinnedStrip"), "pinval"));
            Assert.IsFalse(HasText(TextsUnder(hud, "HudSections"), "pinval"), "pinned cell not duplicated");
            Assert.IsTrue(HasText(TextsUnder(hud, "HudSections"), "normalval"));

            FirstHeaderButton(hud).onClick.Invoke();
            hud.Tick(1f);

            Assert.IsTrue(HasText(TextsUnder(hud, "HudPinnedStrip"), "pinval"), "pins survive collapse");
            Assert.IsFalse(HasText(ActiveTexts(hud), "normalval"));
        }

        [Test]
        public void PinnedSection_RendersInStripOnly()
        {
            TrackPref("PinSec");
            DevHud.AddSection("PinSec", "Pinned Sec")
                .AddRow(DevHudCell.Create("A", () => "1"))
                .SetPinned(true);
            var hud = StartHud();

            Assert.IsTrue(HasText(TextsUnder(hud, "HudPinnedStrip"), "Pinned Sec"));
            Assert.IsTrue(HasText(TextsUnder(hud, "HudPinnedStrip"), "1"));
            Assert.IsFalse(HasText(TextsUnder(hud, "HudSections"), "Pinned Sec"));
        }

        [Test]
        public void PinnedStrip_HiddenWhenNothingPinned()
        {
            TrackPref("NoPins");
            DevHud.AddSection("NoPins", "NoPins").AddRow(DevHudCell.Create("A", () => "1"));
            var hud = StartHud();
            Assert.IsFalse(hud.Rail.Find("HudPinnedStrip").gameObject.activeSelf);
        }

        [Test]
        public void HudNotVisible_HidesEverythingIncludingPins()
        {
            TrackPref("Hidden");
            DevHud.AddSection("Hidden", "Hidden")
                .AddRow(DevHudCell.Create("A", () => "1").Pin());
            var hud = StartHud();
            Assert.IsTrue(hud.Rail.gameObject.activeSelf);

            DevHud.Visible = false;
            hud.Tick(1f);
            Assert.IsFalse(hud.Rail.gameObject.activeSelf);
            Assert.AreEqual(Rect.zero, DevHud.RailScreenRect);
        }

        // ----- key hints ------------------------------------------------------------------

        [Test]
        public void KeyHints_DrawKeysSectionForActiveMode()
        {
            var action = new InputAction("Handbrake", InputActionType.Button, "<Keyboard>/space");
            action.Enable();
            try
            {
                DevHud.SetKeyHints("Drive", (action, "Handbrake"));
                DevHud.SetMode("Drive");
                DevConsoleHost.EnsureExists();
                var hud = DevConsoleHost.HudView;
                Assert.IsNotNull(hud);
                hud.Tick(1f);

                var keys = GameObject.Find("HudKeys");
                Assert.IsNotNull(keys);
                Assert.IsTrue(keys.activeInHierarchy);
                var tmp = keys.GetComponent<TMPro.TextMeshProUGUI>();
                Assert.IsNotNull(tmp);
                StringAssert.Contains("Keys", tmp.text);
                StringAssert.Contains("Handbrake", tmp.text);
            }
            finally
            {
                action.Disable();
                action.Dispose();
            }
        }

        [Test]
        public void KeyHints_StringKeys_Draw()
        {
            DevHud.SetKeyHints("Walk", ("WASD", "Move"), ("Space", "Jump"));
            DevHud.SetMode("Walk");
            var hud = StartHud();

            var tmp = GameObject.Find("HudKeys").GetComponent<TextMeshProUGUI>();
            StringAssert.Contains("WASD", tmp.text);
            StringAssert.Contains("Move", tmp.text);
            StringAssert.Contains("Space", tmp.text);
            StringAssert.Contains("Jump", tmp.text);
            Assert.IsTrue(DevHudRegistry.HasHudContent());
            Assert.IsNotNull(hud);
        }

        [Test]
        public void KeyHints_AddKeyHint_MixesActionsAndStrings()
        {
            var action = new InputAction("Fire", InputActionType.Button, "<Keyboard>/f");
            try
            {
                DevHud.SetKeyHints("Mix", ("LMB", "Shoot"));
                DevHud.AddKeyHint("Mix", "R", "Reload");
                DevHud.AddKeyHint("Mix", action, "Fire");
                DevHud.SetMode("Mix");

                Assert.IsTrue(DevHudKeyHints.TryGetActiveHints(out var hints));
                Assert.AreEqual(3, hints.Count);
                Assert.IsNull(hints[0].Action);
                Assert.AreEqual("LMB", hints[0].KeyText);
                Assert.IsNull(hints[1].Action);
                Assert.AreSame(action, hints[2].Action);
                StringAssert.Contains("Reload", DevHudKeyHints.FormatHint(hints[1]));
                StringAssert.Contains("R", DevHudKeyHints.FormatHint(hints[1]));
            }
            finally
            {
                action.Dispose();
            }
        }

        [Test]
        public void KeyHints_HintsOnly_CountAsHudContent()
        {
            Assert.IsFalse(DevHudRegistry.HasHudContent());
            DevHud.AddKeyHint("OnlyKeys", "E", "Use");
            DevHud.SetMode("OnlyKeys");
            Assert.IsTrue(DevHudRegistry.HasHudContent());
            Assert.AreEqual(0, DevHudRegistry.TotalLineCount());
        }

        [Test]
        public void KeyHints_NullAction_WarnsOnce()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[DevHud\].*InputAction is null"));
            DevHud.AddKeyHint("Warn", (InputAction)null, "First");
            DevHud.AddKeyHint("Warn", (InputAction)null, "Second"); // silent: warned once already
            DevHud.SetKeyHints("Warn", ((InputAction)null, "Third"));
            DevHud.SetMode("Warn");

            Assert.IsFalse(DevHudKeyHints.TryGetActiveHints(out _), "null actions are not added");
            LogAssert.NoUnexpectedReceived();
        }

        // ----- theme ----------------------------------------------------------------------

        [Test]
        public void Theme_HudDefaultsMatchCurrentLook()
        {
            var t = DevConsoleTheme.BuiltinDefault;
            Assert.AreEqual(t.hudBackground.color, t.hudPanelColor);
            Assert.AreEqual(t.sectionHeader, t.hudSectionHeaderColor);
            Assert.AreEqual(t.labelColor, t.hudLabelColor);
            Assert.AreEqual(t.valueColor, t.hudValueColor);
            Assert.AreEqual(1f, t.hudOpacity);
            Assert.AreEqual(14f, t.hudHeaderFontSize);
            Assert.AreEqual(4f, t.hudCellPadding);
            Assert.IsNull(t.hudFont);
            Assert.Greater(t.hudPinnedStripColor.r, t.hudPanelColor.r);
        }
    }
}
#endif
