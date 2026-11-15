using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Save
{
    [Serializable]
    public class SaveKeyedEntry
    {
        public string key;
        public string json;
    }

    // On-disk envelope for saveFormat >= 3 (keyed providers).
    [Serializable]
    public class SaveEnvelope
    {
        public string gameVersion = "";
        public int saveFormat;
        public List<SaveKeyedEntry> entries = new List<SaveKeyedEntry>();

        public SaveKeyedEntry Find(string key)
        {
            if (entries == null || string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e != null && e.key == key) return e;
            }
            return null;
        }

        public void Set(string key, string json)
        {
            if (entries == null) entries = new List<SaveKeyedEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].key == key)
                {
                    entries[i].json = json ?? "";
                    return;
                }
            }
            entries.Add(new SaveKeyedEntry { key = key, json = json ?? "" });
        }

        public string ToJson(bool pretty)
        {
            return JsonUtility.ToJson(this, pretty);
        }

        public static SaveEnvelope FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<SaveEnvelope>(json);
        }
    }
}
