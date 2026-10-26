using UnityEngine;

namespace AmberScreen.LLMChat
{
    /// <summary>
    /// Backend model preference list + reply time budget for the host to load via its LLM store.
    /// Preferred model is index 0; last entry is last-resort.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Amber Screen/LLM Chat/Backend Settings",
        fileName = "LLMChatBackendSettings")]
    public sealed class LLMChatBackendSettings : ScriptableObject
    {
        [Tooltip("Ordered model filenames (preferred first). Resolved by the host (LLMUnity store / StreamingAssets).")]
        public string[] ModelLadder = System.Array.Empty<string>();

        [Tooltip("Max acceptable reply / warmup generation time before stepping down.")]
        [Min(1f)]
        public float ReplyBudgetSeconds = 10f;

        public ModelLadder CreateLadder()
        {
            var models = ModelLadder != null && ModelLadder.Length > 0
                ? ModelLadder
                : new[] { "model.gguf" };
            return new ModelLadder(models, ReplyBudgetSeconds);
        }
    }
}
