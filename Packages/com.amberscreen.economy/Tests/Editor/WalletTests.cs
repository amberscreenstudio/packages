using System.IO;
using AmberScreen.Economy.TempSave;
using NUnit.Framework;
using UnityEngine;

namespace AmberScreen.Economy.Tests
{
    public class WalletTests
    {
        [TearDown]
        public void TearDown()
        {
            Wallet.Verbose = false;
        }

        [Test]
        public void AddAndSpend_WithReasons_UpdatesBalanceAndLog()
        {
            IWallet wallet = new Wallet();
            wallet.Add(10_000, "payday");
            Assert.AreEqual(10_000, wallet.Balance);
            Assert.AreEqual(10_000, wallet.Available);
            Assert.IsTrue(wallet.TrySpend(3_000, "coffee"));
            Assert.AreEqual(7_000, wallet.Balance);
            Assert.AreEqual(2, wallet.Log.Count);
            Assert.AreEqual(WalletTransactionType.Add, wallet.Log[0].type);
            Assert.AreEqual("payday", wallet.Log[0].reason);
            Assert.AreEqual(WalletTransactionType.Spend, wallet.Log[1].type);
            Assert.AreEqual("coffee", wallet.Log[1].reason);
        }

        [Test]
        public void TrySpend_MoreThanAvailable_Fails()
        {
            IWallet wallet = new Wallet(500);
            Assert.IsFalse(wallet.TrySpend(501, "too much"));
            Assert.AreEqual(500, wallet.Balance);
            Assert.AreEqual(0, wallet.Log.Count);
        }

        [Test]
        public void Reserve_Release_And_Commit()
        {
            IWallet wallet = new Wallet(10_000);
            Assert.IsTrue(wallet.TryReserve(5_000, "hold", out string holdId));
            Assert.AreEqual(10_000, wallet.Balance);
            Assert.AreEqual(5_000, wallet.Available);

            Assert.IsTrue(wallet.Release(holdId));
            Assert.AreEqual(10_000, wallet.Available);

            Assert.IsTrue(wallet.TryReserve(4_000, "buy", out holdId));
            Assert.IsTrue(wallet.Commit(holdId));
            Assert.AreEqual(6_000, wallet.Balance);
            Assert.AreEqual(6_000, wallet.Available);
            Assert.AreEqual(WalletTransactionType.Commit, wallet.Log[wallet.Log.Count - 1].type);
        }

        [Test]
        public void TransactionLog_RecordsTypes()
        {
            IWallet wallet = new Wallet();
            wallet.Add(100, "a");
            wallet.TrySpend(10, "b");
            wallet.TryReserve(20, "c", out string id);
            wallet.Release(id);
            wallet.TryReserve(15, "d", out id);
            wallet.Commit(id);

            Assert.AreEqual(6, wallet.Log.Count);
            Assert.AreEqual(WalletTransactionType.Add, wallet.Log[0].type);
            Assert.AreEqual(WalletTransactionType.Spend, wallet.Log[1].type);
            Assert.AreEqual(WalletTransactionType.Reserve, wallet.Log[2].type);
            Assert.AreEqual(WalletTransactionType.Release, wallet.Log[3].type);
            Assert.AreEqual(WalletTransactionType.Reserve, wallet.Log[4].type);
            Assert.AreEqual(WalletTransactionType.Commit, wallet.Log[5].type);
        }

        [Test]
        public void Money_Format_UsesAppliedSettings()
        {
            var settings = ScriptableObject.CreateInstance<EconomyCurrencySettings>();
            var so = new UnityEditor.SerializedObject(settings);
            so.FindProperty("symbol").stringValue = "$";
            so.FindProperty("amountFormat").stringValue = "0.00";
            so.ApplyModifiedPropertiesWithoutUndo();
            Money.Apply(settings);
            Assert.AreEqual("$12.00", Money.Format(12f));
            Assert.AreEqual(Money.Format(1.5f), Money.FormatMinor(Money.ToMinor(1.5f)));
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void WalletSaveState_RoundTrip_ViaJsonBackend()
        {
            const string key = "economy_test_wallet_roundtrip";
            var path = JsonSaveBackend.PathForKey(key);
            if (File.Exists(path))
                File.Delete(path);

            var wallet = new Wallet();
            wallet.Add(12_345, "seed");
            Assert.IsTrue(wallet.TryReserve(200, "pending", out _));

            var save = new WalletSaveState(wallet, key);
            JsonSaveBackend.Write(key, save.Capture());

            var loaded = new Wallet();
            var restore = new WalletSaveState(loaded, key);
            Assert.IsTrue(JsonSaveBackend.TryRead(key, out string json));
            restore.Restore(json);

            Assert.AreEqual(12_345, loaded.Balance);
            Assert.AreEqual(12_145, loaded.Available);
            Assert.GreaterOrEqual(loaded.Log.Count, 2);

            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
