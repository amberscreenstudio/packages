using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmberScreen.ModelReview.Editor
{
    /// <summary>
    /// Unity Test Framework's first EditMode task is SaveModifiedSceneTask →
    /// SaveCurrentModifiedScenesIfUserWantsTo, before any [SetUp]. Lives in the Editor
    /// assembly (always loaded) so it can clear untitled dirtiness before that task runs.
    /// Active while the Test Runner window is open, or whenever -runTests is on the command line.
    /// </summary>
    [InitializeOnLoad]
    static class ModelReviewTestRunnerNoSaveDialog
    {
        static readonly bool s_CommandLineRunTests =
            Environment.GetCommandLineArgs().Any(a =>
                string.Equals(a, "-runTests", StringComparison.OrdinalIgnoreCase));

        static ModelReviewTestRunnerNoSaveDialog()
        {
            EditorApplication.update += OnUpdate;
            // Command-line: clear immediately at domain load and again before first test tick.
            if (s_CommandLineRunTests)
            {
                ClearUntitledSceneDirtiness();
                EditorApplication.delayCall += ClearUntitledSceneDirtiness;
            }
        }

        static void OnUpdate()
        {
            if (!s_CommandLineRunTests && !IsTestRunnerWindowOpen())
                return;
            ClearUntitledSceneDirtiness();
        }

        static bool IsTestRunnerWindowOpen()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (w == null) continue;
                if (w.GetType().Name == "TestRunnerWindow")
                    return true;
            }
            return false;
        }

        internal static void ClearUntitledSceneDirtiness()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isDirty) continue;
                if (!string.IsNullOrEmpty(scene.path)) continue;
                ModelReviewSceneBuilder.TryClearSceneDirtiness(scene, out _);
            }
        }
    }
}
