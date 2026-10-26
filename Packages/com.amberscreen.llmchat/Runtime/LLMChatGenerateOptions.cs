namespace AmberScreen.LLMChat
{
    public sealed class LLMChatGenerateOptions
    {
        public string SystemPrompt;
        public int MaxTokens = 96;
        public float Temperature = 0.8f;
        public float TopP = 0.9f;
        public float RepeatPenalty = 1.15f;
        /// <summary>Recent-token window for repeat penalty (LLMUnity <c>repeatLastN</c>).</summary>
        public int RepeatLastN = 64;
    }
}
