using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmberScreen.LLMChat;
using AmberScreen.PhoneUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AmberScreen.Sandbox
{
    /// <summary>
    /// PhoneUI + LLMChat smoke: round avatars (full / packed UV / fallback) and a canned LLM reply.
    /// </summary>
    public sealed class PhoneUISandbox : MonoBehaviour
    {
        FakeChatSource _chat;
        LLMChatRunner _runner;
        LLMChatPersona _persona;
        LLMChatTimingSettings _timings;
        CancellationTokenSource _cts;
        Texture2D _testIconTex;
        Sprite _testIconSprite;
        Texture2D _testImageTex;

        void Awake()
        {
            EnsureEventSystem();

            var contacts = new FakeContacts();
            var testImage = MakeTestImage();
            _chat = new FakeChatSource(contacts, testImage);

            var shellGo = new GameObject("PhoneShell", typeof(RectTransform), typeof(PhoneShell));
            var shell = shellGo.GetComponent<PhoneShell>();
            shell.Initialize(
                null,
                _chat,
                contacts,
                new IPhoneApp[]
                {
                    new ComingSoonApp("icon_test", "Icon", MakeTestIcon()),
                    new ComingSoonApp("no_icon", "NoIcon")
                });

            _persona = ScriptableObject.CreateInstance<LLMChatPersona>();
            _persona.hideFlags = HideFlags.HideAndDontSave;
            _persona.CharacterId = "full";
            _persona.DisplayName = "Full";
            _persona.SystemPrompt = "You are Full. Reply briefly.";
            _persona.MaxReplyLength = 80;

            _timings = ScriptableObject.CreateInstance<LLMChatTimingSettings>();
            _timings.hideFlags = HideFlags.HideAndDontSave;
            _timings.SeenDelaySeconds = new Vector2(0.8f, 1.0f);
            _timings.TypingBaseSeconds = new Vector2(2.0f, 2.5f);
            _timings.TypingSecondsPerChar = 0f;
            _timings.TypingClampSeconds = new Vector2(2.0f, 2.5f);
            _timings.TypingJitterSeconds = 0f;

            var runnerGo = new GameObject("LLMChatRunner");
            _runner = runnerGo.AddComponent<LLMChatRunner>();
            _runner.Configure(new CannedBackend(), _persona, _timings);

            _chat.BindRunner(_runner, this);
        }

        void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            if (_persona != null) Destroy(_persona);
            if (_timings != null) Destroy(_timings);
            if (_testIconSprite != null) Destroy(_testIconSprite);
            if (_testIconTex != null) Destroy(_testIconTex);
            if (_testImageTex != null) Destroy(_testImageTex);
        }

        Texture2D MakeTestImage()
        {
            const int w = 540;
            const int h = 960;
            _testImageTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
            var mid = new Color32(160, 160, 160, 255);
            var dark = new Color32(40, 40, 44, 255);
            var line = new Color32(90, 90, 96, 255);
            var fill = new Color32(70, 130, 180, 255);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c = mid;
                if (x < 6 || y < 6 || x >= w - 6 || y >= h - 6)
                    c = dark;
                else if (x % 60 == 0 || y % 60 == 0)
                    c = line;
                int dx = x - w / 2;
                int dy = y - h / 2;
                if (dx * dx + dy * dy <= 200 * 200)
                    c = fill;
                _testImageTex.SetPixel(x, y, c);
            }
            _testImageTex.Apply(true);
            _testImageTex.filterMode = FilterMode.Trilinear;
            _testImageTex.wrapMode = TextureWrapMode.Clamp;
            _testImageTex.hideFlags = HideFlags.HideAndDontSave;
            return _testImageTex;
        }

        Sprite MakeTestIcon()
        {
            const int size = 256;
            _testIconTex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var a = new Color32(60, 160, 220, 255);
            var b = new Color32(220, 120, 60, 255);
            var border = new Color32(20, 20, 24, 255);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Color32 c = x + y < size ? a : b;
                if (x < 4 || y < 4 || x >= size - 4 || y >= size - 4)
                    c = border;
                _testIconTex.SetPixel(x, y, c);
            }
            _testIconTex.Apply(true);
            _testIconTex.filterMode = FilterMode.Trilinear;
            _testIconTex.hideFlags = HideFlags.HideAndDontSave;
            _testIconSprite = Sprite.Create(
                _testIconTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _testIconSprite.hideFlags = HideFlags.HideAndDontSave;
            return _testIconSprite;
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        sealed class CannedBackend : ILLMChatBackend
        {
            public bool IsReady => true;
            public string LastError => null;

            public Task PrepareAsync(CancellationToken cancellationToken = default) =>
                Task.CompletedTask;

            public async Task<string> GenerateAsync(
                string prompt,
                LLMChatGenerateOptions options,
                Action<string> onChunk,
                CancellationToken cancellationToken)
            {
                await Task.Delay(500, cancellationToken);
                const string reply = "hey — sandbox canned reply";
                onChunk?.Invoke(reply);
                return reply;
            }
        }

        sealed class FakeContacts : IContactDirectory
        {
            readonly List<ContactInfo> _contacts = new();
            readonly Dictionary<string, ContactInfo> _byId = new();

            public FakeContacts()
            {
                Player = new ContactInfo
                {
                    Id = "player",
                    DisplayName = "Player",
                    IsPlayer = true,
                    Color = new Color(0.3f, 0.55f, 0.95f, 1f)
                };
                Add(Player);
                Add(MakeFull());
                Add(MakePacked());
                Add(new ContactInfo
                {
                    Id = "noavatar",
                    DisplayName = "NoAvatar",
                    Color = Color.clear
                });
            }

            public IReadOnlyList<ContactInfo> Contacts => _contacts;
            public ContactInfo Player { get; }

            public ContactInfo GetContact(string id) =>
                _byId.TryGetValue(id, out var c) ? c : null;

            void Add(ContactInfo c)
            {
                _contacts.Add(c);
                _byId[c.Id] = c;
            }

            static ContactInfo MakeFull()
            {
                var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                var pixels = new Color32[64 * 64];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(80, 180, 120, 255);
                tex.SetPixels32(pixels);
                tex.Apply(false, true);
                tex.hideFlags = HideFlags.HideAndDontSave;
                var sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return new ContactInfo
                {
                    Id = "full",
                    DisplayName = "Full",
                    Avatar = sprite,
                    Color = new Color(0.3f, 0.7f, 0.4f, 1f)
                };
            }

            static ContactInfo MakePacked()
            {
                var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    bool right = x >= 64;
                    bool top = y >= 64;
                    Color32 c;
                    if (!right && !top) c = new Color32(220, 60, 60, 255);
                    else if (right && !top) c = new Color32(60, 60, 220, 255);
                    else if (!right && top) c = new Color32(220, 220, 60, 255);
                    else c = new Color32(60, 180, 220, 255);
                    tex.SetPixel(x, y, c);
                }
                tex.Apply(false, true);
                tex.hideFlags = HideFlags.HideAndDontSave;
                // Bottom-right quadrant only → non-0..1 UVs when atlased.
                var sprite = Sprite.Create(tex, new Rect(64, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return new ContactInfo
                {
                    Id = "packed",
                    DisplayName = "Packed",
                    Avatar = sprite,
                    Color = new Color(0.2f, 0.6f, 0.8f, 1f)
                };
            }
        }

        sealed class FakeChatSource : IChatSource
        {
            readonly FakeContacts _contacts;
            readonly List<ChatThreadInfo> _threads = new();
            readonly Dictionary<string, List<ChatMessage>> _history = new();
            LLMChatRunner _runner;
            PhoneUISandbox _host;

            public FakeChatSource(FakeContacts contacts, Texture2D testImage)
            {
                _contacts = contacts;
                SeedFull(testImage);
                Seed("packed", "Packed", "packed avatar here");
                Seed("noavatar", "NoAvatar", "no avatar thread");
            }

            public void BindRunner(LLMChatRunner runner, PhoneUISandbox host)
            {
                _runner = runner;
                _host = host;
            }

            public IReadOnlyList<ChatThreadInfo> Threads => _threads;

            public IReadOnlyList<ChatMessage> GetHistory(string threadId) =>
                _history.TryGetValue(threadId, out var list) ? list : Array.Empty<ChatMessage>();

            public event Action ThreadsChanged;
            public event Action<string, string> Seen;
            public event Action<string, string> TypingStarted;
            public event Action<string, string> TypingStopped;
            public event Action<ChatMessage> MessageDelivered;

            public void MarkThreadRead(string threadId)
            {
                var t = FindThread(threadId);
                if (t == null) return;
                t.UnreadCount = 0;
                ThreadsChanged?.Invoke();
            }

            public void OpenMessageLink(ChatMessage message) { }

            public void SendPlayerMessage(string threadId, string text)
            {
                var msg = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ThreadId = threadId,
                    SenderId = "player",
                    Text = text,
                    TimestampUtc = DateTime.UtcNow,
                    FromPlayer = true,
                    Receipt = MessageReceipt.Sent
                };
                History(threadId).Add(msg);
                TouchThread(threadId, text);
                ThreadsChanged?.Invoke();

                if (_runner == null || threadId != "full")
                    return;

                _host._cts?.Cancel();
                _host._cts = new CancellationTokenSource();
                var ct = _host._cts.Token;
                _ = RunNpcReplyAsync(threadId, ct);
            }

            async Task RunNpcReplyAsync(string threadId, CancellationToken ct)
            {
                void OnSeen(string id) => Seen?.Invoke(threadId, id);
                void OnTyping(string id) => TypingStarted?.Invoke(threadId, id);
                void OnDone(string id, string text)
                {
                    TypingStopped?.Invoke(threadId, id);
                    var reply = new ChatMessage
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ThreadId = threadId,
                        SenderId = id,
                        Text = text,
                        TimestampUtc = DateTime.UtcNow,
                        FromPlayer = false
                    };
                    History(threadId).Add(reply);
                    TouchThread(threadId, text);
                    MessageDelivered?.Invoke(reply);
                    ThreadsChanged?.Invoke();
                }

                _runner.Seen += OnSeen;
                _runner.TypingStarted += OnTyping;
                _runner.Done += OnDone;
                try
                {
                    var turns = new List<LLMChatHistoryTurn>();
                    foreach (var m in History(threadId))
                    {
                        var c = _contacts.GetContact(m.SenderId);
                        turns.Add(new LLMChatHistoryTurn(
                            m.SenderId,
                            c != null ? c.DisplayName : m.SenderId,
                            m.Text,
                            m.FromPlayer));
                    }
                    await _runner.RunReplyAsync(turns, "Reply casually.", ct);
                }
                finally
                {
                    _runner.Seen -= OnSeen;
                    _runner.TypingStarted -= OnTyping;
                    _runner.Done -= OnDone;
                }
            }

            void SeedFull(Texture2D testImage)
            {
                const string contactId = "full";
                const string preview = "hey from Full";
                _threads.Add(new ChatThreadInfo
                {
                    Id = contactId,
                    Title = "Full",
                    LastMessagePreview = preview,
                    LastMessageUtc = DateTime.UtcNow.AddMinutes(-5),
                    UnreadCount = 0,
                    ParticipantIds = new[] { "player", contactId }
                });
                var hist = History(contactId);
                for (int i = 0; i < 12; i++)
                {
                    bool fromPlayer = i % 2 == 1;
                    float minutesAgo = 30f - i * (23f / 11f);
                    hist.Add(new ChatMessage
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ThreadId = contactId,
                        SenderId = fromPlayer ? "player" : contactId,
                        Text = fromPlayer ? "player filler " + i : "friend filler " + i,
                        TimestampUtc = DateTime.UtcNow.AddMinutes(-minutesAgo),
                        FromPlayer = fromPlayer,
                        Receipt = fromPlayer ? MessageReceipt.Read : MessageReceipt.None
                    });
                }
                hist.Add(new ChatMessage
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ThreadId = contactId,
                    SenderId = contactId,
                    Text = "",
                    TimestampUtc = DateTime.UtcNow.AddMinutes(-6),
                    FromPlayer = false,
                    Image = new ChatImageAttachment
                    {
                        FileName = "sandbox_9x16.png",
                        Texture = testImage
                    }
                });
                hist.Add(new ChatMessage
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ThreadId = contactId,
                    SenderId = contactId,
                    Text = preview,
                    TimestampUtc = DateTime.UtcNow.AddMinutes(-5),
                    FromPlayer = false
                });
            }

            void Seed(string contactId, string title, string preview)
            {
                var threadId = contactId;
                _threads.Add(new ChatThreadInfo
                {
                    Id = threadId,
                    Title = title,
                    LastMessagePreview = preview,
                    LastMessageUtc = DateTime.UtcNow.AddMinutes(-5),
                    UnreadCount = 0,
                    ParticipantIds = new[] { "player", contactId }
                });
                History(threadId).Add(new ChatMessage
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ThreadId = threadId,
                    SenderId = contactId,
                    Text = preview,
                    TimestampUtc = DateTime.UtcNow.AddMinutes(-5),
                    FromPlayer = false
                });
            }

            List<ChatMessage> History(string threadId)
            {
                if (!_history.TryGetValue(threadId, out var list))
                {
                    list = new List<ChatMessage>();
                    _history[threadId] = list;
                }
                return list;
            }

            ChatThreadInfo FindThread(string id)
            {
                for (int i = 0; i < _threads.Count; i++)
                    if (_threads[i].Id == id) return _threads[i];
                return null;
            }

            void TouchThread(string threadId, string preview)
            {
                var t = FindThread(threadId);
                if (t == null) return;
                t.LastMessagePreview = preview;
                t.LastMessageUtc = DateTime.UtcNow;
            }
        }
    }
}
