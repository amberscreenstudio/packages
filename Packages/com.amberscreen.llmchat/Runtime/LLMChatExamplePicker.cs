using System;
using System.Collections.Generic;

namespace AmberScreen.LLMChat
{
    /// <summary>
    /// Picks 6–8 example lines for a prompt: weighted by beat tension / tell tags,
    /// never two samples with the same normalised opener in one pick.
    /// </summary>
    public static class LLMChatExamplePicker
    {
        public const int MinPick = 6;
        public const int MaxPick = 8;

        /// <summary>
        /// Tag values on persona lines: empty, <c>tell</c>, <c>low</c>, <c>mid</c>, <c>high</c>.
        /// Beat tension: <c>low</c> / <c>mid</c> / <c>high</c>.
        /// </summary>
        public static List<string> Pick(
            string[] lines,
            string[] tags,
            string beatTension,
            System.Random rng = null)
        {
            rng ??= new System.Random();
            var result = new List<string>();
            if (lines == null || lines.Length == 0) return result;

            string tension = string.IsNullOrEmpty(beatTension) ? "low" : beatTension.Trim().ToLowerInvariant();
            bool preferTell = tension == "mid" || tension == "high";

            var indexed = new List<int>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    indexed.Add(i);
            }

            // Prefer: matching tension / tell (when mid|high) → untagged → rest
            indexed.Sort((a, b) => Score(TagAt(tags, a), tension, preferTell)
                .CompareTo(Score(TagAt(tags, b), tension, preferTell)));
            // Lower score first; shuffle within same score via rng insert
            ShuffleStableByScore(indexed, tags, tension, preferTell, rng);

            var usedOpeners = new HashSet<string>(StringComparer.Ordinal);
            int target = Math.Min(MaxPick, Math.Max(MinPick, Math.Min(MaxPick, indexed.Count)));
            if (indexed.Count < MinPick) target = indexed.Count;

            foreach (int i in indexed)
            {
                if (result.Count >= target) break;
                string line = lines[i].Trim();
                string opener = LLMChatReplyValidator.FirstWord(line);
                if (!string.IsNullOrEmpty(opener) && !usedOpeners.Add(opener))
                    continue;
                result.Add(line);
            }

            // If opener rule left us short, fill without opener constraint
            if (result.Count < Math.Min(MinPick, indexed.Count))
            {
                foreach (int i in indexed)
                {
                    if (result.Count >= Math.Min(MinPick, indexed.Count)) break;
                    string line = lines[i].Trim();
                    if (result.Contains(line)) continue;
                    result.Add(line);
                }
            }

            return result;
        }

        static string TagAt(string[] tags, int i)
        {
            if (tags == null || i < 0 || i >= tags.Length || string.IsNullOrWhiteSpace(tags[i]))
                return "";
            return tags[i].Trim().ToLowerInvariant();
        }

        /// <summary>Lower is better.</summary>
        static int Score(string tag, string tension, bool preferTell)
        {
            if (preferTell && tag == "tell") return 0;
            if (tag == tension) return 1;
            if (string.IsNullOrEmpty(tag)) return 2;
            return 3;
        }

        static void ShuffleStableByScore(
            List<int> indexed,
            string[] tags,
            string tension,
            bool preferTell,
            System.Random rng)
        {
            // Group by score, shuffle each group, flatten.
            var buckets = new SortedDictionary<int, List<int>>();
            foreach (int i in indexed)
            {
                int s = Score(TagAt(tags, i), tension, preferTell);
                if (!buckets.TryGetValue(s, out var list))
                {
                    list = new List<int>();
                    buckets[s] = list;
                }
                list.Add(i);
            }

            indexed.Clear();
            foreach (var kv in buckets)
            {
                var list = kv.Value;
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
                indexed.AddRange(list);
            }
        }
    }
}
