using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AmberScreen.LLMChat
{
    /// <summary>
    /// Per-character chat runner: builds prompt from persona + history + director instruction,
    /// emits Seen / TypingStarted / Chunk / Done / Error with timing. Several runners may type at once;
    /// the backend gates actual LLM calls.
    /// </summary>
    public sealed class LLMChatRunner : MonoBehaviour
    {
        public const int MaxPromptTokensApprox = 1500;

        [SerializeField] LLMChatPersona persona;
        [SerializeField] LLMChatTimingSettings timings;
        [SerializeField] [Range(4, 40)] int historyWindowTurns = 8;
        [SerializeField] int maxTokens = 80;
        [SerializeField] float generateTimeoutSeconds = 30f;

        ILLMChatBackend _backend;
        CancellationTokenSource _cts;
        bool _busy;
        string _situationBlock;
        string _playerDisplayName = "";
        string _beatTension = "low";
        List<string> _lastPickedExamples;
        static readonly HashSet<string> s_LoggedPromptFriends = new HashSet<string>();
        static bool s_LoggedSampling;

        /// <summary>Dev-console toggle: log the full final prompt once per friend.</summary>
        public static bool LogPrompts { get; set; }

        /// <summary>Shared sampling defaults (logged once at first generate).</summary>
        public const float DefaultTemperature = 0.8f;
        public const float DefaultTopP = 0.9f;
        public const float DefaultRepeatPenalty = 1.15f;
        public const int DefaultRepeatLastN = 64;

        /// <summary>In-game player name used in history lines (never "You"). Set by the host.</summary>
        public string PlayerDisplayName
        {
            get => _playerDisplayName;
            set => _playerDisplayName = value != null ? value.Trim() : "";
        }

        public LLMChatPersona Persona
        {
            get => persona;
            set => persona = value;
        }

        public LLMChatTimingSettings Timings
        {
            get => timings;
            set => timings = value;
        }

        public int HistoryWindowTurns
        {
            get => historyWindowTurns;
            set => historyWindowTurns = Mathf.Clamp(value, 4, 40);
        }

        public string CharacterId => persona != null ? persona.CharacterId : name;

        /// <summary>Last successful generation wall time in ms (for debug readouts).</summary>
        public long LastGenerateMs { get; private set; }

        /// <summary>Approx token count of the last prompt (chars/4).</summary>
        public int LastPromptTokensApprox { get; private set; }

        public bool IsBusy => _busy;

        public int MaxTokens
        {
            get => maxTokens;
            set => maxTokens = Mathf.Clamp(value, 16, 256);
        }

        public float GenerateTimeoutSeconds
        {
            get => generateTimeoutSeconds;
            set => generateTimeoutSeconds = Mathf.Clamp(value, 2f, 120f);
        }

        public void SetSituationBlock(string block) => _situationBlock = block;

        public void ClearSituationBlock() => _situationBlock = null;

        /// <summary>Beat tension: <c>low</c> / <c>mid</c> / <c>high</c> — weights example picks.</summary>
        public string BeatTension
        {
            get => _beatTension;
            set => _beatTension = string.IsNullOrWhiteSpace(value) ? "low" : value.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Optional host-supplied hard-rule sentence(s). When set, <b>replaces</b> the generic
        /// known-facts / secrets wording (e.g. story-specific fact / secret rules).
        /// Empty → neutral generic wording.
        /// </summary>
        public string KnowledgeRulesHint { get; set; }

        /// <summary>Last example lines chosen for the prompt (EditMode / diagnostics).</summary>
        public IReadOnlyList<string> LastPickedExamples =>
            _lastPickedExamples ?? (IReadOnlyList<string>)Array.Empty<string>();

        public event Action<string /*characterId*/> Seen;
        public event Action<string> TypingStarted;
        public event Action<string /*characterId*/, string /*cumulative*/> Chunk;
        public event Action<string /*characterId*/, string /*text*/> Done;
        public event Action<string /*characterId*/, string /*error*/> Error;

        public void Configure(ILLMChatBackend backend, LLMChatPersona personaAsset, LLMChatTimingSettings timingAsset)
        {
            _backend = backend;
            if (personaAsset != null) persona = personaAsset;
            if (timingAsset != null) timings = timingAsset;
        }

        public async Task RunReplyAsync(
            IReadOnlyList<LLMChatHistoryTurn> sharedHistory,
            string directorInstruction,
            CancellationToken externalCt = default)
        {
            if (_backend == null)
            {
                Error?.Invoke(CharacterId, "no_backend");
                return;
            }

            if (persona == null)
            {
                Error?.Invoke(CharacterId, "no_persona");
                return;
            }

            CancelActive();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            if (generateTimeoutSeconds > 0f)
            {
                try { _cts.CancelAfter(TimeSpan.FromSeconds(generateTimeoutSeconds)); }
                catch { /* ignore */ }
            }
            var ct = _cts.Token;
            _busy = true;

            try
            {
                float seenDelay = timings != null ? timings.RollSeenDelay() : 1f;
                await DelaySeconds(seenDelay, ct);
                if (!isActiveAndEnabled) return;
                Seen?.Invoke(CharacterId);

                TypingStarted?.Invoke(CharacterId);

                string system = BuildSystemPrompt();
                string userPrompt = BuildUserPrompt(sharedHistory, directorInstruction, out int histLines);
                userPrompt = EnsureNoThinkSuffix(userPrompt);
                userPrompt = FitPromptToBudget(system, userPrompt, sharedHistory, directorInstruction, ref histLines);

                if (LogPrompts && s_LoggedPromptFriends.Add(CharacterId))
                {
                    Debug.Log("[LLMChatRunner] PROMPT friend=" + CharacterId
                              + "\n---SYSTEM---\n" + system
                              + "\n---USER---\n" + userPrompt);
                }

                var options = new LLMChatGenerateOptions
                {
                    SystemPrompt = system,
                    MaxTokens = maxTokens > 0 ? maxTokens : 80,
                    Temperature = DefaultTemperature,
                    TopP = DefaultTopP,
                    RepeatPenalty = DefaultRepeatPenalty,
                    RepeatLastN = DefaultRepeatLastN
                };

                if (!s_LoggedSampling)
                {
                    s_LoggedSampling = true;
                    Debug.Log("[LLMChat] sampling temp=" + options.Temperature.ToString("0.##")
                              + " top_p=" + options.TopP.ToString("0.##")
                              + " repeatPenalty=" + options.RepeatPenalty.ToString("0.##")
                              + " repeatLastN=" + options.RepeatLastN);
                }

                Debug.Log("[LLMChatRunner] " + CharacterId
                          + " promptTokens~" + LastPromptTokensApprox
                          + " histLines=" + histLines
                          + " timeout=" + generateTimeoutSeconds.ToString("0") + "s");

                var genSw = Stopwatch.StartNew();
                string text = null;
                string failReason = null;
                const int maxAttempts = 2; // first try + one retry on validation fail
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    string promptForAttempt = userPrompt;
                    if (attempt > 0 && !string.IsNullOrEmpty(failReason))
                    {
                        // Retry hint before /no_think; reserve hint tokens then apply the same trim.
                        string hint = "(Previous draft was rejected: " + failReason
                                      + ". Fix that and reply again.)";
                        int reserve = EstimateTokens(hint) + 8;
                        string baseForRetry = FitPromptToBudget(
                            system, userPrompt, sharedHistory, directorInstruction, ref histLines, reserve);
                        promptForAttempt = InsertBeforeNoThink(baseForRetry, hint);
                        LastPromptTokensApprox = EstimateTokens(system) + EstimateTokens(promptForAttempt);
                    }

                    string raw;
                    try
                    {
                        // Streaming removed: ignore partial callbacks; Chunk fires once with the final text.
                        raw = await _backend.GenerateAsync(
                            promptForAttempt,
                            options,
                            _ => { },
                            ct);
                    }
                    catch (OperationCanceledException)
                    {
                        if (externalCt.IsCancellationRequested) throw;
                        string why = "timeout after " + generateTimeoutSeconds.ToString("0")
                            + "s (promptTokens~" + LastPromptTokensApprox
                            + "; dual models on GPU may contend)";
                        Debug.LogWarning("[LLMChatRunner] " + CharacterId + " " + why);
                        Error?.Invoke(CharacterId, "timeout:" + why);
                        return;
                    }

                    if (!isActiveAndEnabled || ct.IsCancellationRequested) return;

                    text = SanitizeReply(raw, persona.DisplayName, persona.MaxReplyLength);
                    if (LLMChatReplyValidator.TryValidate(text, CharacterId, sharedHistory, out failReason))
                    {
                        failReason = null;
                        if (!string.IsNullOrEmpty(text))
                            Chunk?.Invoke(CharacterId, text);
                        break;
                    }

                    Debug.LogWarning("[LLMChatRunner] " + CharacterId
                                     + " validation fail reason=" + failReason
                                     + " attempt=" + (attempt + 1) + "/" + maxAttempts);
                    text = null;
                    if (attempt + 1 >= maxAttempts)
                        break;
                }

                genSw.Stop();
                LastGenerateMs = genSw.ElapsedMilliseconds;
                Debug.Log("[LLMChatRunner] " + CharacterId + " generateMs=" + LastGenerateMs
                          + " promptTokens~" + LastPromptTokensApprox);

                if (!isActiveAndEnabled || ct.IsCancellationRequested) return;

                if (string.IsNullOrEmpty(text))
                {
                    Error?.Invoke(CharacterId, failReason ?? "invalid_reply");
                    return;
                }

                float typingTarget = timings != null ? timings.RollTypingDuration(text) : 2f;
                float remaining = typingTarget - (float)genSw.Elapsed.TotalSeconds;
                if (remaining > 0.05f)
                    await DelaySeconds(remaining, ct);

                if (!isActiveAndEnabled || ct.IsCancellationRequested) return;
                Done?.Invoke(CharacterId, text);
            }
            catch (OperationCanceledException)
            {
                // Destroy / switch source — silent.
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LLMChatRunner] " + CharacterId + ": " + ex.Message);
                if (isActiveAndEnabled)
                    Error?.Invoke(CharacterId, ex.Message);
            }
            finally
            {
                _busy = false;
            }
        }

        public void CancelActive()
        {
            if (_cts == null) return;
            try { _cts.Cancel(); }
            catch { /* ignore */ }
            _cts.Dispose();
            _cts = null;
            _busy = false;
        }

        void OnDestroy() => CancelActive();
        void OnDisable() => CancelActive();

        string BuildSystemPrompt()
        {
            var sb = new StringBuilder();
            string friend = string.IsNullOrWhiteSpace(persona.DisplayName) ? "Friend" : persona.DisplayName.Trim();
            sb.Append("You are ").Append(friend).Append(". ");
            sb.Append("Write ONE message as ").Append(friend);
            sb.AppendLine(" only. Never write your own name or other people's lines.");
            sb.AppendLine(persona.SystemPrompt ?? "");
            if (!string.IsNullOrWhiteSpace(persona.StyleNotes))
            {
                sb.Append("Style: ");
                sb.AppendLine(persona.StyleNotes.Trim());
            }

            if (_beatTension == "mid" || _beatTension == "high")
            {
                if (!string.IsNullOrWhiteSpace(persona.StyleNotes))
                    sb.AppendLine("When it's serious: follow your tell — " + persona.StyleNotes.Trim());
                else
                    sb.AppendLine("When it's serious you go short and flat.");
            }

            if (persona.ExampleLines != null && persona.ExampleLines.Length > 0)
            {
                _lastPickedExamples = LLMChatExamplePicker.Pick(
                    persona.ExampleLines, persona.ExampleLineTags, _beatTension);
                sb.AppendLine("Example lines you might send:");
                foreach (var line in _lastPickedExamples)
                    sb.Append("- ").AppendLine(line);
            }

            sb.Append("Hard rules: reply as ").Append(friend);
            sb.Append(" only; 1-2 short sentences; no quotes around the whole reply; ");
            sb.Append("no narration, no stage directions, no asterisk actions, no markdown; ");
            sb.Append("never say you are an AI or a language model; ");
            sb.Append("never reply with only emoji; ");
            if (!string.IsNullOrWhiteSpace(KnowledgeRulesHint))
                sb.Append(KnowledgeRulesHint.Trim()).Append(' ');
            else
            {
                sb.Append("answer questions concretely from your known facts / allowed knowledge; ");
                sb.Append("be evasive ONLY about secrets you must not reveal; ");
            }
            sb.Append("max ~").Append(persona.MaxReplyLength).Append(" characters.");

            if (!string.IsNullOrWhiteSpace(_situationBlock))
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(_situationBlock.Trim());
            }

            return sb.ToString();
        }

        string BuildUserPrompt(
            IReadOnlyList<LLMChatHistoryTurn> sharedHistory,
            string directorInstruction,
            out int histLinesUsed)
        {
            histLinesUsed = 0;
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(directorInstruction))
            {
                sb.Append("Director note (follow this, stay in character): ");
                sb.AppendLine(directorInstruction.Trim());
            }

            string playerName = PlayerDisplayName;
            string latestPlayer = FindLatestPlayerText(sharedHistory);
            if (!string.IsNullOrEmpty(latestPlayer))
            {
                sb.Append("Reply directly to what ").Append(playerName).Append(" just said: \"");
                sb.Append(latestPlayer);
                sb.AppendLine("\"");
            }

            sb.AppendLine("Recent group chat (includes your own prior lines labelled as you):");
            histLinesUsed = AppendHistory(sb, sharedHistory, historyWindowTurns, playerName);
            if (histLinesUsed == 0)
                sb.AppendLine("(no prior messages)");

            sb.Append("Write ").Append(persona.DisplayName).Append("'s next message only.");
            return sb.ToString();
        }

        static string EnsureNoThinkSuffix(string userPrompt)
        {
            if (string.IsNullOrEmpty(userPrompt)) return "/no_think";
            if (userPrompt.IndexOf("/no_think", StringComparison.OrdinalIgnoreCase) < 0)
                return userPrompt.TrimEnd() + "\n/no_think";
            return userPrompt;
        }

        /// <summary>Insert a line immediately before the trailing /no_think (or append then re-suffix).</summary>
        static string InsertBeforeNoThink(string userPrompt, string line)
        {
            if (string.IsNullOrEmpty(line)) return userPrompt;
            const string tag = "/no_think";
            int idx = userPrompt != null
                ? userPrompt.LastIndexOf(tag, StringComparison.OrdinalIgnoreCase)
                : -1;
            if (idx >= 0)
            {
                string before = userPrompt.Substring(0, idx).TrimEnd();
                string after = userPrompt.Substring(idx);
                return before + "\n" + line + "\n" + after;
            }
            return EnsureNoThinkSuffix((userPrompt ?? "").TrimEnd() + "\n" + line);
        }

        /// <param name="reserveExtraTokens">
        /// Tokens reserved for a soon-to-be-inserted suffix (e.g. retry hint) so the trim leaves room.
        /// </param>
        string FitPromptToBudget(
            string system,
            string userPrompt,
            IReadOnlyList<LLMChatHistoryTurn> sharedHistory,
            string directorInstruction,
            ref int histLines,
            int reserveExtraTokens = 0)
        {
            int budget = Mathf.Max(200, MaxPromptTokensApprox - Mathf.Max(0, reserveExtraTokens));
            LastPromptTokensApprox = EstimateTokens(system) + EstimateTokens(userPrompt);
            if (LastPromptTokensApprox <= budget) return userPrompt;
            userPrompt = TruncateUserPromptToBudget(
                system, sharedHistory, directorInstruction, budget, out histLines);
            userPrompt = EnsureNoThinkSuffix(userPrompt);
            LastPromptTokensApprox = EstimateTokens(system) + EstimateTokens(userPrompt);
            Debug.LogWarning("[LLMChatRunner] " + CharacterId
                + " prompt truncated to ~" + LastPromptTokensApprox + " tokens (histLines=" + histLines + ")");
            return userPrompt;
        }

        string TruncateUserPromptToBudget(
            string system,
            IReadOnlyList<LLMChatHistoryTurn> sharedHistory,
            string directorInstruction,
            int maxTokens,
            out int histLinesUsed)
        {
            histLinesUsed = 0;
            string playerName = PlayerDisplayName;
            int systemTok = EstimateTokens(system);
            int budget = Mathf.Max(200, maxTokens - systemTok);
            for (int window = historyWindowTurns; window >= 2; window--)
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(directorInstruction))
                {
                    sb.Append("Director note: ");
                    sb.AppendLine(directorInstruction.Trim());
                }
                string latestPlayer = FindLatestPlayerText(sharedHistory);
                if (!string.IsNullOrEmpty(latestPlayer))
                {
                    sb.Append("Reply directly to what ").Append(playerName).Append(" just said: \"");
                    sb.Append(latestPlayer);
                    sb.AppendLine("\"");
                }
                sb.AppendLine("Recent group chat:");
                histLinesUsed = AppendHistory(sb, sharedHistory, window, playerName);
                sb.Append("Write ").Append(persona.DisplayName).Append("'s next message only.");
                if (EstimateTokens(sb.ToString()) <= budget)
                    return sb.ToString();
            }

            // Last resort: player line + instruction only.
            var tiny = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(directorInstruction))
                tiny.AppendLine(directorInstruction.Trim());
            string lp = FindLatestPlayerText(sharedHistory);
            if (!string.IsNullOrEmpty(lp))
                tiny.Append("Reply directly to what ").Append(playerName).Append(" just said: \"").Append(lp).AppendLine("\"");
            tiny.Append("Write ").Append(persona.DisplayName).Append("'s next message only.");
            histLinesUsed = 0;
            return tiny.ToString();
        }

        int AppendHistory(StringBuilder sb, IReadOnlyList<LLMChatHistoryTurn> sharedHistory, int window, string playerName)
        {
            if (sharedHistory == null || sharedHistory.Count == 0) return 0;
            int start = Mathf.Max(0, sharedHistory.Count - window);
            int n = 0;
            for (int i = start; i < sharedHistory.Count; i++)
            {
                var t = sharedHistory[i];
                if (t == null) continue;
                string who;
                if (t.FromPlayer)
                {
                    who = playerName;
                }
                else if (!string.IsNullOrEmpty(t.SpeakerDisplayName)
                         && !string.Equals(t.SpeakerDisplayName, "You", StringComparison.OrdinalIgnoreCase))
                {
                    who = t.SpeakerDisplayName;
                }
                else
                {
                    who = t.SpeakerId ?? "Friend";
                }
                sb.Append(who).Append(": ").AppendLine(t.Text ?? "");
                n++;
            }
            return n;
        }

        static string FindLatestPlayerText(IReadOnlyList<LLMChatHistoryTurn> sharedHistory)
        {
            if (sharedHistory == null) return null;
            for (int i = sharedHistory.Count - 1; i >= 0; i--)
            {
                var t = sharedHistory[i];
                if (t != null && t.FromPlayer && !string.IsNullOrWhiteSpace(t.Text))
                    return t.Text.Trim();
            }
            return null;
        }

        public static int EstimateTokens(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            return Mathf.Max(1, (s.Length + 3) / 4);
        }

        public static string SanitizeReply(string raw, string speakerDisplayName, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string t = StripThinkBlocks(raw).Trim();
            if (t.Length >= 2 &&
                ((t[0] == '"' && t[t.Length - 1] == '"') || (t[0] == '\'' && t[t.Length - 1] == '\'')))
                t = t.Substring(1, t.Length - 2).Trim();

            t = StripLeadingSpeakerLabel(t, speakerDisplayName);
            t = CutAtNextSpeakerLine(t, speakerDisplayName);

            int nl = t.IndexOf('\n');
            if (nl > 0)
            {
                int nl2 = t.IndexOf('\n', nl + 1);
                if (nl2 > 0) t = t.Substring(0, nl2).Trim();
            }

            if (maxLen > 0 && t.Length > maxLen)
                t = t.Substring(0, maxLen).TrimEnd();
            return t;
        }

        /// <summary>Strip a leading "&lt;Name&gt;:" label the model echoed.</summary>
        public static string StripLeadingSpeakerLabel(string text, string speakerDisplayName)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string t = text.TrimStart();
            if (!string.IsNullOrWhiteSpace(speakerDisplayName))
            {
                string prefix = speakerDisplayName.Trim() + ":";
                if (t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return t.Substring(prefix.Length).TrimStart();
            }

            // Generic "Word:" at start (single token name).
            int colon = t.IndexOf(':');
            if (colon > 0 && colon <= 24)
            {
                bool allName = true;
                for (int i = 0; i < colon; i++)
                {
                    char c = t[i];
                    if (char.IsLetter(c) || c == ' ' || c == '-' || c == '\'') continue;
                    allName = false;
                    break;
                }
                if (allName) return t.Substring(colon + 1).TrimStart();
            }
            return t;
        }

        /// <summary>Cut at the first "\n&lt;Name&gt;:" (own name or another speaker line).</summary>
        public static string CutAtNextSpeakerLine(string text, string speakerDisplayName)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (!string.IsNullOrWhiteSpace(speakerDisplayName))
            {
                string cut = "\n" + speakerDisplayName.Trim() + ":";
                int idx = text.IndexOf(cut, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) return text.Substring(0, idx).TrimEnd();
            }

            // Any subsequent "Name:" line (multi-speaker leakage).
            int search = 0;
            while (true)
            {
                int nl = text.IndexOf('\n', search);
                if (nl < 0 || nl + 1 >= text.Length) break;
                int lineStart = nl + 1;
                int colon = text.IndexOf(':', lineStart);
                if (colon > lineStart && colon - lineStart <= 24)
                {
                    bool allName = true;
                    for (int i = lineStart; i < colon; i++)
                    {
                        char c = text[i];
                        if (char.IsLetter(c) || c == ' ' || c == '-' || c == '\'') continue;
                        allName = false;
                        break;
                    }
                    if (allName) return text.Substring(0, nl).TrimEnd();
                }
                search = lineStart;
            }
            return text;
        }

        public static string StripThinkBlocks(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            // Qwen3 thinking blocks (+ redacted variant if a template emits it).
            string t = StripTaggedBlocks(raw, "<think>", "</think>");
            t = StripTaggedBlocks(t, "<redacted_thinking>", "</redacted_thinking>");
            return t;
        }

        static string StripTaggedBlocks(string raw, string open, string close)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string t = raw;
            while (true)
            {
                int a = t.IndexOf(open, StringComparison.OrdinalIgnoreCase);
                if (a < 0) break;
                int b = t.IndexOf(close, a, StringComparison.OrdinalIgnoreCase);
                if (b < 0)
                {
                    t = t.Substring(0, a);
                    break;
                }
                t = t.Substring(0, a) + t.Substring(b + close.Length);
            }
            return t;
        }

        public static bool IsEmojiOnly(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            foreach (var ch in text)
            {
                if (char.IsWhiteSpace(ch)) continue;
                if (char.IsLetterOrDigit(ch)) return false;
                if (ch == '?' || ch == '!' || ch == '.' || ch == ',' || ch == '\'' || ch == '"'
                    || ch == ':' || ch == ';' || ch == '-' || ch == '_')
                    return false;
            }
            return true;
        }

        static async Task DelaySeconds(float seconds, CancellationToken ct)
        {
            if (seconds <= 0f) return;
            int ms = Mathf.Max(1, Mathf.RoundToInt(seconds * 1000f));
            await Task.Delay(ms, ct);
        }
    }
}
