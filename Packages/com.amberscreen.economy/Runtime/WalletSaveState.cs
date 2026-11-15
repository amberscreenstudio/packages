using System;
using AmberScreen.Economy.TempSave;
using UnityEngine;

namespace AmberScreen.Economy
{
    /// <summary>
    /// <see cref="ISaveStateProvider"/> for a <see cref="Wallet"/>.
    /// Persists balance, active holds and the transaction log.
    /// With <c>com.amberscreen.save</c> installed, use <c>AmberScreen.Economy.Save.WalletSaveProvider</c>
    /// (it wraps this class) instead of the temporary string interface.
    /// </summary>
    public sealed class WalletSaveState : ISaveStateProvider
    {
        public const int CurrentVersion = 1;

        private readonly Wallet wallet;
        private readonly string key;
        private WalletSaveData data = new WalletSaveData();

        public string Key => key;
        public WalletSaveData Data => data;

        public WalletSaveState(Wallet wallet, string key = "wallet")
        {
            this.wallet = wallet;
            this.key = string.IsNullOrEmpty(key) ? "wallet" : key;
        }

        public string Capture()
        {
            SyncFromWallet();
            return JsonUtility.ToJson(data, true);
        }

        /// <summary>Current wallet as a data object (for keyed saves such as <c>com.amberscreen.save</c>).</summary>
        public WalletSaveData CaptureData()
        {
            SyncFromWallet();
            return data;
        }

        /// <summary>Applies a data object (null = empty wallet). Same rules as <see cref="Restore(string)"/>.</summary>
        public void RestoreData(WalletSaveData loaded)
        {
            if (loaded == null)
            {
                data = new WalletSaveData { version = CurrentVersion };
                wallet?.RestoreState(0, data.holds, data.log);
                return;
            }

            data = loaded;
            if (data.holds == null)
                data.holds = new System.Collections.Generic.List<WalletSaveData.HoldEntry>();
            if (data.log == null)
                data.log = new System.Collections.Generic.List<WalletTransaction>();
            if (data.version != CurrentVersion)
                Debug.LogWarning($"[Wallet] save version {data.version} (expected {CurrentVersion})");
            wallet?.RestoreState(data.balance, data.holds, data.log);
        }

        public void SyncFromWallet()
        {
            data.version = CurrentVersion;
            data.balance = wallet != null ? wallet.Balance : 0;
            if (data.holds == null) data.holds = new System.Collections.Generic.List<WalletSaveData.HoldEntry>();
            if (data.log == null) data.log = new System.Collections.Generic.List<WalletTransaction>();
            wallet?.CaptureHolds(data.holds);
            wallet?.CaptureLog(data.log);
        }

        public void Restore(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                data = new WalletSaveData { version = CurrentVersion };
                wallet?.RestoreState(0, data.holds, data.log);
                return;
            }

            try
            {
                var loaded = JsonUtility.FromJson<WalletSaveData>(payload);
                if (loaded == null)
                {
                    data = new WalletSaveData { version = CurrentVersion };
                    wallet?.RestoreState(0, data.holds, data.log);
                    return;
                }

                data = loaded;
                if (data.holds == null)
                    data.holds = new System.Collections.Generic.List<WalletSaveData.HoldEntry>();
                if (data.log == null)
                    data.log = new System.Collections.Generic.List<WalletTransaction>();
                if (data.version != CurrentVersion)
                    Debug.LogWarning($"[Wallet] save version {data.version} (expected {CurrentVersion})");
                wallet?.RestoreState(data.balance, data.holds, data.log);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Wallet] restore failed: {e.Message}");
                data = new WalletSaveData { version = CurrentVersion };
                wallet?.RestoreState(0, data.holds, data.log);
            }
        }

        public void Clear()
        {
            data = new WalletSaveData { version = CurrentVersion };
            wallet?.RestoreState(0, data.holds, data.log);
        }
    }
}
