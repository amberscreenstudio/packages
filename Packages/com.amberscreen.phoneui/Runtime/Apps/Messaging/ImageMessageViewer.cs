using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.PhoneUI
{
    /// <summary>
    /// Full-screen image viewer inside the phone <see cref="PhoneShell.ScreenContentRoot"/>.
    /// The image keeps its aspect (<see cref="ChatImageAttachment.ResolvedAspect"/>) and is
    /// letterboxed inside the frame. Back/Escape: when <paramref name="listenForEscape"/> is
    /// true the closer polls Esc; when false (MessagingApp owns the viewer) only host Back
    /// closes it. Order: DevConsole Esc first, then viewer, then thread list.
    /// </summary>
    public static class ImageMessageViewer
    {
        public static GameObject Show(
            RectTransform screenContentRoot,
            ChatImageAttachment image,
            string title = null,
            Action onClosed = null,
            bool listenForEscape = true)
        {
            if (screenContentRoot == null || image == null) return null;
            var tex = image.ResolvedTexture;
            if (tex == null) return null;

            var theme = PhoneUITheme.Active;
            var root = new GameObject("ImageViewer", typeof(RectTransform), typeof(ImageViewerEscCloser));
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.SetParent(screenContentRoot, false);
            UiFactory.Stretch(rootRt);
            rootRt.SetAsLastSibling();

            var closer = root.GetComponent<ImageViewerEscCloser>();
            closer.OnClosed = onClosed;
            closer.ListenForEscape = listenForEscape;

            var dim = UiFactory.Panel("Dim", rootRt, theme.ImageViewerDim);
            UiFactory.Stretch(dim);

            var frame = UiFactory.Panel("Frame", dim, theme.ImageViewerFrame);
            var frameRt = frame;
            frameRt.anchorMin = new Vector2(0.06f, 0.18f);
            frameRt.anchorMax = new Vector2(0.94f, 0.82f);
            frameRt.offsetMin = Vector2.zero;
            frameRt.offsetMax = Vector2.zero;

            var areaGo = new GameObject("ImgArea", typeof(RectTransform));
            var areaRt = areaGo.GetComponent<RectTransform>();
            areaRt.SetParent(frameRt, false);
            UiFactory.Stretch(areaRt);
            areaRt.offsetMin = new Vector2(8f, 8f);
            areaRt.offsetMax = new Vector2(-8f, -8f);

            var imgGo = new GameObject("Img", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            var imgRt = imgGo.GetComponent<RectTransform>();
            imgRt.SetParent(areaRt, false);
            UiFactory.Stretch(imgRt);
            // Disable while configuring: in Edit Mode, AspectMode.None syncs aspectRatio from
            // the current rect on SetDirty, which would wipe ResolvedAspect before FitInParent.
            var fit = imgGo.AddComponent<AspectRatioFitter>();
            fit.enabled = false;
            fit.aspectRatio = Mathf.Max(0.01f, image.ResolvedAspect);
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.enabled = true;
            var raw = imgGo.GetComponent<RawImage>();
            if (image.Sprite != null)
            {
                raw.texture = image.Sprite.texture;
                raw.uvRect = ChatImageAttachment.SpriteUvRect(image.Sprite);
            }
            else
            {
                raw.texture = tex;
                raw.uvRect = new Rect(0f, 0f, 1f, 1f);
            }
            raw.color = Color.white;

            string label = !string.IsNullOrEmpty(title) ? title
                : (!string.IsNullOrEmpty(image.FileName) ? image.FileName : "Image");
            var name = UiFactory.Label("Name", dim, label, PhoneDp.NameText, FontStyles.Bold);
            name.color = theme.ImageViewerTitle;
            name.alignment = TextAlignmentOptions.Center;
            var nameRt = name.rectTransform;
            nameRt.anchorMin = new Vector2(0.1f, 0.84f);
            nameRt.anchorMax = new Vector2(0.9f, 0.94f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;

            var close = UiFactory.Button("Close", dim, theme.ImageViewerClose);
            var closeRt = close.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.28f, 0.05f);
            closeRt.anchorMax = new Vector2(0.72f, 0.13f);
            closeRt.offsetMin = Vector2.zero;
            closeRt.offsetMax = Vector2.zero;
            var closeL = UiFactory.Label("L", closeRt, "Close", PhoneDp.BodyText, FontStyles.Bold);
            closeL.color = theme.ImageViewerCloseLabel;
            closeL.alignment = TextAlignmentOptions.Center;
            UiFactory.Stretch(closeL.rectTransform);
            close.onClick.AddListener(closer.Close);

            return root;
        }
    }
}
