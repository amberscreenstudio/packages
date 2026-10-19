#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;

namespace AmberScreen.DevConsole.Tests
{
    public class DevConsoleRegistryTests
    {
        [SetUp]
        public void SetUp()
        {
            DevConsole.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            DevConsole.ResetForTests();
        }

        [Test]
        public void Register_SameSectionAndLabel_ReplacesEntry()
        {
            int first = 0;
            int second = 0;
            DevConsole.Register("Sec", "Act", () => first++);
            DevConsole.Register("Sec", "Act", () => second++);

            var entry = DevConsoleRegistry.Find("Sec", "Act");
            Assert.IsNotNull(entry);
            entry.Action.Invoke();
            Assert.AreEqual(0, first);
            Assert.AreEqual(1, second);
        }

        [Test]
        public void Register_SameSectionAndLabel_ResetsCaptionToLabel()
        {
            DevConsole.Register("Sec", "Act", () => { });
            DevConsole.SetButtonCaption("Sec", "Act", "Custom");
            DevConsole.Register("Sec", "Act", () => { });
            var entry = DevConsoleRegistry.Find("Sec", "Act");
            Assert.IsNotNull(entry);
            Assert.AreEqual("Act", entry.Caption);
        }

        [Test]
        public void Register_AvailableFalse_MarksEntryUnavailable()
        {
            DevConsole.Register("Sec", "Act", () => { }, available: () => false);
            var entry = DevConsoleRegistry.Find("Sec", "Act");
            Assert.IsNotNull(entry);
            Assert.IsFalse(DevConsoleRegistry.IsAvailable(entry));
        }

        [Test]
        public void Reset_ClearsRegistryAndOpenState()
        {
            DevConsole.Register("Sec", "Act", () => { });
            DevConsole.Note("hello");
            Assert.IsNotNull(DevConsoleRegistry.Find("Sec", "Act"));

            DevConsole.ResetForTests();

            Assert.IsNull(DevConsoleRegistry.Find("Sec", "Act"));
            Assert.IsFalse(DevConsole.IsOpen);
            Assert.IsFalse(DevConsole.BlocksGame);
            Assert.AreEqual(0, DevConsoleRegistry.Notes.Count);
            Assert.AreEqual(string.Empty, DevConsoleRegistry.Status);
        }

        [Test]
        public void SetButtonCaption_ChangesCaption()
        {
            DevConsole.Register("Sec", "Act", () => { });
            DevConsole.SetButtonCaption("Sec", "Act", "New Caption");
            var entry = DevConsoleRegistry.Find("Sec", "Act");
            Assert.IsNotNull(entry);
            Assert.AreEqual("New Caption", entry.Caption);
        }

        [Test]
        public void NotifyRegistryChanged_OnlyMarksDirty()
        {
            Assert.IsFalse(DevConsoleHost.IsRegistryDirty);
            DevConsole.Register("Sec", "Act", () => { });
            Assert.IsTrue(DevConsoleHost.IsRegistryDirty);
            DevConsoleHost.ClearRegistryDirty();
            Assert.IsFalse(DevConsoleHost.IsRegistryDirty);
            DevConsoleHost.NotifyRegistryChanged();
            Assert.IsTrue(DevConsoleHost.IsRegistryDirty);
        }
    }
}
#endif
