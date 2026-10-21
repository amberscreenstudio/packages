using System;
using System.Collections.Generic;

namespace AmberScreen.Economy
{
    /// <summary>
    /// Cash wallet in minor units (cents). Display via <see cref="Money.FormatMinor"/>.
    /// Holds reduce <see cref="Available"/> until released or committed.
    /// </summary>
    public interface IWallet
    {
        /// <summary>Settled balance in minor units.</summary>
        long Balance { get; }

        /// <summary>Balance minus active hold amounts.</summary>
        long Available { get; }

        void Add(long amountMinor, string reason);
        bool TrySpend(long amountMinor, string reason);

        /// <summary>Reserve funds (does not change Balance). Returns false if Available is insufficient.</summary>
        bool TryReserve(long amountMinor, string reason, out string holdId);

        /// <summary>Drop a hold without spending.</summary>
        bool Release(string holdId);

        /// <summary>Spend a previously reserved hold (Balance decreases).</summary>
        bool Commit(string holdId);

        IReadOnlyList<WalletTransaction> Log { get; }

        event Action Changed;
    }
}
