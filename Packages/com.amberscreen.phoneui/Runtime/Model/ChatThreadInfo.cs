using System;

namespace AmberScreen.PhoneUI
{
    [Serializable]
    public sealed class ChatThreadInfo
    {
        public string Id;
        public string Title;
        public string LastMessagePreview;
        public DateTime LastMessageUtc;
        public int UnreadCount;
        public string[] ParticipantIds;
    }
}
