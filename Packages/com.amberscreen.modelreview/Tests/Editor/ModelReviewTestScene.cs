using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using AmberScreen.ModelReview.Editor;

namespace AmberScreen.ModelReview.Tests
{
    /// <summary>Scene helpers for EditMode tests — never show a save modal.</summary>
    static class ModelReviewTestScene
    {
        internal static void BeginSuppressSavePrompts()
        {
            ModelReviewMenus.SuppressSceneSavePrompts = true;
            ClearDirtinessOnAllOpenScenes();
        }

        internal static void EndSuppressSavePrompts()
        {
            ClearDirtinessOnAllOpenScenes();
            ModelReviewMenus.SuppressSceneSavePrompts = false;
        }

        internal static void ClearDirtinessOnAllOpenScenes() =>
            ModelReviewSceneBuilder.ClearDirtinessOnAllOpenScenes();

        /// <summary>NewScene(Single) after clearing dirtiness — safe under interactive Test Runner.</summary>
        internal static Scene NewEmptySceneNoPrompt()
        {
            ClearDirtinessOnAllOpenScenes();
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
