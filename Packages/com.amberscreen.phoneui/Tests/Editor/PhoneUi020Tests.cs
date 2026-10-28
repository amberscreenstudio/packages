using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AmberScreen.PhoneUI.Tests
{
    public sealed class PhoneUi020Tests
    {
        [Test]
        public void Theme_Builtin_MatchesPriorHardCodedPlayerBubble()
        {
            var t = PhoneUITheme.Builtin;
            Assert.That(t.PlayerBubble.r, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(t.PlayerBubble.g, Is.EqualTo(0.55f).Within(0.001f));
            Assert.That(t.PlayerBubble.b, Is.EqualTo(0.95f).Within(0.001f));
            Assert.That(t.ImageViewerDim.a, Is.EqualTo(0.96f).Within(0.001f));
            Assert.That(t.MessagingRoot.r, Is.EqualTo(0.97f).Within(0.001f));
            Assert.That(t.HomeBg.r, Is.EqualTo(0.68f).Within(0.001f));
        }

        [Test]
        public void Theme_CreateInstance_Reset_FillsNonZeroColours()
        {
            var t = ScriptableObject.CreateInstance<PhoneUITheme>();
            // Zero out to simulate a Create-menu SO before Reset (Unity 6 may also call Reset on CreateInstance).
            t.PlayerBubble = default;
            t.HomeBg = default;
            t.MessagingRoot = default;
            t.ImageViewerDim = default;
            t.StatusBarBg = default;
            t.NavBar = default;
            Assert.That(t.PlayerBubble.a, Is.EqualTo(0f).Within(0.001f));

            var reset = typeof(PhoneUITheme).GetMethod(
                "Reset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(reset, Is.Not.Null, "PhoneUITheme.Reset must exist for Create Asset menu");
            reset.Invoke(t, null);

            Assert.That(t.PlayerBubble.a, Is.GreaterThan(0.5f));
            Assert.That(t.PlayerBubble.r + t.PlayerBubble.g + t.PlayerBubble.b, Is.GreaterThan(0.1f));
            Assert.That(t.HomeBg.a, Is.GreaterThan(0.5f));
            Assert.That(t.MessagingRoot.a, Is.GreaterThan(0.5f));
            Assert.That(t.ImageViewerDim.a, Is.GreaterThan(0.5f));
            Assert.That(t.StatusBarBg.a, Is.GreaterThan(0.5f));
            Assert.That(t.NavBar.a, Is.GreaterThan(0.5f));
            Object.DestroyImmediate(t);
        }

        [Test]
        public void SpriteUvRect_FullTexture_IsUnitRect()
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            var uv = ChatImageAttachment.SpriteUvRect(sprite);
            Assert.That(uv.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(uv.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(uv.width, Is.EqualTo(1f).Within(0.001f));
            Assert.That(uv.height, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void ImageViewer_Show_CreatesRoot_WithEscCloser()
        {
            var canvasGo = new GameObject("C", typeof(RectTransform), typeof(Canvas));
            var host = canvasGo.GetComponent<RectTransform>();
            var tex = new Texture2D(32, 48, TextureFormat.RGBA32, false);
            tex.Apply();
            var att = new ChatImageAttachment { FileName = "t.png", Texture = tex, Aspect = 32f / 48f };
            var viewer = ImageMessageViewer.Show(host, att, "t.png");
            Assert.That(viewer, Is.Not.Null);
            var closer = viewer.GetComponent<ImageViewerEscCloser>();
            Assert.That(closer, Is.Not.Null);
            Assert.That(closer.ListenForEscape, Is.True);
            Object.DestroyImmediate(viewer);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void ImageViewer_WhenMessagingOwnsBack_ListenForEscapeIsFalse()
        {
            var canvasGo = new GameObject("C", typeof(RectTransform), typeof(Canvas));
            var host = canvasGo.GetComponent<RectTransform>();
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.Apply();
            var att = new ChatImageAttachment { FileName = "x.png", Texture = tex, Aspect = 1f };
            var viewer = ImageMessageViewer.Show(host, att, "x.png", null, listenForEscape: false);
            Assert.That(viewer, Is.Not.Null);
            var closer = viewer.GetComponent<ImageViewerEscCloser>();
            Assert.That(closer.ListenForEscape, Is.False);
            Object.DestroyImmediate(viewer);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void ImageViewerEscCloser_Close_RecordsFrame_ForTryHandleBack()
        {
            var go = new GameObject("V", typeof(ImageViewerEscCloser));
            var closer = go.GetComponent<ImageViewerEscCloser>();
            closer.ListenForEscape = false;
            int before = ImageViewerEscCloser.LastClosedFrame;
            closer.Close();
            Assert.That(ImageViewerEscCloser.LastClosedFrame, Is.GreaterThanOrEqualTo(before));
            Assert.That(ImageViewerEscCloser.LastClosedFrame, Is.EqualTo(Time.frameCount));
        }

        [Test]
        public void PackageInfo_NameVersionDependencies()
        {
            var info = PackageInfo.FindForAssembly(typeof(PhoneUITheme).Assembly);
            Assert.That(info, Is.Not.Null);
            Assert.That(info.name, Is.EqualTo("com.amberscreen.phoneui"));
            Assert.That(info.version, Is.EqualTo("0.2.3"));
            Assert.That(info.dependencies, Has.Some.Matches<UnityEditor.PackageManager.DependencyInfo>(
                d => d.name == "com.unity.inputsystem"));
            Assert.That(info.dependencies, Has.Some.Matches<UnityEditor.PackageManager.DependencyInfo>(
                d => d.name == "com.unity.ugui"));
        }

        [Test]
        public void Composer_PlaceholderAndText_FillTextArea()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var shellGo = new GameObject("PhoneShell", typeof(RectTransform), typeof(PhoneShell));
            shellGo.transform.SetParent(canvasGo.transform, false);
            var shell = shellGo.GetComponent<PhoneShell>();
            var contacts = new TestContacts();
            var chat = new TestChat();
            shell.Initialize(null, chat, contacts);
            shell.OpenMessagesThread("t1");

            var textArea = FindDeep(shellGo.transform, "Text Area");
            Assert.That(textArea, Is.Not.Null);
            var placeholder = textArea.Find("Placeholder");
            var text = textArea.Find("Text");
            Assert.That(placeholder, Is.Not.Null);
            Assert.That(text, Is.Not.Null);

            AssertFill(placeholder.GetComponent<RectTransform>());
            AssertFill(text.GetComponent<RectTransform>());

            var ph = placeholder.GetComponent<TextMeshProUGUI>();
            var tx = text.GetComponent<TextMeshProUGUI>();
            Assert.That(ph.alignment, Is.EqualTo(TextAlignmentOptions.MidlineLeft));
            Assert.That(tx.alignment, Is.EqualTo(TextAlignmentOptions.MidlineLeft));
            Assert.That(ph.textWrappingMode, Is.EqualTo(TextWrappingModes.NoWrap));

            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void RoundedIcon_UsesAAClipMaterial_NoStencilMask()
        {
            var root = new GameObject("Root", typeof(RectTransform));
            var fallback = new Color(0.2f, 0.4f, 0.6f, 1f);
            var icon = UiFactory.RoundedIcon("Icon", root.transform, null, 48f, fallback);
            var img = icon.GetComponent<Image>();
            Assert.That(img, Is.Not.Null);
            Assert.That(img.material, Is.Not.Null);
            Assert.That(img.material.shader.name, Is.EqualTo("AmberScreen/UI/CircleClip"));
            Assert.That(img.material.GetFloat("_CornerRadius"), Is.EqualTo(0.14f).Within(0.001f));
            Assert.That(icon.GetComponent<CircleClipMeshModifier>(), Is.Not.Null);
            Assert.That(icon.GetComponentInChildren<Mask>(true), Is.Null);
            Assert.That(img.sprite, Is.SameAs(UiFactory.WhiteSprite));
            Assert.That(img.color, Is.EqualTo(fallback));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void CircleClipMaterial_CornerRadius_IsHalf()
        {
            Assert.That(
                UiFactory.CircleClipMaterial.GetFloat("_CornerRadius"),
                Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void ImageViewer_PreservesAttachmentAspect_Tall()
        {
            var canvasGo = new GameObject("C", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var hostGo = new GameObject("Host", typeof(RectTransform));
            var host = hostGo.GetComponent<RectTransform>();
            host.SetParent(canvasGo.transform, false);
            host.anchorMin = host.anchorMax = new Vector2(0.5f, 0.5f);
            host.sizeDelta = new Vector2(360f, 640f);

            var tex = new Texture2D(1080, 1920, TextureFormat.RGBA32, false);
            tex.Apply();
            var att = new ChatImageAttachment { FileName = "tall.png", Texture = tex, Aspect = 0.5625f };
            var viewer = ImageMessageViewer.Show(host, att);
            Canvas.ForceUpdateCanvases();

            var img = FindDeep(viewer.transform, "Img");
            var area = FindDeep(viewer.transform, "ImgArea");
            Assert.That(img, Is.Not.Null);
            Assert.That(area, Is.Not.Null);
            var fit = img.GetComponent<AspectRatioFitter>();
            Assert.That(fit, Is.Not.Null);
            Assert.That(fit.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));
            Assert.That(fit.aspectRatio, Is.EqualTo(0.5625f).Within(0.001f));

            var imgRt = img.GetComponent<RectTransform>();
            var areaRt = area.GetComponent<RectTransform>();
            Assert.That(imgRt.rect.width / imgRt.rect.height, Is.EqualTo(0.5625f).Within(0.01f));
            Assert.That(imgRt.rect.width, Is.LessThanOrEqualTo(areaRt.rect.width + 0.5f));
            Assert.That(imgRt.rect.height, Is.LessThanOrEqualTo(areaRt.rect.height + 0.5f));

            Object.DestroyImmediate(viewer);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void ImageViewer_PreservesAttachmentAspect_Wide()
        {
            var canvasGo = new GameObject("C", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var hostGo = new GameObject("Host", typeof(RectTransform));
            var host = hostGo.GetComponent<RectTransform>();
            host.SetParent(canvasGo.transform, false);
            host.anchorMin = host.anchorMax = new Vector2(0.5f, 0.5f);
            host.sizeDelta = new Vector2(360f, 640f);

            var tex = new Texture2D(64, 32, TextureFormat.RGBA32, false);
            tex.Apply();
            var att = new ChatImageAttachment { FileName = "wide.png", Texture = tex, Aspect = 2f };
            var viewer = ImageMessageViewer.Show(host, att);
            Canvas.ForceUpdateCanvases();

            var img = FindDeep(viewer.transform, "Img");
            var area = FindDeep(viewer.transform, "ImgArea");
            Assert.That(img, Is.Not.Null);
            Assert.That(area, Is.Not.Null);
            var fit = img.GetComponent<AspectRatioFitter>();
            Assert.That(fit.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));
            Assert.That(fit.aspectRatio, Is.EqualTo(2f).Within(0.001f));

            var imgRt = img.GetComponent<RectTransform>();
            var areaRt = area.GetComponent<RectTransform>();
            Assert.That(imgRt.rect.width / imgRt.rect.height, Is.EqualTo(2f).Within(0.01f));
            Assert.That(imgRt.rect.width, Is.LessThanOrEqualTo(areaRt.rect.width + 0.5f));

            Object.DestroyImmediate(viewer);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void Thread_TypingAndSeen_AreLastChildrenOfScrollContent()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var shellGo = new GameObject("PhoneShell", typeof(RectTransform), typeof(PhoneShell));
            shellGo.transform.SetParent(canvasGo.transform, false);
            var shell = shellGo.GetComponent<PhoneShell>();
            var contacts = new TestContacts();
            var chat = new TestChat();
            shell.Initialize(null, chat, contacts);
            shell.OpenMessagesThread("t1");

            var content = FindDeep(shellGo.transform, "ThreadScroll")?.Find("Content");
            Assert.That(content, Is.Not.Null);
            var typing = content.Find("Typing");
            var seen = content.Find("Seen");
            Assert.That(typing, Is.Not.Null);
            Assert.That(seen, Is.Not.Null);
            Assert.That(typing.parent, Is.SameAs(content));
            Assert.That(seen.parent, Is.SameAs(content));
            Assert.That(typing.gameObject.activeSelf, Is.False);
            Assert.That(seen.gameObject.activeSelf, Is.False);

            chat.RaiseSeen("t1", "friend");
            chat.RaiseTypingStarted("t1", "friend");
            Assert.That(typing.gameObject.activeSelf, Is.True);
            Assert.That(seen.gameObject.activeSelf, Is.True);
            Assert.That(content.GetChild(content.childCount - 1), Is.SameAs(typing));
            Assert.That(content.GetChild(content.childCount - 2), Is.SameAs(seen));

            var delivered = new ChatMessage
            {
                Id = "m2",
                ThreadId = "t1",
                SenderId = "friend",
                Text = "later",
                TimestampUtc = DateTime.UtcNow.AddSeconds(1),
                FromPlayer = false
            };
            chat.Deliver(delivered);
            Assert.That(content.GetChild(content.childCount - 1), Is.SameAs(typing));
            var bubble = content.Find("Bubble_m2");
            Assert.That(bubble, Is.Not.Null);
            Assert.That(bubble.GetSiblingIndex(), Is.LessThan(seen.GetSiblingIndex()));

            chat.RaiseTypingStopped("t1", "friend");
            Assert.That(typing.gameObject.activeSelf, Is.False);

            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void Thread_AutoScroll_OnlyWhenStuckToBottom()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var shellGo = new GameObject("PhoneShell", typeof(RectTransform), typeof(PhoneShell));
            shellGo.transform.SetParent(canvasGo.transform, false);
            var shellRt = shellGo.GetComponent<RectTransform>();
            shellRt.sizeDelta = new Vector2(360f, 640f);
            var shell = shellGo.GetComponent<PhoneShell>();
            var contacts = new TestContacts();
            var chat = new TestChat();
            chat.AddFiller("t1", 40);
            shell.Initialize(null, chat, contacts);
            shell.OpenMessagesThread("t1");
            Canvas.ForceUpdateCanvases();

            var scrollTf = FindDeep(shellGo.transform, "ThreadScroll");
            Assert.That(scrollTf, Is.Not.Null);
            var scroll = scrollTf.GetComponent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Assume.That(scroll.content.rect.height > scroll.viewport.rect.height,
                "Thread content must exceed viewport height for scroll tests");

            scroll.verticalNormalizedPosition = 0f;
            scroll.onValueChanged.Invoke(new Vector2(0f, 0f));
            chat.Deliver(new ChatMessage
            {
                Id = "stuck_msg",
                ThreadId = "t1",
                SenderId = "friend",
                Text = "stuck",
                TimestampUtc = DateTime.UtcNow,
                FromPlayer = false
            });
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(0f).Within(0.02f));

            scroll.verticalNormalizedPosition = 1f;
            scroll.onValueChanged.Invoke(new Vector2(0f, 1f));
            chat.Deliver(new ChatMessage
            {
                Id = "up_msg",
                ThreadId = "t1",
                SenderId = "friend",
                Text = "up",
                TimestampUtc = DateTime.UtcNow.AddSeconds(1),
                FromPlayer = false
            });
            Assert.That(scroll.verticalNormalizedPosition, Is.GreaterThan(0.9f));
            var pill = FindDeep(shellGo.transform, "NewMessages");
            Assert.That(pill, Is.Not.Null);
            Assert.That(pill.gameObject.activeSelf, Is.True);

            chat.RaiseTypingStarted("t1", "friend");
            Assert.That(scroll.verticalNormalizedPosition, Is.GreaterThan(0.9f));

            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void FontCache_Invalidates_OnThemeOverride()
        {
            var fontA = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            Assert.That(fontA, Is.Not.Null, "TMP Essential Resources (LiberationSans SDF) required");

            var settings = ScriptableObject.CreateInstance<PhoneFontSettings>();
            settings.Primary = fontA;
            var theme = ScriptableObject.CreateInstance<PhoneUITheme>();
            theme.FontSettings = settings;

            PhoneUITheme.SetOverride(theme);
            Assert.That(UiFactory.DefaultFont, Is.SameAs(fontA));

            PhoneUITheme.SetOverride(null);
            var fontField = typeof(UiFactory).GetField("_font", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(fontField, Is.Not.Null);
            Assert.That(fontField.GetValue(null), Is.Null, "SetOverride(null) must clear the font cache");

            Object.DestroyImmediate(theme);
            Object.DestroyImmediate(settings);
        }

        static void AssertFill(RectTransform rt)
        {
            Assert.That(rt.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rt.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rt.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rt.offsetMax, Is.EqualTo(Vector2.zero));
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var hit = FindDeep(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        sealed class TestContacts : IContactDirectory
        {
            public TestContacts()
            {
                Player = new ContactInfo { Id = "player", DisplayName = "Player", IsPlayer = true };
                Contacts = new List<ContactInfo>
                {
                    Player,
                    new ContactInfo { Id = "friend", DisplayName = "Friend" }
                };
            }

            public IReadOnlyList<ContactInfo> Contacts { get; }
            public ContactInfo Player { get; }
            public ContactInfo GetContact(string id)
            {
                foreach (var c in Contacts)
                    if (c.Id == id) return c;
                return null;
            }
        }

        sealed class TestChat : IChatSource
        {
            readonly List<ChatThreadInfo> _threads = new();
            readonly Dictionary<string, List<ChatMessage>> _history = new();

            public TestChat()
            {
                _threads.Add(new ChatThreadInfo
                {
                    Id = "t1",
                    Title = "Friend",
                    LastMessagePreview = "hi",
                    LastMessageUtc = DateTime.UtcNow,
                    UnreadCount = 0,
                    ParticipantIds = new[] { "player", "friend" }
                });
                _history["t1"] = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Id = "m1",
                        ThreadId = "t1",
                        SenderId = "friend",
                        Text = "hi",
                        TimestampUtc = DateTime.UtcNow,
                        FromPlayer = false
                    }
                };
            }

            public IReadOnlyList<ChatThreadInfo> Threads => _threads;
            public IReadOnlyList<ChatMessage> GetHistory(string threadId) =>
                _history.TryGetValue(threadId, out var list) ? list : Array.Empty<ChatMessage>();
            public void SendPlayerMessage(string threadId, string text) { }
            public void MarkThreadRead(string threadId) { }
            public void OpenMessageLink(ChatMessage message) { }

            public void RaiseSeen(string threadId, string id) => Seen?.Invoke(threadId, id);
            public void RaiseTypingStarted(string threadId, string id) => TypingStarted?.Invoke(threadId, id);
            public void RaiseTypingStopped(string threadId, string id) => TypingStopped?.Invoke(threadId, id);

            public void Deliver(ChatMessage m)
            {
                if (!_history.TryGetValue(m.ThreadId, out var list))
                {
                    list = new List<ChatMessage>();
                    _history[m.ThreadId] = list;
                }
                list.Add(m);
                MessageDelivered?.Invoke(m);
            }

            public void AddFiller(string threadId, int count)
            {
                if (!_history.TryGetValue(threadId, out var list))
                {
                    list = new List<ChatMessage>();
                    _history[threadId] = list;
                }
                var now = DateTime.UtcNow;
                for (int i = 0; i < count; i++)
                {
                    list.Add(new ChatMessage
                    {
                        Id = "filler_" + i,
                        ThreadId = threadId,
                        SenderId = "friend",
                        Text = "filler " + i,
                        TimestampUtc = now.AddMinutes(-(count - i)),
                        FromPlayer = false
                    });
                }
            }

#pragma warning disable CS0067
            public event Action ThreadsChanged;
#pragma warning restore CS0067
            public event Action<string, string> Seen;
            public event Action<string, string> TypingStarted;
            public event Action<string, string> TypingStopped;
            public event Action<ChatMessage> MessageDelivered;
        }
    }
}
