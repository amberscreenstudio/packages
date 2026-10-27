using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AmberScreen.ModelReview.Editor
{
    public static class ModelReviewMenus
    {
        const string ReviewScenePath = "Packages/com.amberscreen.modelreview/Scenes/ModelReview.unity";
        const string SessionPrevScene = "AmberScreen.ModelReview.PreviousScene";
        static string s_LastReport;

        [MenuItem("Tools/Amber Screen/Model Review/Open Review Scene")]
        public static void OpenReviewScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var active = EditorSceneManager.GetActiveScene();
            if (active.IsValid() && !string.IsNullOrEmpty(active.path))
                SessionState.SetString(SessionPrevScene, active.path);
            EditorSceneManager.OpenScene(ReviewScenePath, OpenSceneMode.Single);
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
            if (!ModelReviewPipelineGuard.IsReviewSceneActive())
                OpenReviewScene();
            ModelReviewTargetLoader.SetTargetGuid(guid);
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
    }
}
