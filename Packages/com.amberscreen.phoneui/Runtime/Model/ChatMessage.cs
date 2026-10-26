using System;

namespace AmberScreen.PhoneUI
{
    [Serializable]
    public sealed class ChatMessage
    {
        public string Id;
        public string ThreadId;
        public string SenderId;
        public string Text;
        public DateTime TimestampUtc;
        public bool FromPlayer;

        /// <summary>Receipt ticks for player bubbles (ignored for NPC). See <see cref="MessageReceipt"/>.</summary>
        public MessageReceipt Receipt = MessageReceipt.None;

        /// <summary>Optional link action id (e.g. switch_stub). Empty = no link.</summary>
        public string LinkId;

        /// <summary>Optional image attachment (PhoneUI 0.2.0). Null = text-only.</summary>
        public ChatImageAttachment Image;
    }
}
