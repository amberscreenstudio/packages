using UnityEditor;
using UnityEngine;

namespace AmberScreen.ModelReview.Editor
{
    [CustomEditor(typeof(ModelReviewRig))]
    public sealed class ModelReviewRigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var rig = (ModelReviewRig)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target (asset)", EditorStyles.boldLabel);
            string guid = ModelReviewTargetLoader.GetTargetGuid();
            Object current = null;
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                current = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            EditorGUI.BeginChangeCheck();
            var next = (GameObject)EditorGUILayout.ObjectField("Target (asset)", current, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (next == null)
                    ModelReviewTargetLoader.SetTargetGuid("");
                else
                {
                    string path = AssetDatabase.GetAssetPath(next);
                    ModelReviewTargetLoader.SetTargetGuid(AssetDatabase.AssetPathToGUID(path));
                }
            }

            Rect drop = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            GUI.Box(drop, "Drop model / prefab here");
            HandleDrop(drop);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Delivered")) rig.SetViewMode(ModelReviewViewMode.Delivered);
            if (GUILayout.Button("Clay")) rig.SetViewMode(ModelReviewViewMode.Clay);
            if (GUILayout.Button("Wireframe")) rig.SetViewMode(ModelReviewViewMode.Wireframe);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Frame")) rig.FrameAndAutoscale();
            if (GUILayout.Button(rig.ShowGrid ? "Grid On" : "Grid Off"))
                rig.SetGridVisible(!rig.ShowGrid);
            if (GUILayout.Button(rig.ShowCapsule ? "Capsule On" : "Capsule Off"))
                rig.SetCapsuleVisible(!rig.ShowCapsule);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(rig.NeutralTonemap ? "Tonemap On" : "Tonemap Off"))
                rig.SetNeutralTonemap(!rig.NeutralTonemap);
            if (GUILayout.Button(rig.KeyOnly ? "Key Only" : "All Lights"))
                rig.SetKeyOnly(!rig.KeyOnly);
            if (GUILayout.Button(rig.ShowLegend ? "Hide Legend" : "Show Legend"))
                rig.SetLegendVisible(!rig.ShowLegend);
            EditorGUILayout.EndHorizontal();
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;
        }

        void OnSceneGui(SceneView view)
        {
            if (!ModelReviewPipelineGuard.IsReviewSceneActive()) return;
            Event e = Event.current;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
            if (!DragHasGameObject(out _)) return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AcceptFirstGameObject();
                e.Use();
            }
        }

        void HandleDrop(Rect drop)
        {
            Event e = Event.current;
            if (!drop.Contains(e.mousePosition)) return;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
            if (!DragHasGameObject(out _)) return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AcceptFirstGameObject();
                e.Use();
            }
        }

        static bool DragHasGameObject(out GameObject go)
        {
            go = null;
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject g)
                {
                    go = g;
                    return true;
                }
            }
            return false;
        }

        static void AcceptFirstGameObject()
        {
            if (!DragHasGameObject(out var go)) return;
            string path = AssetDatabase.GetAssetPath(go);
            if (string.IsNullOrEmpty(path)) return;
            ModelReviewTargetLoader.SetTargetGuid(AssetDatabase.AssetPathToGUID(path));
        }
    }
}
