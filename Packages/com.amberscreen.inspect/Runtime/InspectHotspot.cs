using UnityEngine;

namespace AmberScreen.Inspect
{
    /// <summary>A marked spot on an inspectable object. Place it as a child at the spot.</summary>
    public sealed class InspectHotspot : MonoBehaviour
    {
        public enum ViewFrom
        {
            /// <summary>Turn the object so the spot faces the camera, seen from the object's centre outwards.</summary>
            Centre,
            /// <summary>Look straight down this transform's blue (forward) axis, keeping its green (up) axis up.</summary>
            HotspotForward
        }

        [Tooltip("Shown/sent when this spot is focused.")]
        public string label = "Spot";
        [Tooltip("Lower comes first when stepping through spots.")]
        public int order;
        public ViewFrom viewFrom = ViewFrom.Centre;
        [Tooltip("Zoom to this distance when focused (0 = keep the current distance).")]
        public float focusDistance;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.02f);
            if (viewFrom == ViewFrom.HotspotForward)
                Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.1f);
        }
    }
}
