using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.PhoneUI
{
    /// <summary>
    /// Phone shell: status, home, apps, nav. Portrait stage uses 360 dp + uniform scale
    /// via <see cref="PhoneAspectFitter"/> (outer canvas Constant Pixel Size).
    /// </summary>
    public sealed class PhoneShell : MonoBehaviour
    {
        readonly List<IPhoneApp> _apps = new();
        readonly List<RectTransform> _screenOverlays = new();
        IPhoneApp _openApp;
        IDeviceStatusProvider _status;
        IChatSource _chat;
        IContactDirectory _contacts;

        RectTransform _safeArea;
        RectTransform _appHost;
        RectTransform _navBar;
        HomeScreenView _home;
        StatusBarView _statusBar;
        NotificationBannerView _banner;
        MessagingApp _messaging;
        PhoneAspectFitter _aspectFitter;
        TextMeshProUGUI _scaleOverlayLabel;
        bool _scaleOverlayEnabled;

        public MessagingApp Messaging => _messaging;
        public PhoneAspectFitter AspectFitter => _aspectFitter;

        /// <summary>
        /// Content area inside the phone frame, below the status bar and above the nav bar.
        /// Apps open here; hosts may parent fullscreen overlays such as login.
        /// </summary>
        public RectTransform ScreenContentRoot => _appHost;

        /// <summary>Full phone screen rect (safe area inside the bezel), used for clip/layout tests.</summary>
        public RectTransform PhoneScreenRoot => _safeArea;

        /// <summary>
        /// Mid-screen overlays (e.g. loading chip) parented under <see cref="PhoneScreenRoot"/>.
        /// Draw order: home → these → <see cref="ScreenContentRoot"/> → status/nav/banner.
        /// While a content overlay is open, ScreenContentRoot stays above these.
        /// </summary>
        public void RegisterScreenOverlay(RectTransform overlay)
        {
            if (overlay == null) return;
            if (!_screenOverlays.Contains(overlay))
                _screenOverlays.Add(overlay);
            EnsureChromeOnTop();
        }

        /// <summary>
        /// Hide home and clear/activate <see cref="ScreenContentRoot"/> for a host overlay
        /// (login, etc.). Call <see cref="EndContentOverlay"/> when done.
        /// </summary>
        public void BeginContentOverlay()
        {
            _banner?.Hide();
            _openApp?.Close();
            _openApp = null;
            _home?.Show(false);
            if (_appHost != null)
            {
                for (int i = _appHost.childCount - 1; i >= 0; i--)
                    UiFactory.SafeDestroy(_appHost.GetChild(i).gameObject);
                _appHost.gameObject.SetActive(true);
            }
            EnsureChromeOnTop();
        }

        /// <summary>Clear overlay children and return to the home screen.</summary>
        public void EndContentOverlay()
        {
            if (_appHost != null)
            {
                for (int i = _appHost.childCount - 1; i >= 0; i--)
                    UiFactory.SafeDestroy(_appHost.GetChild(i).gameObject);
            }
            ShowHome();
        }

        public bool ScaleOverlayEnabled
        {
            get => _scaleOverlayEnabled;
            set
            {
                _scaleOverlayEnabled = value;
                EnsureScaleOverlay();
                if (_scaleOverlayLabel != null)
                    _scaleOverlayLabel.gameObject.SetActive(_scaleOverlayEnabled);
                if (_scaleOverlayEnabled)
                    RefreshScaleOverlay();
            }
        }

        public void Initialize(
            IDeviceStatusProvider status,
            IChatSource chat,
            IContactDirectory contacts,
            IEnumerable<IPhoneApp> extraApps = null,
            Sprite messagingIcon = null)
        {
            _status = status ?? new DefaultDeviceStatusProvider();
            _chat = chat;
            _contacts = contacts;

            BuildFrame();

            _home = gameObject.AddComponent<HomeScreenView>();
            _home.Build(_safeArea);

            _appHost = UiFactory.Panel("AppHost", _safeArea, new Color(0f, 0f, 0f, 0f));
            UiFactory.Stretch(_appHost);
            var appImg = _appHost.GetComponent<Image>();
            appImg.raycastTarget = false;
            _appHost.offsetMin = new Vector2(0f, PhoneDp.NavBar);
            _appHost.offsetMax = new Vector2(0f, -PhoneDp.StatusBar);
            _appHost.gameObject.SetActive(false);

            _statusBar = gameObject.AddComponent<StatusBarView>();
            _statusBar.Build(_safeArea, _status);

            BuildNavBar();

            _banner = gameObject.AddComponent<NotificationBannerView>();
            _banner.Build(_safeArea);

            _messaging = new MessagingApp(_chat, _contacts, this, messagingIcon);
            _apps.Clear();
            _apps.Add(_messaging);
            if (extraApps != null)
            {
                foreach (var a in extraApps)
                    if (a != null) _apps.Add(a);
            }

            Debug.Log("[PhoneShell] Registering apps count=" + _apps.Count);
            _home.SetApps(_apps, OpenApp);
            EnsureChromeOnTop();
            ShowHome();
            _aspectFitter?.Apply();
            LogLayout("after Initialize");

            if (_chat != null)
                _chat.MessageDelivered += OnMessageDelivered;
        }

        void LateUpdate()
        {
            if (_scaleOverlayEnabled)
                RefreshScaleOverlay();
        }

        void EnsureChromeOnTop()
        {
            // home → registered screen overlays (loading) → content (apps/login) → status → nav → banner
            // Content stays above loading so BeginContentOverlay login is usable.
            int i = 0;
            if (_home != null && _home.Root != null) _home.Root.SetSiblingIndex(i++);
            for (int o = 0; o < _screenOverlays.Count; o++)
            {
                var overlay = _screenOverlays[o];
                if (overlay != null) overlay.SetSiblingIndex(i++);
            }
            if (_appHost != null) _appHost.SetSiblingIndex(i++);
            if (_statusBar != null && _statusBar.Bar != null) _statusBar.Bar.SetSiblingIndex(i++);
            if (_navBar != null) _navBar.SetSiblingIndex(i++);
            if (_banner != null && _banner.Root != null) _banner.Root.SetSiblingIndex(i++);
            if (_scaleOverlayLabel != null) _scaleOverlayLabel.rectTransform.SetSiblingIndex(i);
        }

        void EnsureScaleOverlay()
        {
            if (_scaleOverlayLabel != null || _safeArea == null) return;
            _scaleOverlayLabel = UiFactory.Label("ScaleOverlay", _safeArea, "scale: —", 11f, FontStyles.Bold);
            _scaleOverlayLabel.color = new Color(0.9f, 0.15f, 0.1f, 1f);
            _scaleOverlayLabel.alignment = TextAlignmentOptions.TopRight;
            var rt = _scaleOverlayLabel.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(0f, 20f);
            rt.anchoredPosition = new Vector2(0f, -PhoneDp.StatusBar - 2f);
            var m = _scaleOverlayLabel.margin;
            m.z = 8f;
            _scaleOverlayLabel.margin = m;
            _scaleOverlayLabel.gameObject.SetActive(false);
        }

        void RefreshScaleOverlay()
        {
            if (_scaleOverlayLabel == null || _aspectFitter == null) return;
            _scaleOverlayLabel.text = "scale: " + _aspectFitter.LastScale.ToString("0.00") +
                                      ", width dp: " + PhoneAspectFitter.ReferenceWidthDp.ToString("0");
        }

        public string DescribeScale()
        {
            if (_aspectFitter == null) return "n/a";
            return "scale=" + _aspectFitter.LastScale.ToString("0.00") +
                   " colW=" + _aspectFitter.LastColumnWidthPx.ToString("F0") +
                   " hDp=" + _aspectFitter.LastLogicalHeightDp.ToString("F0");
        }

        public void ShowTestBanner(string title = "Test", string body = "Dev console banner")
        {
            _banner?.Show(title, body, () =>
            {
                if (_messaging != null)
                    OpenMessagesThread("the_group");
            });
        }

        void LogLayout(string tag)
        {
            var sb = new StringBuilder();
            sb.Append("[PhoneShell] ").Append(tag);
            sb.Append(" apps=").Append(_apps.Count);
            sb.Append(" screen=").Append(Screen.width).Append('x').Append(Screen.height);
            if (_aspectFitter != null)
                sb.Append(" ").Append(DescribeScale());
            Debug.Log(sb.ToString());
        }

        void OnDestroy()
        {
            if (_chat != null)
                _chat.MessageDelivered -= OnMessageDelivered;
            _openApp?.Close();
        }

        void BuildFrame()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
                gameObject.AddComponent<GraphicRaycaster>();
            }
            else
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = 1f;
                }
            }

            var theme = PhoneUITheme.Active;
            var letterbox = UiFactory.Panel("Letterbox", transform, theme.Letterbox);
            UiFactory.Stretch(letterbox);

            var stage = UiFactory.Panel("PhoneStage", letterbox, theme.PhoneStage);
            _aspectFitter = stage.gameObject.AddComponent<PhoneAspectFitter>();
            _aspectFitter.Apply();

            _safeArea = UiFactory.Panel("SafeArea", stage, theme.SafeArea);
            UiFactory.Stretch(_safeArea);
            _safeArea.offsetMin = new Vector2(PhoneDp.BezelInset, PhoneDp.BezelInset);
            _safeArea.offsetMax = new Vector2(-PhoneDp.BezelInset, -PhoneDp.BezelInset);
            // Clip any app content that tries to paint outside the phone screen.
            if (_safeArea.GetComponent<RectMask2D>() == null)
                _safeArea.gameObject.AddComponent<RectMask2D>();

            Canvas.ForceUpdateCanvases();
            _aspectFitter.Apply();
        }

        void BuildNavBar()
        {
            _navBar = UiFactory.Panel("NavBar", _safeArea, PhoneUITheme.Active.NavBar);
            UiFactory.SetBottomBar(_navBar, PhoneDp.NavBar);
            var nav = _navBar;

            var back = UiFactory.Button("Back", nav, PhoneUITheme.Active.NavButton);
            var br = back.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.05f, 0.18f);
            br.anchorMax = new Vector2(0.32f, 0.82f);
            br.offsetMin = Vector2.zero;
            br.offsetMax = Vector2.zero;
            var bl = UiFactory.Label("L", back.transform, "Back", PhoneDp.NameText, FontStyles.Bold);
            UiFactory.Stretch(bl.rectTransform);
            bl.alignment = TextAlignmentOptions.Center;
            back.onClick.AddListener(GoBack);

            var home = UiFactory.Button("Home", nav, PhoneUITheme.Active.NavButton);
            var hr = home.GetComponent<RectTransform>();
            hr.anchorMin = new Vector2(0.36f, 0.18f);
            hr.anchorMax = new Vector2(0.64f, 0.82f);
            hr.offsetMin = Vector2.zero;
            hr.offsetMax = Vector2.zero;
            var hl = UiFactory.Label("L", home.transform, "Home", PhoneDp.NameText, FontStyles.Bold);
            UiFactory.Stretch(hl.rectTransform);
            hl.alignment = TextAlignmentOptions.Center;
            home.onClick.AddListener(ShowHome);
        }

        public void OpenApp(IPhoneApp app)
        {
            if (PhoneInputGate.BlockPhone) return;
            if (app == null) return;
            _banner.Hide();
            _openApp?.Close();
            _openApp = app;
            _home.Show(false);
            _appHost.gameObject.SetActive(true);
            for (int i = _appHost.childCount - 1; i >= 0; i--)
                UiFactory.SafeDestroy(_appHost.GetChild(i).gameObject);
            app.Open(_appHost);
            EnsureChromeOnTop();
        }

        public void ShowHome()
        {
            if (PhoneInputGate.BlockPhone) return;
            _banner.Hide();
            _openApp?.Close();
            _openApp = null;
            if (_appHost != null)
            {
                for (int i = _appHost.childCount - 1; i >= 0; i--)
                    UiFactory.SafeDestroy(_appHost.GetChild(i).gameObject);
                _appHost.gameObject.SetActive(false);
            }
            _home.Show(true);
            EnsureChromeOnTop();
        }

        public void GoBack()
        {
            if (PhoneInputGate.IgnoreBack) return;
            if (PhoneInputGate.BlockPhone) return;
            if (_openApp is MessagingApp msg && msg.TryHandleBack())
                return;
            if (_openApp != null)
            {
                ShowHome();
                return;
            }
        }

        public void OpenMessagesThread(string threadId)
        {
            if (PhoneInputGate.BlockPhone) return;
            OpenApp(_messaging);
            _messaging.OpenThread(threadId);
        }

        void OnMessageDelivered(ChatMessage msg)
        {
            if (msg == null || msg.FromPlayer) return;
            bool inThread = _openApp == _messaging && _messaging.IsShowingThread(msg.ThreadId);
            if (inThread) return;
            var contact = _contacts?.GetContact(msg.SenderId);
            string title = contact?.DisplayName ?? "Message";
            _banner.Show(title, msg.Text, () => OpenMessagesThread(msg.ThreadId));
        }
    }
}
