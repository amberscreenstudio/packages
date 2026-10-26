using System;
using System.Collections.Generic;
using System.Linq;

namespace AmberScreen.LLMChat
{
    /// <summary>
    /// Ordered model preference list with reply-time budget. Pure logic (no LLMUnity);
    /// the host loads/swaps weights when this reports a step-down.
    /// </summary>
    public sealed class ModelLadder
    {
        readonly string[] _models;
        readonly long _budgetMs;
        readonly List<long> _replySamples = new List<long>(4);
        int _index;
        bool _medianEvaluated;

        public ModelLadder(IReadOnlyList<string> models, float replyBudgetSeconds)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            var list = new List<string>(models.Count);
            foreach (var m in models)
            {
                if (string.IsNullOrWhiteSpace(m)) continue;
                list.Add(m.Trim());
            }

            if (list.Count == 0)
                throw new ArgumentException("Model ladder needs at least one model.", nameof(models));

            _models = list.ToArray();
            _budgetMs = Math.Max(1L, (long)(replyBudgetSeconds * 1000f));
            _index = 0;
        }

        public IReadOnlyList<string> Models => _models;
        public string ActiveModel => _models[_index];
        public int ActiveIndex => _index;
        public long BudgetMs => _budgetMs;
        public float BudgetSeconds => _budgetMs / 1000f;
        public bool HasNext => _index + 1 < _models.Length;
        public int ReplySampleCount => _replySamples.Count;

        /// <summary>
        /// After startup warmup generation: step down when elapsed exceeds budget.
        /// </summary>
        public bool EvaluateWarmup(long warmupMs, out string from, out string to)
        {
            from = null;
            to = null;
            if (warmupMs <= _budgetMs || !HasNext)
                return false;
            return TryStepDown(out from, out to);
        }

        /// <summary>
        /// Record a real reply duration. Once three samples exist, returns true when their
        /// median exceeds budget and a next model exists. Does not advance — call
        /// <see cref="TryStepDown"/> when the host is ready to swap weights.
        /// </summary>
        public bool EvaluateReply(long replyMs, out long? medianMs)
        {
            medianMs = null;
            if (_medianEvaluated)
                return false;

            _replySamples.Add(replyMs);
            if (_replySamples.Count < 3)
                return false;

            _medianEvaluated = true;
            medianMs = Median(_replySamples);
            return medianMs.Value > _budgetMs && HasNext;
        }

        public bool TryStepDown(out string from, out string to)
        {
            from = ActiveModel;
            to = null;
            if (!HasNext)
                return false;
            _index++;
            to = ActiveModel;
            _replySamples.Clear();
            _medianEvaluated = false;
            return true;
        }

        /// <summary>Median of a sample list (fake-timer friendly).</summary>
        public static long Median(IReadOnlyList<long> values)
        {
            if (values == null || values.Count == 0)
                return 0;

            var sorted = values.OrderBy(v => v).ToArray();
            int mid = sorted.Length / 2;
            if ((sorted.Length & 1) == 1)
                return sorted[mid];
            return (sorted[mid - 1] + sorted[mid]) / 2;
        }
    }
}
