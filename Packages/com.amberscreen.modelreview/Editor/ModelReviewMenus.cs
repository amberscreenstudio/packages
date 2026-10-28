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

        [MenuItem("Tools/Amber Screen/Model Review/Open Review Scene")]
        public static void OpenReviewScene()
        {
            EnsureDirtiedHook();

            if (ModelReviewPipelineGuard.IsReviewSceneActive())
            {
                ModelReviewPipelineGuard.TrySwitch();
                ModelReviewTargetLoader.TryReload();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var active = EditorSceneManager.GetActiveScene();
            if (active.IsValid() && !string.IsNullOrEmpty(active.path))
                SessionState.SetString(SessionPrevScene, active.path);
            else if (active.IsValid() && !ModelReviewPipelineGuard.IsReviewScene(active))
                SessionState.EraseString(SessionPrevScene);

            // NewScene can invalidate ScriptableObject refs loaded beforehand — load after.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!ModelReviewSceneBuilder.TryLoadPackageAssets(out var assets, out string error))
            {
                Debug.LogError("[ModelReview] Cannot open review scene: " + error);
                return;
            }

            ModelReviewSceneBuilder.BuildInto(scene, assets);

            bool dirtyBefore = scene.isDirty;
            if (!ModelReviewSceneBuilder.TryClearSceneDirtiness(scene, out bool foundApi))
            {
                if (!s_LoggedClearMissing)
                {
                    s_LoggedClearMissing = true;
                    Debug.LogWarning(
                        "[ModelReview] ClearSceneDirtiness not found; untitled scene may prompt to save.");
                }
            }
            Debug.Log("[ModelReview] Untitled review scene isDirty before clear=" + dirtyBefore
                      + " after=" + scene.isDirty
                      + " clearApi=" + foundApi);

            ModelReviewPipelineGuard.TrySwitch();
            ModelReviewTargetLoader.TryReload();
        }

        [MenuItem("Tools/Amber Screen/Model Review/Close Review Scene")]
        public static void CloseReviewScene()
        {
            ModelReviewTargetLoader.DestroyAllPreviews();
            ModelReviewPipelineGuard.Restore();
            string prev = SessionState.GetString(SessionPrevScene, "");
            if (!string.IsNullOrEmpty(prev) && System.IO.File.Exists(prev))
                EditorSceneManager.OpenScene(prev, OpenSceneMode.Single);
            else
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        [MenuItem("Assets/Amber Screen/Review in Model Review", true)]
        static bool ValidateReviewAsset() =>
            Selection.activeObject is GameObject;

        [MenuItem("Assets/Amber Screen/Review in Model Review")]
        public static void ReviewSelectedAsset()
        {
            var go = Selection.activeObject as GameObject;
            if (go == null) return;
            string path = AssetDatabase.GetAssetPath(go);
            if (string.IsNullOrEmpty(path)) return;
            string guid = AssetDatabase.AssetPathToGUID(path);
            // Store before open so Open's TryReload spawns once.
            SessionState.SetString(ModelReviewTargetLoader.SessionGuidKey, guid ?? "");
            if (!ModelReviewPipelineGuard.IsReviewSceneActive())
                OpenReviewScene();
            else
                ModelReviewTargetLoader.TryReload();
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

        static void EnsureDirtiedHook()
        {
            if (s_DirtiedHook) return;
            s_DirtiedHook = true;
            EditorSceneManager.sceneDirtied += scene =>
            {
                if (!ModelReviewPipelineGuard.IsReviewScene(scene)) return;
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
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
