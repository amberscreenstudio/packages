using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AmberScreen.ModelReview.Editor
{
    /// <summary>
    /// Gathers prefabs/models under Assets and drives one-at-a-time review swaps via SessionState.
    /// </summary>
    [InitializeOnLoad]
    public static class ModelReviewBrowser
    {
        public const string SessionGuidsKey = "AmberScreen.ModelReview.BrowserGuids";
        public const string SessionIndexKey = "AmberScreen.ModelReview.BrowserIndex";
        const char GuidSep = '\n';

        static ModelReviewBrowser()
        {
            ModelReviewRig.BrowseRequested -= OnBrowseRequested;
            ModelReviewRig.BrowseRequested += OnBrowseRequested;
            EditorApplication.delayCall += PushNamesToActiveRig;
            EditorApplication.playModeStateChanged += _ =>
                EditorApplication.delayCall += PushNamesToActiveRig;
            AssemblyReloadEvents.afterAssemblyReload += () =>
                EditorApplication.delayCall += PushNamesToActiveRig;
        }

        public static int Count
        {
            get
            {
                var list = LoadGuids();
                return list.Count;
            }
        }

        public static int Index
        {
            get
            {
                int count = Count;
                if (count == 0) return -1;
                return Mathf.Clamp(SessionState.GetInt(SessionIndexKey, 0), 0, count - 1);
            }
        }

        public static string CurrentGuid
        {
            get
            {
                var list = LoadGuids();
                int i = Index;
                if (i < 0 || i >= list.Count) return "";
                return list[i];
            }
        }

        public static List<string> GatherGuids(string folder = null)
        {
            string[] searchIn = folder == null
                ? new[] { "Assets" }
                : new[] { folder };
            var guids = AssetDatabase.FindAssets("t:GameObject", searchIn);
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string packagePrefix = "Packages/";

            foreach (var guid in guids)
            {
                if (string.IsNullOrEmpty(guid) || !seen.Add(guid)) continue;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                path = path.Replace('\\', '/');
                if (path.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase)) continue;
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(path, "Assets", StringComparison.OrdinalIgnoreCase))
                    continue;

                var type = PrefabUtility.GetPrefabAssetType(AssetDatabase.LoadMainAssetAtPath(path));
                if (type != PrefabAssetType.Regular
                    && type != PrefabAssetType.Variant
                    && type != PrefabAssetType.Model)
                    continue;

                result.Add(guid);
            }

            result.Sort((a, b) =>
            {
                string pa = AssetDatabase.GUIDToAssetPath(a) ?? "";
                string pb = AssetDatabase.GUIDToAssetPath(b) ?? "";
                return string.Compare(pa, pb, StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        public static void SetList(IList<string> guids, int startIndex)
        {
            var list = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (guids != null)
            {
                foreach (var g in guids)
                {
                    if (string.IsNullOrEmpty(g) || !seen.Add(g)) continue;
                    list.Add(g);
                }
            }

            SaveGuids(list);
            int index = list.Count == 0 ? 0 : Mathf.Clamp(startIndex, 0, list.Count - 1);
            SessionState.SetInt(SessionIndexKey, index);
            ApplyCurrent();
        }

        public static void Next()
        {
            var list = LoadGuids();
            if (list.Count == 0) return;
            int i = Index;
            i = (i + 1) % list.Count;
            SessionState.SetInt(SessionIndexKey, i);
            ApplyCurrent();
        }

        public static void Previous()
        {
            var list = LoadGuids();
            if (list.Count == 0) return;
            int i = Index;
            i = (i - 1 + list.Count) % list.Count;
            SessionState.SetInt(SessionIndexKey, i);
            ApplyCurrent();
        }

        public static void Select(int index)
        {
            var list = LoadGuids();
            if (list.Count == 0) return;
            SessionState.SetInt(SessionIndexKey, Mathf.Clamp(index, 0, list.Count - 1));
            ApplyCurrent();
        }

        public static void PushNamesToActiveRig()
        {
            var rig = UnityEngine.Object.FindAnyObjectByType<ModelReviewRig>();
            if (rig == null) return;
            var list = LoadGuids();
            var names = new List<string>(list.Count);
            foreach (var guid in list)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid) ?? "";
                string file = Path.GetFileNameWithoutExtension(path);
                string folder = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "";
                names.Add(string.IsNullOrEmpty(folder) ? file : file + "  (" + folder + ")");
            }
            rig.SetBrowserItems(names, Index);
        }

        static void ApplyCurrent()
        {
            string guid = CurrentGuid;
            ModelReviewTargetLoader.SetTargetGuid(guid);
            PushNamesToActiveRig();
        }

        static void OnBrowseRequested(int request)
        {
            if (!ModelReviewPipelineGuard.IsReviewSceneActive()) return;
            if (request <= int.MinValue / 2)
            {
                Select(request - int.MinValue);
                return;
            }
            if (request > 0)
            {
                for (int n = 0; n < request; n++) Next();
                return;
            }
            if (request < 0)
            {
                for (int n = 0; n < -request; n++) Previous();
            }
        }

        static List<string> LoadGuids()
        {
            string raw = SessionState.GetString(SessionGuidsKey, "");
            var list = new List<string>();
            if (string.IsNullOrEmpty(raw)) return list;
            foreach (var part in raw.Split(GuidSep))
            {
                if (!string.IsNullOrEmpty(part))
                    list.Add(part);
            }
            return list;
        }

        static void SaveGuids(List<string> guids)
        {
            if (guids == null || guids.Count == 0)
            {
                SessionState.EraseString(SessionGuidsKey);
                return;
            }
            var sb = new StringBuilder();
            for (int i = 0; i < guids.Count; i++)
            {
                if (i > 0) sb.Append(GuidSep);
                sb.Append(guids[i]);
            }
            SessionState.SetString(SessionGuidsKey, sb.ToString());
        }
    }
}
