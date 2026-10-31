using UnityEditor;
using UnityEngine;

namespace AmberScreen.ModelReview.Tests
{
    /// <summary>
    /// Interactive Test Runner: Unity 6000 ProjectBrowser can NRE (ExposablePopupMenu) during NewScene.
    /// Close only inside tests; restore Project so Fabio's layout survives Run All.
    /// Never call from package menus. No-op under -batchmode/-nographics (no GPU / no views).
    /// </summary>
    static class ModelReviewTestEditorWindows
    {
        internal static void CloseProjectBrowsers()
        {
            if (Application.isBatchMode) return;
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (w == null) continue;
                if (w.GetType().Name == "ProjectBrowser")
                    w.Close();
            }
        }

        internal static void EnsureProjectBrowserOpen()
        {
            if (Application.isBatchMode) return;
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
            if (type != null)
            {
                EditorWindow.GetWindow(type, false, "Project", false);
                return;
            }

            EditorApplication.ExecuteMenuItem("Window/General/Project");
        }
    }
}
