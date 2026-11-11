using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.Navigation
{
    /// <summary>Optional text only. Assign an existing game-owned HUD Text; no canvas or layout is created.</summary>
    public sealed class PaceNotePresenter : MonoBehaviour
    {
        public Text label;
        public PaceNoteFeed Feed { get; set; }
        private void LateUpdate() => RenderNow();
        public void RenderNow()
        {
            if (label == null) return;
            bool visible = isActiveAndEnabled && NavigationHud.Visible && Feed != null && Feed.CoDriverPresent &&
                Feed.CurrentNote != null && Feed.RemainingSeconds > 0f;
            label.supportRichText = false;
            label.text = visible ? Feed.CurrentNote.Text : "";
            label.enabled = visible;
        }
        private void OnDisable()
        {
            if (label == null) return;
            label.text = ""; label.enabled = false;
        }
    }
}
