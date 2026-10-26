using UnityEngine;

namespace AmberScreen.PhoneUI
{
    [System.Serializable]
    public sealed class ContactInfo
    {
        public string Id;
        public string DisplayName;
        public Sprite Avatar;
        /// <summary>Alpha 0 = unset; UI uses <see cref="PhoneUITheme"/> fallbacks.</summary>
        public Color Color = Color.clear;
        public bool IsPlayer;
    }
}
