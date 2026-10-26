using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmberScreen.PhoneUI
{
    public sealed class MessagingApp : IPhoneApp
    {
        readonly IChatSource _chat;
        readonly IContactDirectory _contacts;
        readonly PhoneShell _shell;
        readonly Sprite _icon;
        RectTransform _threadHeaderAvatar;

        RectTransform _root;
        RectTransform _listPage;
        RectTransform _threadPage;
        RectTransform _threadContent;
        RectTransform _listContent;
        ScrollRect _threadScroll;
        TMP_InputField _input;
        TextMeshProUGUI _typingLabel;
        TextMeshProUGUI _seenLabel;
        TextMeshProUGUI _threadTitle;
        Button _newMessagesPill;
        string _openThreadId;
        bool _stickToBottom = true;
        readonly HashSet<string> _typingIds = new();
        string _lastPlayerMessageId;
        readonly List<GameObject> _listRows = new();
        readonly List<GameObject> _bubbles = new();
        readonly HashSet<string> _renderedMessageIds = new();
        DateTime? _lastBubbleMinute;
        GameObject _imageViewer;
        readonly Dictionary<string, TextMeshProUGUI> _receiptLabels =
            new Dictionary<string, TextMeshProUGUI>(StringComparer.Ordinal);

        public MessagingApp(IChatSource chat, IContactDirectory contacts, PhoneShell shell, Sprite icon = null)
        {
            _chat = chat;
            _contacts = contacts;
            _shell = shell;
            _icon = icon;
        }

        public string Id => "messages";
        public string DisplayName => "Messages";
        public Sprite Icon => _icon;

        bool IsAlive => _root != null;

        public void Open(RectTransform host)
        {
            Close();
            _root = UiFactory.Panel("MessagingApp", host, PhoneUITheme.Active.MessagingRoot);
            UiFactory.Stretch(_root);
            if (_root.GetComponent<RectMask2D>() == null)
                _root.gameObject.AddComponent<RectMask2D>();
            BuildListPage();
            BuildThreadPage();
            ShowList();
            Subscribe(true);
            RefreshThreadList();
        }

        public void Close()
        {
            Subscribe(false);
            _typingIds.Clear();
            _openThreadId = null;
            _listRows.Clear();
            _bubbles.Clear();
            _renderedMessageIds.Clear();
            _receiptLabels.Clear();
            _lastBubbleMinute = null;
            _lastPlayerMessageId = null;
            _listContent = null;
            _threadContent = null;
            _threadScroll = null;
            _input = null;
            _typingLabel = null;
            _seenLabel = null;
            _threadTitle = null;
            _newMessagesPill = null;
            _listPage = null;
            _threadPage = null;
            CloseImageViewerIfOpen();
            if (_root != null)
            {
                UiFactory.SafeDestroy(_root.gameObject);
                _root = null;
            }
        }

        public bool TryHandleBack()
        {
            if (!IsAlive) return false;
            // DevConsole Esc closes first (host sets IgnoreBack via ConsumedCloseThisFrame).
            if (PhoneInputGate.IgnoreBack || PhoneInputGate.BlockPhone) return false;
            // Viewer owned Esc closer may have already closed this frame — consume it.
            if (ImageViewerEscCloser.LastClosedFrame == Time.frameCount)
            {
                _imageViewer = null;
                return true;
            }
            if (CloseImageViewerIfOpen())
                return true;
            if (_threadPage != null && _threadPage.gameObject.activeSelf)
            {
                ShowList();
                return true;
            }
            return false;
        }

        bool CloseImageViewerIfOpen()
        {
            if (_imageViewer == null) return false;
            var closer = _imageViewer.GetComponent<ImageViewerEscCloser>();
            if (closer != null) closer.Close();
            else UiFactory.SafeDestroy(_imageViewer);
            _imageViewer = null;
            return true;
        }

        public bool IsShowingThread(string threadId) =>
            IsAlive && _threadPage != null && _threadPage.gameObject.activeSelf && _openThreadId == threadId;

        public void OpenThread(string threadId)
        {
            if (!IsAlive) return;
            _openThreadId = threadId;
            _chat?.MarkThreadRead(threadId);
            ShowThread();
            RebuildThreadMessages();
            MarkStickBottom();
            RefreshThreadList();
        }

        void Subscribe(bool on)
        {
            if (_chat == null) return;
            if (on)
            {
                _chat.ThreadsChanged += OnThreadsChanged;
                _chat.Seen += OnSeen;
                _chat.TypingStarted += OnTypingStarted;
                _chat.TypingStopped += OnTypingStopped;
                _chat.MessageDelivered += OnMessageDelivered;
            }
            else
            {
                _chat.ThreadsChanged -= OnThreadsChanged;
                _chat.Seen -= OnSeen;
                _chat.TypingStarted -= OnTypingStarted;
                _chat.TypingStopped -= OnTypingStopped;
                _chat.MessageDelivered -= OnMessageDelivered;
            }
        }

        static void ConfigureScrollContent(RectTransform content, bool topPivot)
        {
            // Width follows parent; height from ContentSizeFitter (vertical only).
            if (topPivot)
            {
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                content.anchorMin = new Vector2(0f, 0f);
                content.anchorMax = new Vector2(1f, 0f);
                content.pivot = new Vector2(0.5f, 0f);
            }
            content.offsetMin = new Vector2(0f, content.offsetMin.y);
            content.offsetMax = new Vector2(0f, content.offsetMax.y);
            content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
            content.localScale = Vector3.one;
        }

        void BuildListPage()
        {
            _listPage = UiFactory.Panel("ThreadList", _root, PhoneUITheme.Active.ThreadListBg);
            UiFactory.Stretch(_listPage);

            var header = UiFactory.Label("Hdr", _listPage, "Messages", PhoneDp.TitleText, FontStyles.Bold);
            var hr = header.rectTransform;
            hr.anchorMin = new Vector2(0f, 1f);
            hr.anchorMax = new Vector2(1f, 1f);
            hr.pivot = new Vector2(0.5f, 1f);
            hr.sizeDelta = new Vector2(0f, 40f);
            hr.anchoredPosition = Vector2.zero;
            header.alignment = TextAlignmentOptions.Center;

            var scrollGo = new GameObject("ListScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            var srt = scrollGo.GetComponent<RectTransform>();
            srt.SetParent(_listPage, false);
            UiFactory.Stretch(srt);
            srt.offsetMin = new Vector2(0f, 0f);
            srt.offsetMax = new Vector2(0f, -40f);
            scrollGo.GetComponent<Image>().color = PhoneUITheme.Active.ScrollViewportTint;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var crt = content.GetComponent<RectTransform>();
            crt.SetParent(srt, false);
            ConfigureScrollContent(crt, topPivot: true);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 2f;
            var fit = content.GetComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollGo.GetComponent<ScrollRect>();
            sr.content = crt;
            sr.viewport = srt;
            sr.horizontal = false;
            sr.vertical = true;
            _listPage.gameObject.SetActive(true);
            _listContent = crt;
        }

        void BuildThreadPage()
        {
            _threadPage = UiFactory.Panel("Thread", _root, PhoneUITheme.Active.ThreadBg);
            UiFactory.Stretch(_threadPage);
            _threadPage.gameObject.SetActive(false);

            _threadHeaderAvatar = UiFactory.CircleAvatar(
                "ThreadAvatar", _threadPage, null, 28f, PhoneUITheme.Active.AppIconFallback);
            _threadHeaderAvatar.anchorMin = new Vector2(0f, 1f);
            _threadHeaderAvatar.anchorMax = new Vector2(0f, 1f);
            _threadHeaderAvatar.pivot = new Vector2(0f, 1f);
            _threadHeaderAvatar.anchoredPosition = new Vector2(8f, -4f);

            var title = UiFactory.Label("ThreadTitle", _threadPage, "Chat", PhoneDp.BodyText, FontStyles.Bold);
            var tr = title.rectTransform;
            tr.anchorMin = new Vector2(0f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.sizeDelta = new Vector2(0f, 36f);
            tr.offsetMin = new Vector2(40f, tr.offsetMin.y);
            title.alignment = TextAlignmentOptions.Center;
            _threadTitle = title;

            var scrollGo = new GameObject("ThreadScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            var srt = scrollGo.GetComponent<RectTransform>();
            srt.SetParent(_threadPage, false);
            UiFactory.Stretch(srt);
            srt.offsetMin = new Vector2(0f, PhoneDp.Composer + 28f);
            srt.offsetMax = new Vector2(0f, -36f);
            scrollGo.GetComponent<Image>().color = PhoneUITheme.Active.ScrollViewportTint;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _threadContent = content.GetComponent<RectTransform>();
            _threadContent.SetParent(srt, false);
            ConfigureScrollContent(_threadContent, topPivot: false);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fit = content.GetComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _threadScroll = scrollGo.GetComponent<ScrollRect>();
            _threadScroll.content = _threadContent;
            _threadScroll.viewport = srt;
            _threadScroll.horizontal = false;
            _threadScroll.vertical = true;
            _threadScroll.onValueChanged.AddListener(OnThreadScroll);

            _typingLabel = UiFactory.Label("Typing", _threadPage, "", PhoneDp.TimestampText);
            _typingLabel.color = PhoneUITheme.Active.TypingLabel;
            var tyr = _typingLabel.rectTransform;
            tyr.anchorMin = new Vector2(0f, 0f);
            tyr.anchorMax = new Vector2(1f, 0f);
            tyr.pivot = new Vector2(0.5f, 0f);
            tyr.sizeDelta = new Vector2(0f, 16f);
            tyr.anchoredPosition = new Vector2(0f, PhoneDp.Composer + 4f);
            _typingLabel.alignment = TextAlignmentOptions.Left;
            var tipPad = _typingLabel.margin;
            tipPad.x = 12f;
            _typingLabel.margin = tipPad;

            _seenLabel = UiFactory.Label("Seen", _threadPage, "", PhoneDp.TimestampText);
            _seenLabel.color = PhoneUITheme.Active.SeenLabel;
            var srSeen = _seenLabel.rectTransform;
            srSeen.anchorMin = new Vector2(0f, 0f);
            srSeen.anchorMax = new Vector2(1f, 0f);
            srSeen.pivot = new Vector2(1f, 0f);
            srSeen.sizeDelta = new Vector2(0f, 14f);
            srSeen.anchoredPosition = new Vector2(0f, PhoneDp.Composer + 18f);
            _seenLabel.alignment = TextAlignmentOptions.Right;
            var sm = _seenLabel.margin;
            sm.z = 12f;
            _seenLabel.margin = sm;

            var composer = UiFactory.Panel("Composer", _threadPage, PhoneUITheme.Active.ComposerBg);
            UiFactory.SetBottomBar(composer, PhoneDp.Composer);

            var inputGo = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            var irt = inputGo.GetComponent<RectTransform>();
            irt.SetParent(composer, false);
            irt.anchorMin = new Vector2(0.03f, 0.2f);
            irt.anchorMax = new Vector2(0.78f, 0.85f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            inputGo.GetComponent<Image>().color = PhoneUITheme.Active.ComposerInputBg;
            _input = inputGo.GetComponent<TMP_InputField>();
            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            var ta = textArea.GetComponent<RectTransform>();
            ta.SetParent(irt, false);
            UiFactory.Stretch(ta);
            ta.offsetMin = new Vector2(8f, 4f);
            ta.offsetMax = new Vector2(-8f, -4f);
            var placeholder = UiFactory.Label("Placeholder", ta, "Message", PhoneDp.BodyText);
            placeholder.color = PhoneUITheme.Active.ComposerPlaceholder;
            var text = UiFactory.Label("Text", ta, "", PhoneDp.BodyText);
            text.raycastTarget = true;
            text.overflowMode = TextOverflowModes.Overflow;
            _input.textViewport = ta;
            _input.textComponent = text;
            _input.placeholder = placeholder;
            _input.lineType = TMP_InputField.LineType.MultiLineSubmit;
            _input.onSubmit.AddListener(_ => Send());

            var send = UiFactory.Button("Send", composer, PhoneUITheme.Active.SendButton);
            var sendRt = send.GetComponent<RectTransform>();
            sendRt.anchorMin = new Vector2(0.8f, 0.2f);
            sendRt.anchorMax = new Vector2(0.97f, 0.85f);
            sendRt.offsetMin = Vector2.zero;
            sendRt.offsetMax = Vector2.zero;
            var sl = UiFactory.Label("L", send.transform, "Send", PhoneDp.NameText, FontStyles.Bold);
            sl.color = PhoneUITheme.Active.SendButtonLabel;
            UiFactory.Stretch(sl.rectTransform);
            sl.alignment = TextAlignmentOptions.Center;
            send.onClick.AddListener(Send);

            var pill = UiFactory.Button("NewMessages", _threadPage, PhoneUITheme.Active.NewMessagesPill);
            _newMessagesPill = pill;
            var pr = pill.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0.5f, 0f);
            pr.anchorMax = new Vector2(0.5f, 0f);
            pr.pivot = new Vector2(0.5f, 0f);
            pr.sizeDelta = new Vector2(132f, 28f);
            pr.anchoredPosition = new Vector2(0f, PhoneDp.Composer + 32f);
            var pl = UiFactory.Label("L", pill.transform, "New messages", PhoneDp.TimestampText, FontStyles.Bold);
            pl.color = PhoneUITheme.Active.NewMessagesPillLabel;
            UiFactory.Stretch(pl.rectTransform);
            pl.alignment = TextAlignmentOptions.Center;
            pill.onClick.AddListener(() =>
            {
                if (!IsAlive || _newMessagesPill == null) return;
                MarkStickBottom();
                ScrollToBottom();
                _newMessagesPill.gameObject.SetActive(false);
            });
            pill.gameObject.SetActive(false);
        }

        void ShowList()
        {
            if (!IsAlive) return;
            _openThreadId = null;
            _typingIds.Clear();
            _listPage.gameObject.SetActive(true);
            _threadPage.gameObject.SetActive(false);
            RefreshThreadList();
        }

        void ShowThread()
        {
            if (!IsAlive) return;
            _listPage.gameObject.SetActive(false);
            _threadPage.gameObject.SetActive(true);
            var t = _chat?.Threads?.FirstOrDefault(x => x.Id == _openThreadId);
            if (_threadTitle != null)
                _threadTitle.text = t?.Title ?? "Chat";
            RefreshThreadHeaderAvatar(t);
            if (_seenLabel != null) _seenLabel.text = "";
            UpdateTypingLabel();
        }

        void RefreshThreadHeaderAvatar(ChatThreadInfo thread)
        {
            if (_threadHeaderAvatar == null) return;
            var contact = ResolveThreadAvatarContact(thread);
            UiFactory.SetCircleAvatarContent(
                _threadHeaderAvatar,
                contact?.Avatar,
                ContactColorOr(contact, PhoneUITheme.Active.AppIconFallback));
        }

        ContactInfo ResolveThreadAvatarContact(ChatThreadInfo thread)
        {
            if (thread?.ParticipantIds == null || _contacts == null) return null;
            foreach (var id in thread.ParticipantIds)
            {
                var c = _contacts.GetContact(id);
                if (c != null && !c.IsPlayer) return c;
            }
            return _contacts.Player;
        }

        /// <summary>Alpha 0 on <see cref="ContactInfo.Color"/> means unset → theme fallback.</summary>
        static Color ContactColorOr(ContactInfo contact, Color themeFallback) =>
            contact != null && contact.Color.a > 0.001f ? contact.Color : themeFallback;

        float _cachedThreadContentWidth;

        /// <summary>
        /// Width for bubble layout. Call <see cref="CacheThreadContentWidth"/> once per rebuild;
        /// never ForceUpdate inside per-bubble layout (Unity logs recursive canvas rebuilds).
        /// </summary>
        float ThreadContentWidth()
        {
            if (_cachedThreadContentWidth >= 1f)
                return _cachedThreadContentWidth;
            float w = _threadContent != null ? _threadContent.rect.width : 0f;
            if (w < 1f)
                w = PhoneDp.ReferenceWidth - 20f; // padding fallback inside 360 dp screen
            return w;
        }

        void CacheThreadContentWidth()
        {
            if (_threadContent == null)
            {
                _cachedThreadContentWidth = PhoneDp.ReferenceWidth - 20f;
                return;
            }
            Canvas.ForceUpdateCanvases();
            float w = _threadContent.rect.width;
            _cachedThreadContentWidth = w >= 1f ? w : PhoneDp.ReferenceWidth - 20f;
        }

        void RefreshThreadList()
        {
            if (!IsAlive || _listContent == null || _chat == null) return;
            foreach (var go in _listRows) UiFactory.SafeDestroy(go);
            _listRows.Clear();
            foreach (var thread in _chat.Threads.OrderByDescending(t => t.LastMessageUtc))
            {
                var row = UiFactory.Button("Row_" + thread.Id, _listContent, PhoneUITheme.Active.ThreadListRow);
                var rt = row.GetComponent<RectTransform>();
                // Layout group drives width from parent; kill Stretch leftovers from MakeImage.
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, 56f);
                rt.localScale = Vector3.one;
                var le = row.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 56f;
                le.preferredHeight = 56f;
                le.flexibleWidth = 1f;

                var avatarContact = ResolveThreadAvatarContact(thread);
                var av = UiFactory.CircleAvatar(
                    "Av", row.transform, avatarContact?.Avatar, 40f,
                    ContactColorOr(avatarContact, PhoneUITheme.Active.AppIconFallback));
                av.anchorMin = new Vector2(0f, 0.5f);
                av.anchorMax = new Vector2(0f, 0.5f);
                av.pivot = new Vector2(0f, 0.5f);
                av.anchoredPosition = new Vector2(8f, 0f);

                var name = UiFactory.Label("Name", row.transform, thread.Title, PhoneDp.NameText, FontStyles.Bold);
                var nr = name.rectTransform;
                nr.anchorMin = new Vector2(0f, 0.45f);
                nr.anchorMax = new Vector2(0.7f, 1f);
                nr.offsetMin = new Vector2(56f, 0f);
                nr.offsetMax = new Vector2(0f, -4f);

                var preview = UiFactory.Label("Prev", row.transform, thread.LastMessagePreview ?? "", PhoneDp.TimestampText);
                preview.color = PhoneUITheme.Active.ThreadListPreview;
                preview.overflowMode = TextOverflowModes.Ellipsis;
                var pr = preview.rectTransform;
                pr.anchorMin = new Vector2(0f, 0f);
                pr.anchorMax = new Vector2(0.75f, 0.5f);
                pr.offsetMin = new Vector2(56f, 6f);
                pr.offsetMax = Vector2.zero;

                var time = UiFactory.Label("Time", row.transform, FormatTime(thread.LastMessageUtc), PhoneDp.TimestampText);
                time.alignment = TextAlignmentOptions.Right;
                var tmr = time.rectTransform;
                tmr.anchorMin = new Vector2(0.7f, 0.5f);
                tmr.anchorMax = new Vector2(1f, 1f);
                tmr.offsetMin = Vector2.zero;
                tmr.offsetMax = new Vector2(-10f, -6f);

                if (thread.UnreadCount > 0)
                {
                    var badge = UiFactory.MakeImage("Badge", row.transform, PhoneUITheme.Active.UnreadBadge);
                    var br = badge.rectTransform;
                    br.anchorMin = new Vector2(1f, 0f);
                    br.anchorMax = new Vector2(1f, 0f);
                    br.pivot = new Vector2(1f, 0f);
                    br.sizeDelta = new Vector2(22f, 22f);
                    br.anchoredPosition = new Vector2(-12f, 8f);
                    var bt = UiFactory.Label("N", badge.transform, thread.UnreadCount.ToString(), 11f, FontStyles.Bold);
                    bt.color = PhoneUITheme.Active.UnreadBadgeLabel;
                    bt.alignment = TextAlignmentOptions.Center;
                    UiFactory.Stretch(bt.rectTransform);
                }

                string id = thread.Id;
                row.onClick.AddListener(() =>
                {
                    if (IsAlive) OpenThread(id);
                });
                _listRows.Add(row.gameObject);
            }
        }

        void RebuildThreadMessages()
        {
            if (!IsAlive || _threadContent == null || _chat == null || string.IsNullOrEmpty(_openThreadId)) return;
            ClearBubbles();
            CacheThreadContentWidth();
            foreach (var msg in _chat.GetHistory(_openThreadId))
                AppendMessageIfNew(msg);
            Canvas.ForceUpdateCanvases();
            if (_stickToBottom) ScrollToBottom();
        }

        void ClearBubbles()
        {
            foreach (var go in _bubbles) UiFactory.SafeDestroy(go);
            _bubbles.Clear();
            _renderedMessageIds.Clear();
            _receiptLabels.Clear();
            _lastBubbleMinute = null;
            _lastPlayerMessageId = null;
        }

        void AppendMissingFromHistory()
        {
            if (!IsAlive || _chat == null || string.IsNullOrEmpty(_openThreadId)) return;
            foreach (var msg in _chat.GetHistory(_openThreadId))
                AppendMessageIfNew(msg);
            if (_stickToBottom) ScrollToBottom();
        }

        void AppendMessageIfNew(ChatMessage msg)
        {
            if (!IsAlive || msg == null || _threadContent == null) return;
            if (_renderedMessageIds.Contains(msg.Id)) return;

            var minute = TruncateMinute(msg.TimestampUtc);
            if (_lastBubbleMinute == null || minute != _lastBubbleMinute.Value)
            {
                AddTimestamp(minute);
                _lastBubbleMinute = minute;
            }
            AddBubble(msg);
            _renderedMessageIds.Add(msg.Id);
            if (msg.FromPlayer) _lastPlayerMessageId = msg.Id;
        }

        static DateTime TruncateMinute(DateTime utc) =>
            new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, DateTimeKind.Utc);

        void AddTimestamp(DateTime utc)
        {
            var local = utc.ToLocalTime();
            var label = UiFactory.Label("Ts", _threadContent, local.ToString("HH:mm"), PhoneDp.TimestampText);
            label.alignment = TextAlignmentOptions.Center;
            label.color = PhoneUITheme.Active.TimestampLabel;
            var le = label.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 18f;
            le.preferredHeight = 18f;
            le.flexibleWidth = 1f;
            _bubbles.Add(label.gameObject);
        }

        void AddBubble(ChatMessage msg)
        {
            var contact = _contacts?.GetContact(msg.SenderId);
            bool mine = msg.FromPlayer;

            // Row: width from parent VLG (not Screen / not Stretch-to-canvas).
            var rowGo = new GameObject("Bubble_" + msg.Id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            var row = rowGo.GetComponent<RectTransform>();
            row.SetParent(_threadContent, false);
            row.localScale = Vector3.one;
            var rowImg = rowGo.GetComponent<Image>();
            rowImg.color = PhoneUITheme.Active.BubbleRowTransparent;
            rowImg.raycastTarget = false;
            var rowLe = rowGo.GetComponent<LayoutElement>();
            rowLe.minHeight = 36f;
            rowLe.flexibleWidth = 1f;

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = mine ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(mine ? 8 : 4, mine ? 4 : 8, 0, 0);
            hlg.spacing = 6f;

            float contentW = ThreadContentWidth();
            float maxBubble = contentW * PhoneDp.BubbleMaxWidthFraction;
            var theme = PhoneUITheme.Active;

            // Sender avatar (NPC in group/DM; player avatar on own messages).
            var avatarContact = mine ? _contacts?.Player : contact;
            var avRt = UiFactory.CircleAvatar(
                "Av", row, avatarContact?.Avatar, 28f,
                ContactColorOr(avatarContact, theme.AppIconFallback));
            var avLe = avRt.gameObject.AddComponent<LayoutElement>();
            avLe.minWidth = 28f;
            avLe.minHeight = 28f;
            avLe.preferredWidth = 28f;
            avLe.preferredHeight = 28f;
            avLe.flexibleWidth = 0f;
            if (mine)
                avRt.SetAsLastSibling();

            var bubbleColor = mine
                ? theme.PlayerBubble
                : ContactColorOr(contact, theme.NpcBubbleFallback);
            if (!mine)
                bubbleColor = Color.Lerp(bubbleColor, Color.white, theme.NpcBubbleLerpToWhite);

            var bubble = UiFactory.MakeImage("B", row, bubbleColor);
            if (mine)
                bubble.transform.SetSiblingIndex(0);
            var brt = bubble.rectTransform;
            brt.localScale = Vector3.one;
            var vlg = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 6, 6);
            vlg.spacing = 2f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Horizontal: driven by LayoutElement (capped). Vertical: preferred from text.
            var fitter = bubble.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var ble = bubble.gameObject.AddComponent<LayoutElement>();
            ble.flexibleWidth = 0f;
            ble.minWidth = 48f;

            if (!mine)
            {
                var name = UiFactory.Label("N", bubble.transform, contact?.DisplayName ?? msg.SenderId, PhoneDp.NameText, FontStyles.Bold);
                name.color = ContactColorOr(contact, PhoneUITheme.Active.ContactNameFallback);
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (msg.Image != null && msg.Image.ResolvedTexture != null)
            {
                AddImageAttachment(bubble.transform, msg, ble, maxBubble, mine);
            }
            else
            {
                string bodyText = msg.Text ?? "";
                if (!string.IsNullOrEmpty(msg.LinkId) && !string.IsNullOrEmpty(bodyText))
                    bodyText = bodyText + "\n↗ open link";
                var body = UiFactory.Label("T", bubble.transform, bodyText, PhoneDp.BodyText);
                body.color = mine ? theme.PlayerBubbleText : theme.NpcBubbleText;
                body.textWrappingMode = TextWrappingModes.Normal;
                body.overflowMode = TextOverflowModes.Overflow;
                body.raycastTarget = false;

                body.ForceMeshUpdate();
                float padX = 20f;
                float ideal = body.preferredWidth + padX;
                if (!mine) ideal = Mathf.Max(ideal, 80f);
                ble.preferredWidth = Mathf.Clamp(ideal, 48f, maxBubble);

                var bodyLe = body.gameObject.AddComponent<LayoutElement>();
                bodyLe.preferredWidth = ble.preferredWidth - padX;
                bodyLe.flexibleWidth = 1f;
            }

            if (!string.IsNullOrEmpty(msg.LinkId) && msg.Image == null)
            {
                bubble.raycastTarget = true;
                var linkBtn = bubble.gameObject.GetComponent<Button>();
                if (linkBtn == null) linkBtn = bubble.gameObject.AddComponent<Button>();
                linkBtn.targetGraphic = bubble;
                var captured = msg;
                linkBtn.onClick.RemoveAllListeners();
                linkBtn.onClick.AddListener(() => _chat?.OpenMessageLink(captured));
            }

            if (mine && msg.Receipt != MessageReceipt.None)
            {
                var ticks = UiFactory.Label("Ticks", bubble.transform, ReceiptGlyph(msg.Receipt), PhoneDp.TimestampText);
                ticks.alignment = TextAlignmentOptions.Right;
                ticks.color = msg.Receipt == MessageReceipt.Read
                    ? theme.ReceiptRead
                    : theme.ReceiptGrey;
                ticks.textWrappingMode = TextWrappingModes.NoWrap;
                if (!string.IsNullOrEmpty(msg.Id))
                    _receiptLabels[msg.Id] = ticks;
            }

            _bubbles.Add(rowGo);
        }

        void AddImageAttachment(
            Transform bubble,
            ChatMessage msg,
            LayoutElement ble,
            float maxBubble,
            bool mine)
        {
            var att = msg.Image;
            float aspect = att.ResolvedAspect;
            float thumbW = Mathf.Min(maxBubble * 0.92f, maxBubble);
            float thumbH = thumbW / Mathf.Max(0.4f, aspect);
            thumbH = Mathf.Clamp(thumbH, 72f, 200f);
            thumbW = thumbH * aspect;
            if (thumbW > maxBubble)
            {
                thumbW = maxBubble;
                thumbH = thumbW / aspect;
            }

            ble.preferredWidth = Mathf.Clamp(thumbW + 20f, 80f, maxBubble);
            ble.minHeight = thumbH + 28f;

            var theme = PhoneUITheme.Active;
            var frame = UiFactory.MakeImage("ImgFrame", bubble, theme.ImageThumbFrame);
            var frt = frame.rectTransform;
            var fle = frame.gameObject.AddComponent<LayoutElement>();
            fle.preferredWidth = thumbW;
            fle.preferredHeight = thumbH;
            fle.flexibleWidth = 0f;

            var imgGo = new GameObject("Thumb", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(Button));
            var irt = imgGo.GetComponent<RectTransform>();
            irt.SetParent(frt, false);
            UiFactory.Stretch(irt);
            irt.offsetMin = new Vector2(3f, 3f);
            irt.offsetMax = new Vector2(-3f, -3f);
            var raw = imgGo.GetComponent<RawImage>();
            ApplyAttachmentTexture(raw, att);
            raw.color = Color.white;
            var btn = imgGo.GetComponent<Button>();
            btn.targetGraphic = raw;
            var captured = msg;
            btn.onClick.AddListener(() => OpenImageViewer(captured));

            string fileLabel = string.IsNullOrEmpty(att.FileName) ? "image" : att.FileName;
            var fname = UiFactory.Label("File", bubble, fileLabel, PhoneDp.TimestampText);
            fname.color = mine ? theme.ImageFileNameOnPlayer : theme.ImageFileNameOnNpc;
            fname.alignment = TextAlignmentOptions.Left;
            fname.overflowMode = TextOverflowModes.Ellipsis;
            fname.textWrappingMode = TextWrappingModes.NoWrap;
        }

        static void ApplyAttachmentTexture(RawImage raw, ChatImageAttachment att)
        {
            if (raw == null || att == null) return;
            // Packed sprites: use the sprite UV rect so atlas sub-rects display correctly.
            if (att.Sprite != null)
            {
                raw.texture = att.Sprite.texture;
                raw.uvRect = ChatImageAttachment.SpriteUvRect(att.Sprite);
                return;
            }
            raw.texture = att.ResolvedTexture;
            raw.uvRect = new Rect(0f, 0f, 1f, 1f);
        }

        void OpenImageViewer(ChatMessage msg)
        {
            if (msg?.Image == null || _shell == null) return;
            var root = _shell.ScreenContentRoot;
            if (root == null) return;
            CloseImageViewerIfOpen();
            _imageViewer = ImageMessageViewer.Show(root, msg.Image, msg.Image.FileName, () =>
            {
                _imageViewer = null;
            }, listenForEscape: false);
        }

        static string ReceiptGlyph(MessageReceipt r)
        {
            switch (r)
            {
                case MessageReceipt.Sent: return "✓";
                case MessageReceipt.Delivered: return "✓✓";
                case MessageReceipt.Read: return "✓✓";
                default: return "";
            }
        }

        void Send()
        {
            if (PhoneInputGate.BlockPhone) return;
            if (!IsAlive || _input == null || string.IsNullOrEmpty(_openThreadId)) return;
            string text = (_input.text ?? "").Trim();
            if (text.Length == 0) return;
            _input.text = "";
            _chat.SendPlayerMessage(_openThreadId, text);
            _stickToBottom = true;
            if (_newMessagesPill != null) _newMessagesPill.gameObject.SetActive(false);
            if (_seenLabel != null) _seenLabel.text = "";
        }

        void OnThreadsChanged()
        {
            if (!IsAlive) return;
            RefreshThreadList();
            if (IsShowingThread(_openThreadId))
            {
                AppendMissingFromHistory();
                RefreshReceiptTicks();
            }
        }

        /// <summary>
        /// Receipt ticks are baked into bubbles at create time; refresh glyphs when the
        /// host upgrades Sent → Delivered → Read on the same <see cref="ChatMessage"/> instances.
        /// </summary>
        void RefreshReceiptTicks()
        {
            if (!IsAlive || _chat == null || string.IsNullOrEmpty(_openThreadId)) return;
            foreach (var msg in _chat.GetHistory(_openThreadId))
            {
                if (msg == null || !msg.FromPlayer || string.IsNullOrEmpty(msg.Id)) continue;
                if (!_receiptLabels.TryGetValue(msg.Id, out var ticks) || ticks == null) continue;
                ticks.text = ReceiptGlyph(msg.Receipt);
                ticks.color = msg.Receipt == MessageReceipt.Read
                    ? PhoneUITheme.Active.ReceiptRead
                    : PhoneUITheme.Active.ReceiptGrey;
            }
        }

        void OnSeen(string threadId, string contactId)
        {
            if (!IsAlive || _seenLabel == null) return;
            if (threadId != _openThreadId) return;
            var c = _contacts?.GetContact(contactId);
            _seenLabel.text = "Seen by " + (c?.DisplayName ?? contactId);
        }

        void OnTypingStarted(string threadId, string contactId)
        {
            if (!IsAlive) return;
            if (threadId != _openThreadId) return;
            _typingIds.Add(contactId);
            UpdateTypingLabel();
        }

        void OnTypingStopped(string threadId, string contactId)
        {
            if (!IsAlive) return;
            if (threadId != _openThreadId) return;
            _typingIds.Remove(contactId);
            UpdateTypingLabel();
        }

        void OnMessageDelivered(ChatMessage msg)
        {
            if (!IsAlive || msg == null) return;
            if (IsShowingThread(msg.ThreadId))
            {
                AppendMessageIfNew(msg);
                if (_stickToBottom)
                {
                    ScrollToBottom();
                    if (_newMessagesPill != null) _newMessagesPill.gameObject.SetActive(false);
                }
                else if (_newMessagesPill != null)
                    _newMessagesPill.gameObject.SetActive(true);
            }
            RefreshThreadList();
        }

        void UpdateTypingLabel()
        {
            if (_typingLabel == null) return;
            if (_typingIds.Count == 0)
            {
                _typingLabel.text = "";
                return;
            }
            var names = _typingIds
                .Select(id => _contacts?.GetContact(id)?.DisplayName ?? id)
                .ToList();
            string who = names.Count == 1 ? names[0] : string.Join(" & ", names);
            _typingLabel.text = who + " typing…";
        }

        void OnThreadScroll(Vector2 _)
        {
            if (!IsAlive || _threadScroll == null) return;
            _stickToBottom = _threadScroll.verticalNormalizedPosition <= 0.08f;
            if (_stickToBottom && _newMessagesPill != null)
                _newMessagesPill.gameObject.SetActive(false);
        }

        void MarkStickBottom() => _stickToBottom = true;

        void ScrollToBottom()
        {
            if (!IsAlive || _threadScroll == null) return;
            Canvas.ForceUpdateCanvases();
            _threadScroll.verticalNormalizedPosition = 0f;
        }

        static string FormatTime(DateTime utc)
        {
            var local = utc.ToLocalTime();
            if (local.Date == DateTime.Now.Date) return local.ToString("HH:mm");
            return local.ToString("MMM d", CultureInfo.InvariantCulture);
        }
    }
}
