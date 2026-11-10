using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>One global switch, polled by HUD presenters; no GameObject or singleton host.</summary>
    public static class NavigationHud
    {
        public static bool Visible { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Visible = true;
    }
}
