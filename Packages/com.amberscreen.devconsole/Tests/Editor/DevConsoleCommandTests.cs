#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;
using UnityEngine;

namespace AmberScreen.Dev.Tests
{
    public class DevConsoleCommandTests
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
            m_Settings.commandLine = true;
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
        public void Parse_SplitsNameAndArgs()
        {
            Assert.IsTrue(DevConsoleCommands.TryParse("foo bar baz", out string name, out string[] args));
            Assert.AreEqual("foo", name);
            Assert.AreEqual(2, args.Length);
            Assert.AreEqual("bar", args[0]);
            Assert.AreEqual("baz", args[1]);
        }

        [Test]
        public void Parse_QuotedArgsKeepSpaces()
        {
            Assert.IsTrue(DevConsoleCommands.TryParse("say \"hello world\" x", out string name, out string[] args));
            Assert.AreEqual("say", name);
            Assert.AreEqual(2, args.Length);
            Assert.AreEqual("hello world", args[0]);
            Assert.AreEqual("x", args[1]);
        }

        [Test]
        public void Parse_UnclosedQuoteFails()
        {
            Assert.IsFalse(DevConsoleCommands.TryParse("say \"hello", out _, out _));
        }

        [Test]
        public void Run_UnknownCommand_NotesError()
        {
            Assert.IsFalse(DevConsoleCommands.TryRun("nope 1", out _));
            StringAssert.Contains("Unknown command", DevConsoleRegistry.Status);
        }

        [Test]
        public void Run_RegisteredCommand_Invokes()
        {
            string seen = null;
            DevConsole.RegisterCommand("echo", "Echo args", args =>
            {
                seen = args.Length > 0 ? args[0] : string.Empty;
            });
            Assert.IsTrue(DevConsoleCommands.TryRun("echo hi", out _));
            Assert.AreEqual("hi", seen);
        }

        [Test]
        public void Help_ListsRegisteredCommands()
        {
            DevConsole.RegisterCommand("ping", "Ping help text", _ => { });
            Assert.IsTrue(DevConsoleCommands.TryRun("help", out _));
            StringAssert.Contains("ping", DevConsoleRegistry.Status);
            StringAssert.Contains("Ping help text", DevConsoleRegistry.Status);
            StringAssert.Contains("help", DevConsoleRegistry.Status);
        }

        [Test]
        public void CommandLine_WhenEnabled_ShowsInputOnRail()
        {
            DevConsoleView.SetLayoutScreenOverride(1920, 1080, 160f);
            DevConsole.Register("Sec", "One", () => { });
            DevConsoleHost.EnsureExists();
            DevConsole.Open();

            var cmd = GameObject.Find("CommandLine");
            Assert.IsNotNull(cmd, "CommandLine row missing");
            Assert.IsTrue(cmd.activeInHierarchy);
            Assert.IsNotNull(cmd.GetComponent<TMPro.TMP_InputField>());
            Assert.AreEqual("Rail", cmd.transform.parent.name);
        }

        [Test]
        public void CommandLine_WhenDisabled_Hidden()
        {
            m_Settings.commandLine = false;
            DevConsoleHost.ReloadCachedSettings(m_Settings);
            DevConsoleView.SetLayoutScreenOverride(1920, 1080, 160f);
            DevConsoleHost.EnsureExists();
            DevConsole.Open();
            DevConsoleHost.View?.OnSettingsReloaded();

            var cmd = GameObject.Find("CommandLine");
            if (cmd != null)
                Assert.IsFalse(cmd.activeInHierarchy);
        }
    }
}
#endif
