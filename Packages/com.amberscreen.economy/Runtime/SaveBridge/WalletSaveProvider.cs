using AmberScreen.Save;

namespace AmberScreen.Economy.Save
{
    /// <summary>
    /// Saves a <see cref="Wallet"/> through <c>com.amberscreen.save</c> (keyed slots, .bak, version stamp).
    /// This assembly only compiles when the save package is installed, so Economy never requires it.
    /// <code>WalletSaveProvider.Register(wallet);   // once at boot, key "wallet"</code>
    /// </summary>
    public sealed class WalletSaveProvider : ISaveStateProvider
    {
        readonly WalletSaveState m_State;

        public WalletSaveProvider(Wallet wallet, string key = "wallet")
        {
            m_State = new WalletSaveState(wallet, key);
        }

        public string Key => m_State.Key;

        /// <summary>The wrapped state (same data as the temporary string save).</summary>
        public WalletSaveState State => m_State;

        public object Capture() => m_State.CaptureData();

        public void Restore(object state) => m_State.RestoreData(state as WalletSaveData);

        /// <summary>Creates a provider and registers it with <see cref="SaveRegistry"/>. Null if the key is taken.</summary>
        public static WalletSaveProvider Register(Wallet wallet, string key = "wallet")
        {
            var provider = new WalletSaveProvider(wallet, key);
            return SaveRegistry.Register(provider, typeof(WalletSaveData)) ? provider : null;
        }
    }
}
