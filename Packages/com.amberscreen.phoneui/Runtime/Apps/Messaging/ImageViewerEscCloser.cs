using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberScreen.PhoneUI
{
    /// <summary>
    /// Listens for Escape / phone Back while the image viewer is open.
    /// When <see cref="ListenForEscape"/> is false (MessagingApp owns Back), only
    /// <see cref="Close"/> / host <c>TryHandleBack</c> dismisses the viewer — avoids
    /// the same Esc both closing the viewer and leaving the thread.
    /// </summary>
    public sealed class ImageViewerEscCloser : MonoBehaviour
    {
        public Action OnClosed;

        /// <summary>
        /// When false, Esc is not polled here (host MessagingApp / PhoneShell owns Back).
        /// Default true for standalone viewers.
        /// </summary>
        public bool ListenForEscape = true;

        /// <summary>Frame count when this viewer last closed (0 if never).</summary>
        public static int LastClosedFrame { get; private set; }

        public void Close()
        {
            LastClosedFrame = Time.frameCount;
            var cb = OnClosed;
            OnClosed = null;
            UiFactory.SafeDestroy(gameObject);
            try { cb?.Invoke(); } catch { /* ignore */ }
        }

        void Update()
        {
            if (!ListenForEscape) return;
            // Console Esc sets IgnoreBack for this frame — do not steal that close.
            if (PhoneInputGate.IgnoreBack || PhoneInputGate.BlockPhone) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                Close();
        }
    }
}
