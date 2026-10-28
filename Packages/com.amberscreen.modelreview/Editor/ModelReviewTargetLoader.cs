using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmberScreen.ModelReview.Editor
{
    /// <summary>
    /// Instantiates the review target under a DontSave marked root in the temporary untitled
    /// review scene (<see cref="ModelReviewSceneMarker"/>). Uses Object.Instantiate (no prefab link).
    /// Tracks and destroys all previews on every exit.
    /// </summary>
    [InitializeOnLoad]
    public static class ModelReviewTargetLoader
    {
        public const string SessionGuidKey = "AmberScreen.ModelReview.TargetGuid";
        public const string TargetRootName = "ModelReview_Target";

        static readonly List<GameObject> s_TrackedRoots = new();
        static bool s_Hooks;

        static ModelReviewTargetLoader()
        {
            EnsureHooks();
            EditorApplication.delayCall += () =>
            {
                if (!ModelReviewPipelineGuard.IsReviewSceneActive())
                    DestroyAllPreviews();
                else
                    TryReload();
            };
        }

        static void EnsureHooks()
        {
            if (s_Hooks) return;
            s_Hooks = true;

            EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += TryReload;
            EditorSceneManager.sceneClosing += (scene, _) =>
            {
                if (ModelReviewPipelineGuard.IsReviewScene(scene))
                    DestroyAllPreviews();
            };
            EditorSceneManager.activeSceneChangedInEditMode += (_, next) =>
            {
                // Closing scene loses the marker; destroy whenever we leave the review scene.
                if (!ModelReviewPipelineGuard.IsReviewScene(next))
                    DestroyAllPreviews();
            };

            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode
                    || state == PlayModeStateChange.ExitingPlayMode)
                    DestroyAllPreviews();
                else if (state == PlayModeStateChange.EnteredPlayMode
                         || state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += TryReload;
            };

            AssemblyReloadEvents.beforeAssemblyReload += DestroyAllPreviews;
            AssemblyReloadEvents.afterAssemblyReload += () =>
                EditorApplication.delayCall += TryReload;

            EditorApplication.wantsToQuit += () =>
            {
                DestroyAllPreviews();
                return true;
            };
            EditorApplication.quitting += DestroyAllPreviews;
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

            DestroyAllPreviews();
            string guid = GetTargetGuid();
            if (string.IsNullOrEmpty(guid))
                return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("[ModelReview] Target GUID not found: " + guid);
                return;
            }

            SpawnPreview(asset, rig.gameObject.scene, rig);
        }

        /// <summary>
        /// Spawns a single DontSave preview under <paramref name="scene"/> and wires the rig.
        /// Callable without the package scene path (tests / helpers).
        /// </summary>
        public static GameObject SpawnPreview(GameObject asset, Scene scene, ModelReviewRig rig)
        {
            if (asset == null) throw new System.ArgumentNullException(nameof(asset));
            if (rig == null) throw new System.ArgumentNullException(nameof(rig));

            DestroyAllPreviews();

            var instance = Object.Instantiate(asset);
            instance.name = asset.name;
            SetDontSaveRecursive(instance);

            var root = new GameObject(TargetRootName);
            root.AddComponent<ModelReviewPreviewMarker>();
            SetDontSaveRecursive(root);
            instance.transform.SetParent(root.transform, false);

            PlaceOnFloor(root, instance);

            if (scene.IsValid())
                SceneManager.MoveGameObjectToScene(root, scene);

            Track(root);
            rig.SetTarget(instance);
            return root;
        }

        public static void DestroyAllPreviews()
        {
            var rig = Object.FindAnyObjectByType<ModelReviewRig>();
            rig?.ClearTarget();

            foreach (var root in FindAllPreviewRoots())
            {
                if (root != null)
                    Object.DestroyImmediate(root);
            }
            s_TrackedRoots.Clear();
        }

        public static List<GameObject> FindAllPreviewRoots()
        {
            var found = new HashSet<GameObject>();
            for (int i = s_TrackedRoots.Count - 1; i >= 0; i--)
            {
                var go = s_TrackedRoots[i];
                if (go == null)
                {
                    s_TrackedRoots.RemoveAt(i);
                    continue;
                }
                found.Add(go);
            }

            foreach (var marker in Resources.FindObjectsOfTypeAll<ModelReviewPreviewMarker>())
            {
                if (marker == null) continue;
                if (EditorUtility.IsPersistent(marker)) continue;
                var go = marker.gameObject;
                if (go == null) continue;
                // Scene objects and DontSave orphans (including invalid/empty scenes).
                found.Add(GetPreviewRoot(go));
            }

            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || go.name != TargetRootName) continue;
                if (EditorUtility.IsPersistent(go)) continue;
                found.Add(go);
            }

            return new List<GameObject>(found);
        }

        static GameObject GetPreviewRoot(GameObject go)
        {
            var t = go.transform;
            while (t.parent != null
                   && (t.parent.GetComponent<ModelReviewPreviewMarker>() != null
                       || t.parent.name == TargetRootName))
                t = t.parent;
            var marker = go.GetComponentInParent<ModelReviewPreviewMarker>();
            if (marker != null) return marker.gameObject;
            return t.gameObject;
        }

        static void Track(GameObject root)
        {
            if (root == null) return;
            if (!s_TrackedRoots.Contains(root))
                s_TrackedRoots.Add(root);
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
