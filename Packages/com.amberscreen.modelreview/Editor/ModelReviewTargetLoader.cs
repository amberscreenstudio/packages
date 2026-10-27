using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmberScreen.ModelReview.Editor
{
    /// <summary>
    /// Instantiates the review target under a DontSave root so the package scene stays clean.
    /// Uses Object.Instantiate (no prefab link).
    /// </summary>
    [InitializeOnLoad]
    public static class ModelReviewTargetLoader
    {
        public const string SessionGuidKey = "AmberScreen.ModelReview.TargetGuid";
        public const string TargetRootName = "ModelReview_Target";

        static ModelReviewTargetLoader()
        {
            EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += TryReload;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode
                    || state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += TryReload;
            };
            AssemblyReloadEvents.afterAssemblyReload += () =>
                EditorApplication.delayCall += TryReload;
        }

        public static void SetTargetGuid(string guid)
        {
            SessionState.SetString(SessionGuidKey, guid ?? "");
            TryReload();
        }

        public static string GetTargetGuid() => SessionState.GetString(SessionGuidKey, "");

        public static void TryReload()
        {
            if (!ModelReviewPipelineGuard.IsReviewSceneActive())
                return;

            var rig = Object.FindAnyObjectByType<ModelReviewRig>();
            if (rig == null) return;

            DestroyExistingTarget();
            string guid = GetTargetGuid();
            if (string.IsNullOrEmpty(guid))
            {
                rig.ClearTarget();
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("[ModelReview] Target GUID not found: " + guid);
                return;
            }

            var instance = Object.Instantiate(asset);
            instance.name = asset.name;
            SetDontSaveRecursive(instance);

            var root = new GameObject(TargetRootName);
            SetDontSaveRecursive(root);
            instance.transform.SetParent(root.transform, false);

            PlaceOnFloor(root, instance);
            // DontSave root: package scene stays clean (verified by LoadTarget_DoesNotDirtyScene).
            SceneManager.MoveGameObjectToScene(root, SceneManager.GetActiveScene());
            rig.SetTarget(instance);
        }

        public static void DestroyExistingTarget()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == TargetRootName)
                    Object.DestroyImmediate(go);
            }
            var rig = Object.FindAnyObjectByType<ModelReviewRig>();
            rig?.ClearTarget();
        }

        static void PlaceOnFloor(GameObject root, GameObject instance)
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            var bounds = ModelReviewRig.ComputeBounds(instance);
            float bottom = bounds.min.y;
            root.transform.position = new Vector3(-bounds.center.x, -bottom, -bounds.center.z);
        }

        static void SetDontSaveRecursive(GameObject go)
        {
            go.hideFlags = HideFlags.DontSave;
            foreach (Transform t in go.transform)
                SetDontSaveRecursive(t.gameObject);
        }
    }
}
