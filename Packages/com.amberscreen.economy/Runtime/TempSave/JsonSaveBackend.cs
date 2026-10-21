using System.IO;
using UnityEngine;

namespace AmberScreen.Economy.TempSave
{
    // replace with com.amberscreen.save
    /// <summary>
    /// Temporary JSON writer under Application.persistentDataPath.
    /// Moves out of Economy when the shared save package lands.
    /// </summary>
    public static class JsonSaveBackend
    {
        public static string SavesDirectory =>
            Path.Combine(Application.persistentDataPath, "saves");

        public static string PathForKey(string key) =>
            Path.Combine(SavesDirectory, key + ".json");

        public static void Write(string key, string json)
        {
            if (string.IsNullOrEmpty(key)) return;
            Directory.CreateDirectory(SavesDirectory);
            File.WriteAllText(PathForKey(key), json ?? "");
            Debug.Log($"[Save] wrote {PathForKey(key)}");
        }

        public static bool TryRead(string key, out string json)
        {
            json = null;
            if (string.IsNullOrEmpty(key)) return false;
            string path = PathForKey(key);
            if (!File.Exists(path)) return false;
            json = File.ReadAllText(path);
            return true;
        }
    }
}
