using System;
using System.Threading;
using System.Threading.Tasks;

namespace AmberScreen.LLMChat
{
    /// <summary>
    /// Thin generation backend. Only <c>LLMUnityChatBackend</c> should talk to LLMUnity.
    /// </summary>
    public interface ILLMChatBackend
    {
        /// <summary>True after <see cref="PrepareAsync"/> completed successfully.</summary>
        bool IsReady { get; }

        /// <summary>Last generation/prepare failure message (for host UI / console).</summary>
        string LastError { get; }

        /// <summary>Wait for the underlying model/agent caller to be usable.</summary>
        Task PrepareAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate a completion for <paramref name="prompt"/>.
        /// <paramref name="onChunk"/> receives the cumulative streamed text when supported.
        /// </summary>
        Task<string> GenerateAsync(
            string prompt,
            LLMChatGenerateOptions options,
            Action<string> onChunk,
            CancellationToken cancellationToken);
    }
}
