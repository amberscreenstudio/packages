namespace AmberScreen.LLMChat
{
    /// <summary>One turn in shared group history (speaker display name + text).</summary>
    public sealed class LLMChatHistoryTurn
    {
        public string SpeakerId;
        public string SpeakerDisplayName;
        public string Text;
        public bool FromPlayer;

        public LLMChatHistoryTurn() { }

        public LLMChatHistoryTurn(string speakerId, string displayName, string text, bool fromPlayer)
        {
            SpeakerId = speakerId;
            SpeakerDisplayName = displayName;
            Text = text;
            FromPlayer = fromPlayer;
        }
    }
}
