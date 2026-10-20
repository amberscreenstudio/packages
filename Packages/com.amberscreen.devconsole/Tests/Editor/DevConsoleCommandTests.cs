#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;

namespace AmberScreen.Dev.Tests
{
    public class DevConsoleCommandTests
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
    }
}
#endif
