using System;
using System.Threading;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AmberScreen.LLMChat
{
    /// <summary>
    /// Sole LLMUnity adapter for llmchat. Multiple runners share one instance; generates are gated.
    /// </summary>
    public sealed class LLMUnityChatBackend : MonoBehaviour, ILLMChatBackend
    {
        [SerializeField] LLMAgent agent;

        readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        CancellationTokenSource _activeCts;

        public LLMAgent Agent
        {
            get => agent;
            set => agent = value;
        }

        public bool IsReady { get; private set; }
        public string LastError { get; private set; }

        public void Bind(LLMAgent llmAgent)
        {
            agent = llmAgent;
            IsReady = false;
            LastError = null;
        }

        /// <summary>
        /// Wait for LLM load + agent caller setup (Warmup). Call after the agent GameObject is
        /// enabled with <c>llm</c> already assigned.
        /// </summary>
        public async Task PrepareAsync(CancellationToken cancellationToken = default)
        {
            if (agent == null)
            {
                LastError = "LLMUnityChatBackend: LLMAgent not set";
                IsReady = false;
                throw new InvalidOperationException(LastError);
            }

            if (agent.llm == null)
            {
                LastError = "LLMUnityChatBackend: LLMAgent.llm is null (bind llm before enabling the agent)";
                IsReady = false;
                throw new InvalidOperationException(LastError);
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await agent.llm.WaitUntilReady();
                cancellationToken.ThrowIfCancellationRequested();

                // LLMAgent.Start (async) may not have finished SetupCaller yet right after enable.
                // Retry Warmup until the caller exists (or timeout).
                Exception last = null;
                for (int attempt = 0; attempt < 120; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        await agent.Warmup();
                        IsReady = true;
                        LastError = null;
                        Debug.Log("[LLMUnityChatBackend] Prepare OK (agent caller ready, attempt=" + (attempt + 1) + ")");
                        return;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        last = ex;
                        string m = ex.Message ?? "";
                        bool notReady = m.IndexOf("caller not initialized", StringComparison.OrdinalIgnoreCase) >= 0
                                        || m.IndexOf("llmClient not initialized", StringComparison.OrdinalIgnoreCase) >= 0
                                        || m.IndexOf("LLMAgent not initialized", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!notReady)
                            throw;
                        await Task.Delay(250, cancellationToken);
                    }
                }

                IsReady = false;
                LastError = "chat_prepare_timeout: " + (last != null ? last.Message : "caller never ready");
                Debug.LogError("[LLMUnityChatBackend] " + LastError);
                throw new TimeoutException(LastError);
            }
            catch (OperationCanceledException)
            {
                IsReady = false;
                throw;
            }
            catch (Exception ex)
            {
                IsReady = false;
                if (string.IsNullOrEmpty(LastError))
                    LastError = "chat_prepare: " + ex.Message;
                Debug.LogError("[LLMUnityChatBackend] Prepare failed: " + ex.Message);
                throw;
            }
        }

        public async Task<string> GenerateAsync(
            string prompt,
            LLMChatGenerateOptions options,
            Action<string> onChunk,
            CancellationToken cancellationToken)
        {
            if (agent == null)
            {
                LastError = "LLMUnityChatBackend: LLMAgent not set";
                Debug.LogError("[LLMUnityChatBackend] " + LastError);
                throw new InvalidOperationException(LastError);
            }

            if (!IsReady)
            {
                LastError = "LLMUnityChatBackend: not ready (PrepareAsync not completed)";
                Debug.LogError("[LLMUnityChatBackend] " + LastError);
                throw new InvalidOperationException(LastError);
            }

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(true);
            _activeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = _activeCts.Token;
            try
            {
                ct.ThrowIfCancellationRequested();

                if (agent.llm != null)
                    await agent.llm.WaitUntilReady();

                if (options != null)
                {
                    if (!string.IsNullOrEmpty(options.SystemPrompt))
                        agent.systemPrompt = options.SystemPrompt;
                    agent.numPredict = options.MaxTokens > 0 ? options.MaxTokens : 96;
                    agent.temperature = options.Temperature > 0f ? options.Temperature : 0.8f;
                    agent.topP = options.TopP > 0f ? options.TopP : 0.9f;
                    agent.repeatPenalty = options.RepeatPenalty > 0f ? options.RepeatPenalty : 1.15f;
                    agent.repeatLastN = options.RepeatLastN > 0 ? options.RepeatLastN : 64;
                }

                // Do not pollute LLMUnity's internal history — llmchat owns the window.
                string result = await agent.Chat(
                    prompt ?? "",
                    onChunk,
                    null,
                    addToHistory: false);

                ct.ThrowIfCancellationRequested();
                LastError = null;
                return result ?? "";
            }
            catch (OperationCanceledException)
            {
                try { agent.CancelRequests(); } catch { /* ignore */ }
                throw;
            }
            catch (Exception ex)
            {
                LastError = "generate: " + ex.Message;
                Debug.LogError("[LLMUnityChatBackend] Generate failed: " + ex.Message);
                throw;
            }
            finally
            {
                _activeCts?.Dispose();
                _activeCts = null;
                _gate.Release();
            }
        }

        public void CancelActive()
        {
            try { _activeCts?.Cancel(); } catch { /* ignore */ }
            try { agent?.CancelRequests(); } catch { /* ignore */ }
        }

        void OnDestroy()
        {
            CancelActive();
            _gate.Dispose();
        }
    }
}
