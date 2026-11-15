// Game shell options. Put one named AmberScreenGameShellSettings in a Resources folder, or pass one to GameShell.Boot.
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberScreen.GameShell
{
    [CreateAssetMenu(menuName = "AmberScreen/Game Shell Settings", fileName = ResourcesName)]
    public class GameShellSettings : ScriptableObject
    {
        public const string ResourcesName = "AmberScreenGameShellSettings";

        [Tooltip("Empty = the built-in look.")]
        public GameShellTheme theme;

        [Header("Boot")]
        [Tooltip("Boot the shell before the first scene loads. Off = the game calls GameShell.Boot() itself.")]
        public bool autoBoot;

        [Header("Scenes")]
        [Tooltip("Scenes SceneFlow.Go may load. Empty = any scene in Build Settings.")]
        public string[] knownScenes = new string[0];
        public float fadeSeconds = 0.3f;
        public int fadeSortingOrder = 5000;
        [Tooltip("Write the autosave slot before each scene change.")]
        public bool autosaveOnSceneChange = true;
        [Tooltip("After loading a slot, go to the scene it was saved in (if known).")]
        public bool restoreSceneOnLoad;

        [Header("Pause")]
        public bool pauseEnabled = true;
        public Key pauseKey = Key.Escape;
        [Tooltip("Gamepad Start opens/closes, East (B) steps back.")]
        public bool gamepadPause = true;
        public string pauseTitle = "Paused";
        public int pauseSortingOrder = 1000;
        public bool showSaveLoad = true;
        public bool showQuit = true;
        [Tooltip("Switch the Input System to Dynamic update so menus work at timeScale 0.")]
        public bool fixInputUpdateMode = true;

        [Header("Version label")]
        public bool showVersionLabel = true;
        public int versionSortingOrder = 1100;
        public float versionWarningSeconds = 8f;

        static GameShellSettings s_Cached;

        public GameShellTheme Theme => theme != null ? theme : GameShellTheme.BuiltinDefault;

        /// <summary>Resources copy, else a default instance.</summary>
        public static GameShellSettings LoadOrDefault()
        {
            if (s_Cached != null) return s_Cached;
            s_Cached = Resources.Load<GameShellSettings>(ResourcesName);
            if (s_Cached == null)
            {
                s_Cached = CreateInstance<GameShellSettings>();
                s_Cached.name = ResourcesName + " (default)";
                s_Cached.hideFlags = HideFlags.DontSave;
            }
            return s_Cached;
        }

        public void ApplyAsDefault() { s_Cached = this; }

        public static void ClearCache() { s_Cached = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_Cached = null; }
    }
}
