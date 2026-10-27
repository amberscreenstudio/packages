using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AmberScreen.LLMChat
{
    /// <summary>
    /// Shared reply validation used before delivering an LLM line.
    /// Failures go to the host fallback path (after one runner retry) with the reason string.
    /// </summary>
    public static class LLMChatReplyValidator
    {
        public const string ReasonEmpty = "empty_reply";
        public const string ReasonEmojiOnly = "emoji_only";
        public const string ReasonSelfRepeat = "self_repeat";
        public const string ReasonCrossRepeat = "cross_repeat";
        public const string ReasonRoleplayMarkup = "roleplay_markup";
        public const string ReasonRepeatOpener = "repeat_opener";

        public const int SelfRepeatLookback = 6;
        public const float SelfRepeatSimilarityThreshold = 0.85f;
        public const int CrossRepeatLookback = 8;
        public const float CrossRepeatSimilarityThreshold = 0.8f;
        public const int CrossRepeatMinWords = 3;
        public const int OpenerLookback = 3;
        public const int OpenerMatchThreshold = 2;

        // Leading _ / * / # still rejected; paired _..._ removed — it false-positived on snake_case.
        static readonly Regex RoleplayMarkup = new Regex(
            @"(\*[^*\n]+\*)|(\*\*[^*\n]+\*\*)|(^[\*_#])",
            RegexOptions.Multiline | RegexOptions.Compiled);

        public static bool TryValidate(
            string reply,
            string characterId,
            IReadOnlyList<LLMChatHistoryTurn> sharedHistory,
            out string failReason)
        {
            failReason = null;
            if (string.IsNullOrWhiteSpace(reply))
            {
                failReason = ReasonEmpty;
                return false;
            }

            if (LLMChatRunner.IsEmojiOnly(reply))
            {
                failReason = ReasonEmojiOnly;
                return false;
            }

            if (HasRoleplayMarkup(reply))
            {
                failReason = ReasonRoleplayMarkup;
                return false;
            }

            if (IsSelfRepeat(reply, characterId, sharedHistory, SelfRepeatLookback, SelfRepeatSimilarityThreshold))
            {
                failReason = ReasonSelfRepeat;
                return false;
            }

            if (IsCrossSpeakerRepeat(
                    reply, characterId, sharedHistory, CrossRepeatLookback, CrossRepeatSimilarityThreshold))
            {
                failReason = ReasonCrossRepeat;
                return false;
            }

            if (IsRepeatOpener(reply, characterId, sharedHistory, OpenerLookback, OpenerMatchThreshold))
            {
                failReason = ReasonRepeatOpener;
                return false;
            }

            return true;
        }

        public static bool HasRoleplayMarkup(string reply)
        {
            if (string.IsNullOrEmpty(reply)) return false;
            string t = reply.Trim();
            if (t.Length > 0 && (t[0] == '*' || t[0] == '_' || t[0] == '#'))
                return true;
            return RoleplayMarkup.IsMatch(t);
        }

        public static bool IsSelfRepeat(
            string reply,
            string characterId,
            IReadOnlyList<LLMChatHistoryTurn> sharedHistory,
            int lookback,
            float threshold)
        {
            if (string.IsNullOrWhiteSpace(reply) || string.IsNullOrEmpty(characterId) || sharedHistory == null)
                return false;

            string needle = Normalize(reply);
            if (needle.Length < 3) return false;

            int found = 0;
            for (int i = sharedHistory.Count - 1; i >= 0 && found < lookback; i--)
            {
                var turn = sharedHistory[i];
                if (turn == null || turn.FromPlayer) continue;
                if (!string.Equals(turn.SpeakerId, characterId, StringComparison.OrdinalIgnoreCase))
                    continue;
                found++;
                float sim = Similarity(needle, Normalize(turn.Text));
                if (sim >= threshold)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// True when <paramref name="reply"/> closely matches a recent line from another speaker
        /// (Dice ≥ threshold, or shorter ≥4-word line contained in the longer).
        /// Self and (by default) player turns are skipped.
        /// </summary>
        public static bool IsCrossSpeakerRepeat(
            string reply,
            string characterId,
            IReadOnlyList<LLMChatHistoryTurn> history,
            int lookback,
            float threshold,
            bool includePlayer = false)
        {
            if (string.IsNullOrWhiteSpace(reply) || history == null)
                return false;

            string needle = Normalize(reply);
            if (WordCount(needle) < CrossRepeatMinWords)
                return false;

            int found = 0;
            for (int i = history.Count - 1; i >= 0 && found < lookback; i--)
            {
                var turn = history[i];
                if (turn == null) continue;
                if (!includePlayer && turn.FromPlayer) continue;
                if (!string.IsNullOrEmpty(characterId)
                    && string.Equals(turn.SpeakerId, characterId, StringComparison.OrdinalIgnoreCase))
                    continue;

                found++;
                string other = Normalize(turn.Text);
                if (string.IsNullOrEmpty(other)) continue;

                if (Similarity(needle, other) >= threshold)
                    return true;

                string shorter = needle.Length <= other.Length ? needle : other;
                string longer = needle.Length <= other.Length ? other : needle;
                if (WordCount(shorter) >= 4 && longer.Contains(shorter))
                    return true;
            }

            return false;
        }

        static int WordCount(string normalized)
        {
            if (string.IsNullOrEmpty(normalized)) return 0;
            int n = 1;
            for (int i = 0; i < normalized.Length; i++)
                if (normalized[i] == ' ') n++;
            return n;
        }

        /// <summary>
        /// Fails when the reply's first word matches the opener of ≥ <paramref name="matchThreshold"/>
        /// of that friend's last <paramref name="lookback"/> messages.
        /// </summary>
        public static bool IsRepeatOpener(
            string reply,
            string characterId,
            IReadOnlyList<LLMChatHistoryTurn> sharedHistory,
            int lookback,
            int matchThreshold)
        {
            string opener = FirstWord(reply);
            if (string.IsNullOrEmpty(opener) || sharedHistory == null || string.IsNullOrEmpty(characterId))
                return false;

            int found = 0;
            int matches = 0;
            for (int i = sharedHistory.Count - 1; i >= 0 && found < lookback; i--)
            {
                var turn = sharedHistory[i];
                if (turn == null || turn.FromPlayer) continue;
                if (!string.Equals(turn.SpeakerId, characterId, StringComparison.OrdinalIgnoreCase))
                    continue;
                found++;
                if (string.Equals(FirstWord(turn.Text), opener, StringComparison.Ordinal))
                    matches++;
            }

            return matches >= matchThreshold;
        }

        public static string FirstWord(string text)
        {
            string n = Normalize(text);
            if (string.IsNullOrEmpty(n)) return "";
            int sp = n.IndexOf(' ');
            return sp < 0 ? n : n.Substring(0, sp);
        }

        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder(text.Length);
            bool prevSpace = false;
            foreach (var ch in text.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                    prevSpace = false;
                }
                else if (char.IsWhiteSpace(ch))
                {
                    if (!prevSpace && sb.Length > 0)
                    {
                        sb.Append(' ');
                        prevSpace = true;
                    }
                }
            }
            return sb.ToString().Trim();
        }

        public static float Similarity(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1f;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;
            if (a == b) return 1f;

            if (a.Length <= 4 || b.Length <= 4)
            {
                if (a.Contains(b) || b.Contains(a))
                    return Mathf.Min(a.Length, b.Length) / (float)Mathf.Max(a.Length, b.Length);
            }

            var bigramsA = Bigrams(a);
            var bigramsB = Bigrams(b);
            if (bigramsA.Count == 0 || bigramsB.Count == 0)
                return a == b ? 1f : 0f;

            int overlap = 0;
            foreach (var kv in bigramsA)
            {
                if (bigramsB.TryGetValue(kv.Key, out int n))
                    overlap += Mathf.Min(kv.Value, n);
            }

            return (2f * overlap) / (Sum(bigramsA) + Sum(bigramsB));
        }

        static Dictionary<string, int> Bigrams(string s)
        {
            var d = new Dictionary<string, int>();
            if (s.Length < 2)
            {
                d[s] = 1;
                return d;
            }
            for (int i = 0; i < s.Length - 1; i++)
            {
                string g = s.Substring(i, 2);
                d.TryGetValue(g, out int n);
                d[g] = n + 1;
            }
            return d;
        }

        static int Sum(Dictionary<string, int> d)
        {
            int s = 0;
            foreach (var kv in d) s += kv.Value;
            return s;
        }
    }
}
