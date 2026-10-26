using System;
using System.Collections.Generic;

namespace AmberScreen.PhoneUI
{
    public interface IChatSource
    {
        IReadOnlyList<ChatThreadInfo> Threads { get; }
        IReadOnlyList<ChatMessage> GetHistory(string threadId);
        void SendPlayerMessage(string threadId, string text);
        void MarkThreadRead(string threadId);

        event Action ThreadsChanged;
        event Action<string /*threadId*/, string /*contactId*/> Seen;
        event Action<string, string> TypingStarted;
        event Action<string, string> TypingStopped;
        event Action<ChatMessage> MessageDelivered;

        /// <summary>Optional deep-link from a chat bubble (e.g. switch stub). No-op if unsupported.</summary>
        void OpenMessageLink(ChatMessage message);
    }
}
