using UnityEngine;

namespace AmberScreen.ModelReview
{
    /// <summary>
    /// Marks the temporary untitled model-review studio scene.
    /// Survives play mode and domain reload; detection never uses scene path or name.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ModelReviewSceneMarker : MonoBehaviour
    {
    }
}
