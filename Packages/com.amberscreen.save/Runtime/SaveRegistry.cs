using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Save
{
    public static class SaveRegistry
    {
        struct Entry
        {
            public ISaveStateProvider provider;
            public Type stateType;
        }

        static readonly Dictionary<string, Entry> map = new Dictionary<string, Entry>();

        public static IEnumerable<string> Keys { get { return map.Keys; } }

        public static bool Register(ISaveStateProvider provider, Type stateType)
        {
            if (provider == null || stateType == null)
            {
                Debug.LogError("[Save] Register rejected: null provider or state type.");
                return false;
            }
            string key = provider.Key;
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[Save] Register rejected: empty key.");
                return false;
            }
            if (map.ContainsKey(key))
            {
                Debug.LogError("[Save] Duplicate provider key \"" + key + "\" rejected.");
                return false;
            }
            map[key] = new Entry { provider = provider, stateType = stateType };
            return true;
        }

        public static void Unregister(string key)
        {
            if (!string.IsNullOrEmpty(key)) map.Remove(key);
        }

        public static void Clear() { map.Clear(); }

        public static bool TryGet(string key, out ISaveStateProvider provider, out Type stateType)
        {
            Entry e;
            if (!map.TryGetValue(key, out e))
            {
                provider = null;
                stateType = null;
                return false;
            }
            provider = e.provider;
            stateType = e.stateType;
            return true;
        }

        public static void CaptureAll(SaveEnvelope envelope)
        {
            if (envelope == null) return;
            foreach (var kv in map)
            {
                var e = kv.Value;
                if (e.provider == null) continue;
                try
                {
                    object state = e.provider.Capture();
                    string json = state != null ? JsonUtility.ToJson(state) : "";
                    envelope.Set(kv.Key, json);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Save] Capture \"" + kv.Key + "\" failed: " + ex.Message);
                }
            }
        }

        public static void RestoreAll(SaveEnvelope envelope, LoadResult result)
        {
            if (envelope == null || envelope.entries == null) return;
            var restored = new HashSet<string>();
            for (int i = 0; i < envelope.entries.Count; i++)
            {
                var ent = envelope.entries[i];
                if (ent == null || string.IsNullOrEmpty(ent.key)) continue;
                ISaveStateProvider provider;
                Type stateType;
                if (!TryGet(ent.key, out provider, out stateType))
                {
                    // Unknown key: leave in the envelope for round-trip.
                    if (result != null) result.skippedKeys.Add(ent.key);
                    continue;
                }
                try
                {
                    if (string.IsNullOrEmpty(ent.json)) continue;
                    object state = Activator.CreateInstance(stateType);
                    JsonUtility.FromJsonOverwrite(ent.json, state);
                    provider.Restore(state);
                    restored.Add(ent.key);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Save] Restore \"" + ent.key + "\" failed: " + ex.Message);
                    if (result != null) result.skippedKeys.Add(ent.key);
                }
            }
            // Providers with no data keep their defaults (do not call Restore).
        }
    }
}
