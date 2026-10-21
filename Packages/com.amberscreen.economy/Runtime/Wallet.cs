using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Economy
{
    /// <summary>Default in-memory wallet with holds and a transaction log.</summary>
    public sealed class Wallet : IWallet
    {
        /// <summary>When true, logs add/spend/reserve/release/commit. Off by default (no DevConsole ref).</summary>
        public static bool Verbose;

        private long balanceMinor;
        private readonly Dictionary<string, Hold> holds = new Dictionary<string, Hold>();
        private readonly List<WalletTransaction> log = new List<WalletTransaction>();

        public long Balance => balanceMinor;

        public long Available
        {
            get
            {
                long held = 0;
                foreach (var pair in holds)
                    held += pair.Value.AmountMinor;
                return balanceMinor - held;
            }
        }

        public IReadOnlyList<WalletTransaction> Log => log;

        public event Action Changed;

        public Wallet(long startingMinor = 0) => balanceMinor = startingMinor;

        public void Add(long amountMinor, string reason)
        {
            balanceMinor += amountMinor;
            AppendLog(amountMinor, reason, WalletTransactionType.Add);
            if (Verbose)
                Debug.Log($"[Wallet] +{amountMinor} ({reason}) → {balanceMinor}");
            Changed?.Invoke();
        }

        public bool TrySpend(long amountMinor, string reason)
        {
            if (amountMinor < 0) amountMinor = -amountMinor;
            if (Available < amountMinor) return false;
            balanceMinor -= amountMinor;
            AppendLog(-amountMinor, reason, WalletTransactionType.Spend);
            if (Verbose)
                Debug.Log($"[Wallet] -{amountMinor} ({reason}) → {balanceMinor}");
            Changed?.Invoke();
            return true;
        }

        public bool TryReserve(long amountMinor, string reason, out string holdId)
        {
            holdId = null;
            if (amountMinor < 0) amountMinor = -amountMinor;
            if (amountMinor == 0 || Available < amountMinor) return false;

            holdId = Guid.NewGuid().ToString("N");
            holds[holdId] = new Hold
            {
                Id = holdId,
                AmountMinor = amountMinor,
                Reason = reason ?? ""
            };
            AppendLog(amountMinor, reason, WalletTransactionType.Reserve);
            if (Verbose)
                Debug.Log($"[Wallet] reserve {amountMinor} ({reason}) hold={holdId} available={Available}");
            Changed?.Invoke();
            return true;
        }

        public bool Release(string holdId)
        {
            if (string.IsNullOrEmpty(holdId) || !holds.TryGetValue(holdId, out var hold))
                return false;
            holds.Remove(holdId);
            AppendLog(hold.AmountMinor, hold.Reason, WalletTransactionType.Release);
            if (Verbose)
                Debug.Log($"[Wallet] release hold={holdId} ({hold.Reason}) available={Available}");
            Changed?.Invoke();
            return true;
        }

        public bool Commit(string holdId)
        {
            if (string.IsNullOrEmpty(holdId) || !holds.TryGetValue(holdId, out var hold))
                return false;
            holds.Remove(holdId);
            balanceMinor -= hold.AmountMinor;
            AppendLog(-hold.AmountMinor, hold.Reason, WalletTransactionType.Commit);
            if (Verbose)
                Debug.Log($"[Wallet] commit hold={holdId} -{hold.AmountMinor} ({hold.Reason}) → {balanceMinor}");
            Changed?.Invoke();
            return true;
        }

        /// <summary>Replace balance, holds and log (save restore / dev reset).</summary>
        public void RestoreState(
            long balance,
            IList<WalletSaveData.HoldEntry> holdEntries,
            IList<WalletTransaction> transactions)
        {
            balanceMinor = balance;
            holds.Clear();
            if (holdEntries != null)
            {
                for (int i = 0; i < holdEntries.Count; i++)
                {
                    var h = holdEntries[i];
                    if (h == null || string.IsNullOrEmpty(h.id)) continue;
                    holds[h.id] = new Hold
                    {
                        Id = h.id,
                        AmountMinor = h.amount,
                        Reason = h.reason ?? ""
                    };
                }
            }

            log.Clear();
            if (transactions != null)
            {
                for (int i = 0; i < transactions.Count; i++)
                {
                    if (transactions[i] != null)
                        log.Add(transactions[i]);
                }
            }

            Changed?.Invoke();
        }

        public void CaptureHolds(List<WalletSaveData.HoldEntry> into)
        {
            if (into == null) return;
            into.Clear();
            foreach (var pair in holds)
            {
                into.Add(new WalletSaveData.HoldEntry
                {
                    id = pair.Value.Id,
                    amount = pair.Value.AmountMinor,
                    reason = pair.Value.Reason
                });
            }
        }

        public void CaptureLog(List<WalletTransaction> into)
        {
            if (into == null) return;
            into.Clear();
            for (int i = 0; i < log.Count; i++)
                into.Add(log[i]);
        }

        private void AppendLog(long amount, string reason, WalletTransactionType type)
        {
            log.Add(new WalletTransaction
            {
                time = DateTime.UtcNow.ToString("o"),
                amount = amount,
                reason = reason ?? "",
                type = type
            });
        }

        private sealed class Hold
        {
            public string Id;
            public long AmountMinor;
            public string Reason;
        }
    }
}
