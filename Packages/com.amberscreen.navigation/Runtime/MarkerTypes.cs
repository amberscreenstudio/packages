using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Navigation
{
    [Serializable]
    public sealed class MarkerType
    {
        public string typeId;
        public Sprite sprite;
        public Color tint = Color.white;
        public bool defaultShowDistance = true;
    }

    [CreateAssetMenu(fileName = "NavigationMarkerTypes", menuName = "AmberScreen/Navigation/Marker Types")]
    public sealed class MarkerTypes : ScriptableObject
    {
        public List<MarkerType> types = new List<MarkerType>();
        // First match wins. Unknown types get text-only, white, distance-on defaults.
        public MarkerType Find(string typeId) => types.Find(t => t != null && t.typeId == typeId);
    }
}
