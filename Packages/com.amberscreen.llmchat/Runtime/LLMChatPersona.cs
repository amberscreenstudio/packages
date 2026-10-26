using UnityEngine;

namespace AmberScreen.LLMChat
{
    [CreateAssetMenu(menuName = "Amber Screen/LLM Chat/Persona", fileName = "LLMChatPersona")]
    public sealed class LLMChatPersona : ScriptableObject
    {
        [Tooltip("Stable id used by the host (e.g. friend_a).")]
        public string CharacterId = "friend";

        public string DisplayName = "Friend";

        [TextArea(4, 12)]
        public string SystemPrompt =
            "You are a friend in a group chat. Reply in short casual texts. Never narrate. Never admit to being AI.";

        [TextArea(2, 6)]
        public string StyleNotes = "lowercase ok; 1-2 short sentences; no hashtags.";

        [Tooltip("Optional example reply lines shown in the prompt.")]
        public string[] ExampleLines =
        {
            "yeah that tracks",
            "wait really?",
            "lol fair"
        };

        /// <summary>
        /// Parallel to <see cref="ExampleLines"/>: empty, <c>tell</c>, <c>low</c>, <c>mid</c>, <c>high</c>.
        /// Used by <see cref="LLMChatExamplePicker"/> with beat tension.
        /// </summary>
        public string[] ExampleLineTags;

        [Range(24, 280)]
        public int MaxReplyLength = 120;
    }
}
