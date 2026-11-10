using UnityEngine;

namespace AmberScreen.Navigation
{
    public interface INavigationDistanceProvider
    {
        // Return nonnegative metres (or metre-equivalent game units). Invalid values hide distance.
        float GetDistance(Vector3 origin, NavigationMarker marker);
    }

    /// <summary>Explicitly add components to a presenter's CustomSources. Bearing uses its north axis.</summary>
    public abstract class CustomBearingSource : MonoBehaviour
    {
        public string label = "Bearing";
        public Sprite sprite;
        public Color tint = Color.white;
        public int priority;
        public abstract bool TryGetBearing(out float bearing);
        // Deliberately no position or distance API.
    }
}
