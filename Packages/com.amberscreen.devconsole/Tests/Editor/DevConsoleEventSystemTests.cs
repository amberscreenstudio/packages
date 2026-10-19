#if AMBERSCREEN_DEVCONSOLE
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AmberScreen.Dev.Tests
{
    public class DevConsoleEventSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            DevConsole.ResetForTests();
            DestroyAllEventSystems();
        }

        [TearDown]
        public void TearDown()
        {
            if (DevConsoleHost.Instance != null)
                Object.DestroyImmediate(DevConsoleHost.Instance.gameObject);
            DestroyAllEventSystems();
            DevConsole.ResetForTests();
        }

        [Test]
        public void EnsureEventSystem_HostCreated_HasNonNullActionsAsset()
        {
            DevConsoleHost.EnsureExists();
            Assert.IsNotNull(DevConsoleHost.Instance);

            DevConsoleHost.Instance.EnsureEventSystem();

            var module = Object.FindAnyObjectByType<InputSystemUIInputModule>();
            Assert.IsNotNull(module);
            Assert.IsNotNull(module.actionsAsset);
        }

        static void DestroyAllEventSystems()
        {
            var systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null)
                    Object.DestroyImmediate(systems[i].gameObject);
            }
        }
    }
}
#endif
