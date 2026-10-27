using UnityEngine;

namespace AmberScreen.ModelReview
{
    /// <summary>
    /// Marks a temporary model-review preview root. Never saved; used to find DontSave orphans.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ModelReviewPreviewMarker : MonoBehaviour
    {
    }
}
