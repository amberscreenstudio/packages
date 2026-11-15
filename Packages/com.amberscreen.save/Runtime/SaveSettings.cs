using UnityEngine;

namespace AmberScreen.Save
{
    /// <summary>
    /// Slot count, folder and file names. Put one in a Resources folder named
    /// <c>AmberScreenSaveSettings</c> so it is used in builds too; without one the built-in defaults apply.
    /// </summary>
    [CreateAssetMenu(fileName = ResourcesName, menuName = "Amber Screen/Save Settings", order = 10)]
    public class SaveSettings : ScriptableObject
    {
        /// <summary>Name the asset must have inside a Resources folder.</summary>
        public const string ResourcesName = "AmberScreenSaveSettings";

        [Tooltip("Total slots including autosave at index 0. Valid indices are 0 .. slotCount-1.")]
        public int slotCount = 4;

        [Tooltip("Subfolder under Application.persistentDataPath.")]
        public string folderName = "saves";

        public string fileExtension = ".json";
        public string tempSuffix = ".tmp";
        public string bakSuffix = ".bak";

        [Tooltip("Integer format stamp written into every save. Bump when the envelope shape changes.")]
        public int saveFormat = 3;

        static SaveSettings cached;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cached = null; }

        public static SaveSettings LoadOrDefault()
        {
            if (cached != null) return cached;
            // Resources works in the Editor and in builds.
            cached = Resources.Load<SaveSettings>(ResourcesName);
#if UNITY_EDITOR
            // Editor-only fallback: any SaveSettings asset in the project (old location, any name).
            if (cached == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:SaveSettings");
                for (int i = 0; guids != null && i < guids.Length && cached == null; i++)
                    cached = UnityEditor.AssetDatabase.LoadAssetAtPath<SaveSettings>(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
                if (cached != null)
                    Debug.LogWarning("[Save] Using " + UnityEditor.AssetDatabase.GetAssetPath(cached)
                        + " in the Editor only. Move it to a Resources folder and name it "
                        + ResourcesName + " so builds use it too.");
            }
#endif
            if (cached == null)
            {
                cached = CreateInstance<SaveSettings>();
                cached.name = "SaveSettings (runtime default)";
                cached.hideFlags = HideFlags.DontSave;
            }
            return cached;
        }

        public void ApplyAsDefault() { cached = this; }

        /// <summary>Forgets the cached settings so the next call loads them again (tests, tools).</summary>
        public static void ClearCache() { cached = null; }
    }
}
