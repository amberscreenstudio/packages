using UnityEngine;

namespace AmberScreen.Inspect
{
    /// <summary>Optional marker for objects the game lets the player pick up and inspect.</summary>
    public sealed class Inspectable : MonoBehaviour
    {
        [Tooltip("Hold distance for this object (0 = use the viewer's).")]
        public float holdDistance;
        [Tooltip("Name a game can show, e.g. in a prompt.")]
        public string displayName;
    }
}
