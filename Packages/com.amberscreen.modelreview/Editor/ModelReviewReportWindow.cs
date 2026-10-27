using UnityEditor;
using UnityEngine;

namespace AmberScreen.ModelReview.Editor
{
    public sealed class ModelReviewReportWindow : EditorWindow
    {
        string _text = "";
        Vector2 _scroll;

        public static void Show(string text)
        {
            var win = GetWindow<ModelReviewReportWindow>(true, "Model Review Report", true);
            win._text = text ?? "";
            win.minSize = new Vector2(480, 320);
            win.Show();
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_text, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Copy as text"))
                EditorGUIUtility.systemCopyBuffer = _text;
        }
    }
}
