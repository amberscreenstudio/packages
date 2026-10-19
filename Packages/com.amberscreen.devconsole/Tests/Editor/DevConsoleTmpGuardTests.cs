#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AmberScreen.Dev.Tests
{
    public class DevConsoleTmpGuardTests
    {
        [SetUp]
        public void SetUp()
        {
            DevConsole.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (DevConsoleHost.Instance != null)
                UnityEngine.Object.DestroyImmediate(DevConsoleHost.Instance.gameObject);
            DevConsole.ResetForTests();
        }

        [Test]
        public void Bootstrap_WhenTmpNotReady_LogsErrorAndDoesNotThrow()
        {
            DevConsoleHost.TmpReady = () => false;

            LogAssert.Expect(LogType.Error, DevConsoleHost.TmpEssentialsMissingMessage);

            Assert.DoesNotThrow(() => DevConsoleHost.EnsureExists());
            Assert.IsNotNull(DevConsoleHost.Instance);
        }
    }
}
#endif
