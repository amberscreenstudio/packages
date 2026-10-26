using System;
using UnityEngine;

namespace AmberScreen.PhoneUI
{
    /// <summary>
    /// Skinnable colours/fonts for PhoneUI. Null <see cref="Active"/> falls back to built-in defaults
    /// matching the 0.2.0 look. Field initialisers are empty; <see cref="ApplyBuiltinDefaults"/> is
    /// the sole source of default values.
    /// </summary>
    [CreateAssetMenu(menuName = "Amber Screen/Phone UI/Theme", fileName = "PhoneUITheme")]
    public sealed class PhoneUITheme : ScriptableObject
    {
        public static PhoneUITheme Active
        {
            get
            {
                if (s_Override != null) return s_Override;
                if (!s_ResourcesLookedUp)
                {
                    s_ResourcesLookedUp = true;
                    s_Cached = Resources.Load<PhoneUITheme>("PhoneUITheme");
                }
                return s_Cached != null ? s_Cached : Builtin;
            }
        }

        static PhoneUITheme s_Override;
        static PhoneUITheme s_Cached;
        static PhoneUITheme s_Builtin;
        static bool s_ResourcesLookedUp;

        /// <summary>Test/host override; pass null to clear.</summary>
        public static void SetOverride(PhoneUITheme theme)
        {
            s_Override = theme;
            UiFactory.InvalidateFontCache();
        }

        public static PhoneUITheme Builtin
        {
            get
            {
                if (s_Builtin == null)
                {
                    s_Builtin = CreateInstance<PhoneUITheme>();
                    s_Builtin.hideFlags = HideFlags.HideAndDontSave;
                    s_Builtin.ApplyBuiltinDefaults();
                }
                return s_Builtin;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Override = null;
            s_Cached = null;
            s_Builtin = null;
            s_ResourcesLookedUp = false;
            UiFactory.InvalidateFontCache();
        }

        [Header("Messaging")]
        public Color MessagingRoot;
        public Color ThreadListBg;
        public Color ThreadBg;
        public Color ScrollViewportTint;
        public Color ComposerBg;
        public Color ComposerInputBg;
        public Color ComposerPlaceholder;
        public Color SendButton;
        public Color SendButtonLabel;
        public Color NewMessagesPill;
        public Color NewMessagesPillLabel;
        public Color TypingLabel;
        public Color SeenLabel;
        public Color ThreadListRow;
        public Color ThreadListPreview;
        public Color UnreadBadge;
        public Color UnreadBadgeLabel;
        public Color TimestampLabel;
        public Color BubbleRowTransparent;
        public Color ContactNameFallback;

        [Header("Messaging bubbles")]
        public Color PlayerBubble;
        public Color PlayerBubbleText;
        public Color NpcBubbleFallback;
        public Color NpcBubbleText;
        [Range(0f, 1f)] public float NpcBubbleLerpToWhite;

        [Header("Receipts")]
        public Color ReceiptGrey;
        public Color ReceiptRead;

        [Header("Image attachment")]
        public Color ImageThumbFrame;
        public Color ImageFileNameOnPlayer;
        public Color ImageFileNameOnNpc;
        public Color ImageViewerDim;
        public Color ImageViewerFrame;
        public Color ImageViewerClose;
        public Color ImageViewerCloseLabel;
        public Color ImageViewerTitle;

        [Header("Home")]
        public Color HomeBg;
        public Color HomeTitle;
        public Color HomeIconTile;
        public Color HomeIconLabel;
        public Color AppIconFallback;
        public AppIconColorEntry[] AppIconColors;

        [Header("Status / shell")]
        public Color StatusBarBg;
        public Color StatusBarText;
        public Color Letterbox;
        public Color PhoneStage;
        public Color SafeArea;
        public Color NavBar;
        public Color NavButton;
        public Color NotificationBanner;
        public Color ComingSoonBg;
        public Color DefaultLabel;

        [Header("Fonts")]
        [Tooltip("Optional; when null UiFactory uses PhoneFontSettings / TMP defaults.")]
        public PhoneFontSettings FontSettings;

        [Serializable]
        public struct AppIconColorEntry
        {
            public string AppId;
            public Color Color;
        }

        public Color AppIconColor(string appId)
        {
            if (AppIconColors != null)
            {
                for (int i = 0; i < AppIconColors.Length; i++)
                {
                    if (string.Equals(AppIconColors[i].AppId, appId, StringComparison.Ordinal))
                        return AppIconColors[i].Color;
                }
            }
            return AppIconFallback;
        }

        /// <summary>Unity calls this when a Theme is created from the Create Asset menu.</summary>
        void Reset() => ApplyBuiltinDefaults();

        public void ApplyBuiltinDefaults()
        {
            MessagingRoot = new Color(0.97f, 0.97f, 0.98f, 1f);
            ThreadListBg = new Color(0.97f, 0.97f, 0.98f, 1f);
            ThreadBg = new Color(0.94f, 0.95f, 0.97f, 1f);
            ScrollViewportTint = new Color(1f, 1f, 1f, 0.01f);
            ComposerBg = new Color(0.92f, 0.93f, 0.95f, 1f);
            ComposerInputBg = Color.white;
            ComposerPlaceholder = new Color(0.55f, 0.55f, 0.6f, 1f);
            SendButton = new Color(0.2f, 0.55f, 0.95f, 1f);
            SendButtonLabel = Color.white;
            NewMessagesPill = new Color(0.2f, 0.45f, 0.9f, 0.95f);
            NewMessagesPillLabel = Color.white;
            TypingLabel = new Color(0.35f, 0.35f, 0.4f, 1f);
            SeenLabel = new Color(0.4f, 0.45f, 0.5f, 1f);
            ThreadListRow = Color.white;
            ThreadListPreview = new Color(0.4f, 0.4f, 0.45f, 1f);
            UnreadBadge = new Color(0.9f, 0.25f, 0.3f, 1f);
            UnreadBadgeLabel = Color.white;
            TimestampLabel = new Color(0.45f, 0.45f, 0.5f, 1f);
            BubbleRowTransparent = new Color(0f, 0f, 0f, 0f);
            ContactNameFallback = Color.gray;

            PlayerBubble = new Color(0.25f, 0.55f, 0.95f, 1f);
            PlayerBubbleText = Color.white;
            NpcBubbleFallback = new Color(0.88f, 0.88f, 0.9f, 1f);
            NpcBubbleText = new Color(0.12f, 0.12f, 0.14f, 1f);
            NpcBubbleLerpToWhite = 0.55f;

            ReceiptGrey = new Color(0.75f, 0.82f, 0.95f, 0.85f);
            ReceiptRead = new Color(0.45f, 0.75f, 1f, 1f);

            ImageThumbFrame = new Color(0.08f, 0.08f, 0.1f, 1f);
            ImageFileNameOnPlayer = new Color(0.85f, 0.9f, 1f, 0.9f);
            ImageFileNameOnNpc = new Color(0.35f, 0.35f, 0.4f, 1f);
            ImageViewerDim = new Color(0.05f, 0.05f, 0.07f, 0.96f);
            ImageViewerFrame = new Color(0.1f, 0.1f, 0.12f, 1f);
            ImageViewerClose = new Color(0.28f, 0.3f, 0.34f, 1f);
            ImageViewerCloseLabel = Color.white;
            ImageViewerTitle = Color.white;

            HomeBg = new Color(0.68f, 0.74f, 0.84f, 1f);
            HomeTitle = new Color(0.08f, 0.09f, 0.12f, 1f);
            HomeIconTile = new Color(1f, 1f, 1f, 0.95f);
            HomeIconLabel = new Color(0.08f, 0.09f, 0.12f, 1f);
            AppIconFallback = new Color(0.45f, 0.5f, 0.55f, 1f);
            // Package-neutral ids only; host games put cast/app ids on their own theme asset.
            AppIconColors = new[]
            {
                new AppIconColorEntry { AppId = "messages", Color = new Color(0.15f, 0.65f, 0.35f, 1f) },
                new AppIconColorEntry { AppId = "app_a", Color = new Color(0.25f, 0.4f, 0.9f, 1f) },
                new AppIconColorEntry { AppId = "app_b", Color = new Color(0.9f, 0.45f, 0.2f, 1f) },
                new AppIconColorEntry { AppId = "app_c", Color = new Color(0.65f, 0.3f, 0.8f, 1f) },
            };

            StatusBarBg = new Color(0.94f, 0.95f, 0.97f, 1f);
            StatusBarText = new Color(0.08f, 0.09f, 0.12f, 1f);
            Letterbox = new Color(0.06f, 0.07f, 0.09f, 1f);
            PhoneStage = new Color(0.12f, 0.13f, 0.16f, 1f);
            SafeArea = new Color(0.72f, 0.76f, 0.84f, 1f);
            NavBar = new Color(0.92f, 0.93f, 0.95f, 1f);
            NavButton = new Color(0.82f, 0.84f, 0.88f, 1f);
            NotificationBanner = new Color(1f, 1f, 1f, 0.96f);
            ComingSoonBg = new Color(0.98f, 0.98f, 0.99f, 1f);
            DefaultLabel = new Color(0.12f, 0.12f, 0.14f, 1f);

            FontSettings = null;
        }
    }
}
