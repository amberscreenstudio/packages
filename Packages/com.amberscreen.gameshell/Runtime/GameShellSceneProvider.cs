// Saves which scene the player was in, so a load can go back there (GameShellSettings.restoreSceneOnLoad).
using System;
using AmberScreen.Save;
using UnityEngine.SceneManagement;

namespace AmberScreen.GameShell
{
    [Serializable]
    public class GameShellSceneState
    {
        public string scene = "";
    }

    sealed class GameShellSceneProvider : ISaveStateProvider
    {
        public const string KeyId = "shell.scene";

        public string Key => KeyId;

        public object Capture() => new GameShellSceneState { scene = SceneManager.GetActiveScene().name };

        public void Restore(object state)
        {
            var s = state as GameShellSceneState;
            GameShell.LastSavedScene = s != null ? s.scene ?? "" : "";
        }

        public static void Register()
        {
            SaveRegistry.Unregister(KeyId);
            SaveRegistry.Register(new GameShellSceneProvider(), typeof(GameShellSceneState));
        }
    }
}
