using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmberScreen.ModelReview.Editor
{
    /// <summary>
    /// Menus for the temporary untitled review scene (marked with <see cref="ModelReviewSceneMarker"/>).
    /// </summary>
    public static class ModelReviewMenus
    {
        const string SessionPrevScene = "AmberScreen.ModelReview.PreviousScene";
        static string s_LastReport;
        static bool s_LoggedClearMissing;
        static bool s_DirtiedHook;

        /// <summary>
        /// Set by EditMode tests so Open/Close never call SaveCurrentModifiedScenesIfUserWantsTo
        /// and always clear dirtiness before replacing scenes (no modal dialogs under Test Runner).
        /// </summary>
        internal static bool SuppressSceneSavePrompts { get; set; }

        /// <summary>Discard dirty flags on untitled (no path) scenes — used before UTF SaveModifiedSceneTask.</summary>
        public static void DiscardUntitledSceneDirtiness() =>
            ModelReviewTestRunnerNoSaveDialog.ClearUntitledSceneDirtiness();

        /// <summary>Increments when the interactive save prompt path would run (tests assert this stays put when suppressed).</summary>
        internal static int SavePromptPathEntries { get; private set; }

        [MenuItem("Tools/Amber Screen/Model Review/Open Review Scene")]
        public static void OpenReviewScene()
        {
            EnsureDirtiedHook();

            if (ModelReviewPipelineGuard.IsReviewSceneActive())
            {
                ModelReviewPipelineGuard.TrySwitch();
                ModelReviewTargetLoader.TryReload();
                ModelReviewBrowser.PushNamesToActiveRig();
                ClearActiveReviewDirtiness();
                return;
            }

            if (!PrepareToReplaceScenes())
                return;

            var active = EditorSceneManager.GetActiveScene();
            if (active.IsValid() && !string.IsNullOrEmpty(active.path))
                SessionState.SetString(SessionPrevScene, active.path);
            else if (active.IsValid() && !ModelReviewPipelineGuard.IsReviewScene(active))
                SessionState.EraseString(SessionPrevScene);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!ModelReviewSceneBuilder.TryLoadPackageAssets(out var assets, out string error))
            {
                Debug.LogError("[ModelReview] Cannot open review scene: " + error);
                return;
            }

            ModelReviewSceneBuilder.BuildInto(scene, assets);

            ModelReviewPipelineGuard.TrySwitch();
            ModelReviewTargetLoader.TryReload();
            ModelReviewBrowser.PushNamesToActiveRig();

            // Build/reload/tone/volume wiring can mark Untitled dirty — clear after all of it.
            bool dirtyBefore = scene.isDirty;
            if (!ClearActiveReviewDirtiness() && !s_LoggedClearMissing)
            {
                s_LoggedClearMissing = true;
                Debug.LogWarning(
                    "[ModelReview] ClearSceneDirtiness not found; untitled scene may prompt to save.");
            }
            Debug.Log("[ModelReview] Untitled review scene isDirty before clear=" + dirtyBefore
                      + " after=" + scene.isDirty);
        }

        [MenuItem("Tools/Amber Screen/Model Review/Close Review Scene")]
        public static void CloseReviewScene()
        {
            var review = SceneManager.GetActiveScene();
            bool wasReview = ModelReviewPipelineGuard.IsReviewScene(review);

            ModelReviewTargetLoader.DestroyAllPreviews();
            ModelReviewPipelineGuard.Restore();

            // Destroying previews / restoring pipeline can re-dirty; never prompt to save a marker scene.
            if (wasReview && review.IsValid())
                ModelReviewSceneBuilder.TryClearSceneDirtiness(review, out _);

            // Tests / batch: clear every loaded scene so OpenScene/NewScene cannot show a modal.
            if (SuppressSceneSavePrompts || Application.isBatchMode)
                ModelReviewSceneBuilder.ClearDirtinessOnAllOpenScenes();

            string prev = SessionState.GetString(SessionPrevScene, "");
            if (!string.IsNullOrEmpty(prev) && File.Exists(prev))
                EditorSceneManager.OpenScene(prev, OpenSceneMode.Single);
            else
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        /// <returns>False if the user cancelled the save dialog (interactive menu only).</returns>
        static bool PrepareToReplaceScenes()
        {
            if (SuppressSceneSavePrompts || Application.isBatchMode)
            {
                ModelReviewSceneBuilder.ClearDirtinessOnAllOpenScenes();
                return true;
            }

            SavePromptPathEntries++;
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        [MenuItem("Tools/Amber Screen/Model Review/Browse Whole Project")]
        public static void BrowseWholeProject()
        {
            OpenBrowserWithFolder(null);
        }

        [MenuItem("Tools/Amber Screen/Model Review/Browse Folder…")]
        public static void BrowseFolderDialog()
        {
            string abs = EditorUtility.OpenFolderPanel("Browse models in folder", Application.dataPath, "");
            if (string.IsNullOrEmpty(abs)) return;
            if (!TryMakeAssetsRelative(abs, out string assetsPath, out string error))
            {
                EditorUtility.DisplayDialog("Model Review", error, "OK");
                return;
            }
            OpenBrowserWithFolder(assetsPath);
        }

        [MenuItem("Assets/Amber Screen/Review Folder in Model Review", true)]
        public static bool ValidateReviewFolder() =>
            TryGetSelectedProjectFolder(out _);

        [MenuItem("Assets/Amber Screen/Review Folder in Model Review")]
        public static void ReviewSelectedFolder()
        {
            if (!TryGetSelectedProjectFolder(out string path)) return;
            OpenBrowserWithFolder(path);
        }

        /// <summary>
        /// Project window selection: works for one-column, two-column left tree, and right pane
        /// right-clicks. Prefers Selection.assetGUIDs (set on folder right-click); falls back to
        /// activeObject / GetFiltered.
        /// </summary>
        internal static bool TryGetSelectedProjectFolder(out string folderPath)
        {
            folderPath = null;
            foreach (var guid in Selection.assetGUIDs)
            {
                if (string.IsNullOrEmpty(guid)) continue;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    folderPath = path;
                    return true;
                }
            }

            if (Selection.activeObject != null)
            {
                string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (AssetDatabase.IsValidFolder(path))
                {
                    folderPath = path;
                    return true;
                }
            }

            foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                if (obj == null) continue;
                string path = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(path))
                {
                    folderPath = path;
                    return true;
                }
            }

            return false;
        }

        [MenuItem("Assets/Amber Screen/Review in Model Review", true)]
        static bool ValidateReviewAsset()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject) return true;
            }
            return false;
        }

        [MenuItem("Assets/Amber Screen/Review in Model Review")]
        public static void ReviewSelectedAsset()
        {
            var guids = new List<string>();
            var seen = new HashSet<string>();
            foreach (var obj in Selection.objects)
            {
                if (!(obj is GameObject)) continue;
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;
                var main = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
                if (main == null) continue;
                var type = PrefabUtility.GetPrefabAssetType(main);
                if (type != PrefabAssetType.Regular
                    && type != PrefabAssetType.Variant
                    && type != PrefabAssetType.Model)
                    continue;
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid) || !seen.Add(guid)) continue;
                guids.Add(guid);
            }

            if (guids.Count == 0) return;

            ModelReviewBrowser.SetList(guids, 0);
            if (!ModelReviewPipelineGuard.IsReviewSceneActive())
                OpenReviewScene();
            else
            {
                ModelReviewTargetLoader.TryReload();
                ModelReviewBrowser.PushNamesToActiveRig();
                ClearActiveReviewDirtiness();
            }
        }

        [MenuItem("Tools/Amber Screen/Model Review/Material Report")]
        public static void MaterialReport()
        {
            GameObject root = null;
            string sourcePath = null;
            var rig = Object.FindAnyObjectByType<ModelReviewRig>();
            if (rig != null && ModelReviewPipelineGuard.IsReviewSceneActive() && rig.TargetInstance != null)
            {
                root = rig.TargetInstance;
                string guid = ModelReviewTargetLoader.GetTargetGuid();
                if (!string.IsNullOrEmpty(guid))
                    sourcePath = AssetDatabase.GUIDToAssetPath(guid);
            }
            else if (Selection.activeObject is GameObject sel)
            {
                root = sel;
                sourcePath = AssetDatabase.GetAssetPath(sel);
            }

            if (root == null)
            {
                Debug.LogWarning("[ModelReview] No target for material report.");
                return;
            }

            var result = ModelMaterialReport.Build(root, sourcePath);
            s_LastReport = result.ToText();
            ModelMaterialReport.LogAndShow(result);
        }

        [MenuItem("Tools/Amber Screen/Model Review/Copy Last Report")]
        public static void CopyLastReport()
        {
            if (string.IsNullOrEmpty(s_LastReport))
            {
                Debug.LogWarning("[ModelReview] No report to copy.");
                return;
            }
            EditorGUIUtility.systemCopyBuffer = s_LastReport;
            Debug.Log("[ModelReview] Last report copied to clipboard.");
        }

        /// <summary>Clear dirty flag on the active review scene if it has a marker.</summary>
        internal static bool ClearActiveReviewDirtiness()
        {
            var scene = SceneManager.GetActiveScene();
            if (!ModelReviewPipelineGuard.IsReviewScene(scene)) return false;
            return ModelReviewSceneBuilder.TryClearSceneDirtiness(scene, out _);
        }

        static void OpenBrowserWithFolder(string assetsFolderOrNull)
        {
            var guids = ModelReviewBrowser.GatherGuids(assetsFolderOrNull);
            string label = assetsFolderOrNull ?? "Assets";
            if (guids.Count == 0)
            {
                Debug.Log("[ModelReview] No models or prefabs found in " + label);
                return;
            }

            ModelReviewBrowser.SetList(guids, 0);
            if (!ModelReviewPipelineGuard.IsReviewSceneActive())
                OpenReviewScene();
            else
            {
                ModelReviewTargetLoader.TryReload();
                ModelReviewBrowser.PushNamesToActiveRig();
                ClearActiveReviewDirtiness();
            }
        }

        internal static bool TryMakeAssetsRelative(string absoluteFolder, out string assetsPath, out string error)
        {
            assetsPath = null;
            error = null;
            if (string.IsNullOrEmpty(absoluteFolder))
            {
                error = "No folder selected.";
                return false;
            }

            string abs = Path.GetFullPath(absoluteFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string data = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!abs.StartsWith(data, System.StringComparison.OrdinalIgnoreCase))
            {
                error = "Pick a folder inside this project's Assets.";
                return false;
            }

            string rel = abs.Equals(data, System.StringComparison.OrdinalIgnoreCase)
                ? "Assets"
                : ("Assets" + abs.Substring(data.Length).Replace('\\', '/'));
            if (!AssetDatabase.IsValidFolder(rel) && rel != "Assets")
            {
                error = "Not a valid Assets folder: " + rel;
                return false;
            }
            assetsPath = rel;
            return true;
        }

        [InitializeOnLoadMethod]
        static void InitDirtiedHook() => EnsureDirtiedHook();

        static void EnsureDirtiedHook()
        {
            if (s_DirtiedHook) return;
            s_DirtiedHook = true;
            EditorSceneManager.sceneDirtied += scene =>
            {
                if (!ModelReviewPipelineGuard.IsReviewScene(scene)) return;
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                // Clear immediately — delayCall alone races Close Review Scene and shows Untitled save.
                ModelReviewSceneBuilder.TryClearSceneDirtiness(scene, out _);
                EditorApplication.delayCall += () =>
                {
                    if (!scene.IsValid() || !ModelReviewPipelineGuard.IsReviewScene(scene)) return;
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    ModelReviewSceneBuilder.TryClearSceneDirtiness(scene, out _);
                };
            };
        }
    }
}
