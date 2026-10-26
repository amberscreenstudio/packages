using UnityEngine;

namespace AmberScreen.LLMChat
{
    [CreateAssetMenu(menuName = "Amber Screen/LLM Chat/Timing Settings", fileName = "LLMChatTimingSettings")]
    public sealed class LLMChatTimingSettings : ScriptableObject
    {
        [Header("Seen")]
        public Vector2 SeenDelaySeconds = new Vector2(0.8f, 2.5f);

        [Header("Typing")]
        [Tooltip("Base typing time before length scaling.")]
        public Vector2 TypingBaseSeconds = new Vector2(1.5f, 4f);

        public float TypingSecondsPerChar = 0.03f;

        [Tooltip("Clamp for total typing duration (min, max).")]
        public Vector2 TypingClampSeconds = new Vector2(1.2f, 10f);

        [Tooltip("Extra random jitter added to typing (± seconds).")]
        public float TypingJitterSeconds = 0.4f;

        [Header("Multi-reply")]
        public Vector2 SecondReplyStartDelaySeconds = new Vector2(0.3f, 1.8f);

        public float RollSeenDelay() =>
            Random.Range(SeenDelaySeconds.x, SeenDelaySeconds.y);

        public float RollTypingDuration(string replyText)
        {
            int len = string.IsNullOrEmpty(replyText) ? 12 : replyText.Length;
            float t = Random.Range(TypingBaseSeconds.x, TypingBaseSeconds.y)
                      + len * TypingSecondsPerChar
                      + Random.Range(-TypingJitterSeconds, TypingJitterSeconds);
            return Mathf.Clamp(t, TypingClampSeconds.x, TypingClampSeconds.y);
        }

        public float RollSecondStartDelay() =>
            Random.Range(SecondReplyStartDelaySeconds.x, SecondReplyStartDelaySeconds.y);
    }
}
