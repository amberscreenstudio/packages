using System;
using System.Collections.Generic;

namespace AmberScreen.Economy
{
    /// <summary>Serializable wallet blob (balance, active holds, transaction log).</summary>
    [Serializable]
    public sealed class WalletSaveData
    {
        public int version = 1;
        public long balance;
        public List<HoldEntry> holds = new List<HoldEntry>();
        public List<WalletTransaction> log = new List<WalletTransaction>();

        [Serializable]
        public sealed class HoldEntry
        {
            public string id;
            public long amount;
            public string reason;
        }
    }
}
