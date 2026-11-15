// Slot files, atomic write (tmp → replace, keep .bak), version stamp, provider capture/restore.
using System;
using System.IO;
using UnityEngine;

namespace AmberScreen.Save
{
    public static class SaveStore
    {
        public const int AutosaveSlot = 0;

        public static SaveSettings Settings { get { return SaveSettings.LoadOrDefault(); } }

        public static int SlotCount
        {
            get
            {
                int n = Settings != null ? Settings.slotCount : 4;
                return n < 1 ? 1 : n;
            }
        }

        public static string Folder
        {
            get
            {
                string name = Settings != null && !string.IsNullOrEmpty(Settings.folderName)
                    ? Settings.folderName : "saves";
                return Path.Combine(Application.persistentDataPath, name);
            }
        }

        public static int SaveFormat
        {
            get { return Settings != null ? Settings.saveFormat : 3; }
        }

        public static bool Valid(int slot)
        {
            return slot >= 0 && slot < SlotCount;
        }

        public static string PathFor(int slot)
        {
            string ext = Settings != null && !string.IsNullOrEmpty(Settings.fileExtension)
                ? Settings.fileExtension : ".json";
            if (!ext.StartsWith(".")) ext = "." + ext;
            return Path.Combine(Folder, "slot" + slot + ext);
        }

        public static string TempPath(int slot)
        {
            string suf = Settings != null && !string.IsNullOrEmpty(Settings.tempSuffix)
                ? Settings.tempSuffix : ".tmp";
            return PathFor(slot) + suf;
        }

        public static string BakPath(int slot)
        {
            string suf = Settings != null && !string.IsNullOrEmpty(Settings.bakSuffix)
                ? Settings.bakSuffix : ".bak";
            return PathFor(slot) + suf;
        }

        public static bool Exists(int slot)
        {
            return Valid(slot) && File.Exists(PathFor(slot));
        }

        public static bool Save(int slot, SaveEnvelope envelope, out string error)
        {
            error = "";
            if (!Valid(slot))
            {
                error = "Bad slot " + slot + " (valid 0.." + (SlotCount - 1) + ")";
                Debug.LogWarning("[Save] " + error);
                return false;
            }
            if (envelope == null)
            {
                error = "Null envelope";
                return false;
            }
            try
            {
                Directory.CreateDirectory(Folder);
                envelope.gameVersion = Application.version;
                envelope.saveFormat = SaveFormat;
                SaveRegistry.CaptureAll(envelope);
                string path = PathFor(slot);
                string tmp = TempPath(slot);
                File.WriteAllText(tmp, envelope.ToJson(true));
                // Ensure bytes hit disk before the swap (best-effort on Windows).
                using (var fs = new FileStream(tmp, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    fs.Flush(true);
                ReplaceKeepingBak(tmp, path, BakPath(slot));
                Debug.Log("[Save] Slot " + slot + " → " + path);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                Debug.LogError("[Save] Slot " + slot + " failed: " + e.Message);
                return false;
            }
        }

        public static LoadResult Load(int slot, Func<string, SaveEnvelope> migrateLegacy)
        {
            var result = new LoadResult();
            if (!Valid(slot))
            {
                result.message = "Bad slot " + slot;
                Debug.LogWarning("[Save] " + result.message);
                return result;
            }
            string path = PathFor(slot);
            string bak = BakPath(slot);
            string json = null;
            string source = null;
            if (File.Exists(path))
            {
                try
                {
                    json = File.ReadAllText(path);
                    source = path;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Save] Main file unreadable (" + e.Message + "); trying .bak");
                }
            }
            if (json == null && File.Exists(bak))
            {
                try
                {
                    json = File.ReadAllText(bak);
                    source = bak;
                    Debug.LogWarning("[Save] Loaded slot " + slot + " from backup " + bak);
                }
                catch (Exception e)
                {
                    result.message = "Main and .bak failed: " + e.Message;
                    Debug.LogError("[Save] " + result.message);
                    return result;
                }
            }
            if (json == null)
            {
                result.message = "No file for slot " + slot;
                Debug.LogWarning("[Save] " + result.message);
                return result;
            }

            SaveEnvelope envelope = null;
            try
            {
                envelope = ParseOrMigrate(json, migrateLegacy);
            }
            catch (Exception e)
            {
                result.message = "Parse failed: " + e.Message;
                Debug.LogError("[Save] " + result.message);
                return result;
            }
            if (envelope == null)
            {
                result.message = "Empty slot " + slot;
                return result;
            }

            result.savedVersion = string.IsNullOrEmpty(envelope.gameVersion) ? "0.1.0" : envelope.gameVersion;
            result.versionMismatch = IsMajorMismatch(result.savedVersion, Application.version);
            if (result.versionMismatch)
            {
                string warn = "Save is from v" + result.savedVersion + " and may be incompatible with v"
                    + Application.version + ".";
                result.message = warn;
                Debug.LogWarning("[Save] " + warn + " Loading as much as possible.");
            }

            SaveRegistry.RestoreAll(envelope, result);
            result.ok = true;
            Debug.Log("[Save] Loaded slot " + slot + " from " + source
                + " (format " + envelope.saveFormat + ", game " + result.savedVersion + ")");
            return result;
        }

        public static SaveEnvelope Peek(int slot, Func<string, SaveEnvelope> migrateLegacy)
        {
            if (!Exists(slot)) return null;
            try
            {
                return ParseOrMigrate(File.ReadAllText(PathFor(slot)), migrateLegacy);
            }
            catch
            {
                try
                {
                    if (File.Exists(BakPath(slot)))
                        return ParseOrMigrate(File.ReadAllText(BakPath(slot)), migrateLegacy);
                }
                catch { }
                return null;
            }
        }

        public static bool Delete(int slot)
        {
            if (!Valid(slot))
            {
                Debug.LogWarning("[Save] Bad slot " + slot);
                return false;
            }
            bool any = false;
            string path = PathFor(slot);
            string bak = BakPath(slot);
            string tmp = TempPath(slot);
            if (File.Exists(path)) { File.Delete(path); any = true; }
            if (File.Exists(bak)) { File.Delete(bak); any = true; }
            if (File.Exists(tmp)) { File.Delete(tmp); any = true; }
            if (any) Debug.Log("[Save] Deleted slot " + slot);
            return any;
        }

        public static int NewestSlot()
        {
            int best = -1;
            DateTime newest = DateTime.MinValue;
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (!Exists(slot)) continue;
                var written = File.GetLastWriteTimeUtc(PathFor(slot));
                if (best >= 0 && written <= newest) continue;
                best = slot;
                newest = written;
            }
            return best;
        }

        static SaveEnvelope ParseOrMigrate(string json, Func<string, SaveEnvelope> migrateLegacy)
        {
            // Keyed envelope has saveFormat >= 3 and an entries list.
            var env = SaveEnvelope.FromJson(json);
            if (env != null && env.saveFormat >= 3 && env.entries != null)
                return env;
            if (migrateLegacy != null)
                return migrateLegacy(json);
            return env;
        }

        static void ReplaceKeepingBak(string tmp, string path, string bak)
        {
            if (!File.Exists(path))
            {
                File.Move(tmp, path);
                return;
            }
            try
            {
                // File.Replace writes the replaced file to bak and keeps it.
                File.Replace(tmp, path, bak);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Replace failed (" + e.Message + "); copying over the slot");
                try
                {
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Copy(path, bak, true);
                }
                catch { }
                File.Copy(tmp, path, true);
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        /// <summary>True when the two versions have a different MAJOR number (see <see cref="MajorOf"/>).</summary>
        public static bool IsMajorMismatch(string savedVersion, string currentVersion)
        {
            return MajorOf(savedVersion) != MajorOf(currentVersion);
        }

        /// <summary>
        /// MAJOR number of a version. Games use 0.MAJOR.MINOR while in early access, so a leading 0 is
        /// skipped ("0.3.4" gives 3). "1.2.0" gives 1. Empty or unreadable gives 0.
        /// </summary>
        public static int MajorOf(string version)
        {
            if (string.IsNullOrEmpty(version)) return 0;
            var parts = version.Trim().TrimStart('v', 'V').Split('.');
            int first;
            if (parts.Length == 0 || !int.TryParse(parts[0], out first)) return 0;
            if (first != 0 || parts.Length < 2) return first;
            int second;
            return int.TryParse(parts[1], out second) ? second : 0;
        }
    }
}
