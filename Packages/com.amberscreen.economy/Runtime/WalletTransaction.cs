using System;

namespace AmberScreen.Economy
{
    /// <summary>One wallet ledger row (time, amount in minor units, reason, type).</summary>
    [Serializable]
    public sealed class WalletTransaction
    {
        public string time;
        public long amount;
        public string reason;
        public WalletTransactionType type;
    }
}
