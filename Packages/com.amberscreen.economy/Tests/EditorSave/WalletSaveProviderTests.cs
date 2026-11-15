using System;
using System.IO;
using AmberScreen.Save;
using NUnit.Framework;
using UnityEngine;

namespace AmberScreen.Economy.Save.Tests
{
    public sealed class WalletSaveProviderTests
    {
        SaveSettings m_Settings;
        string m_Folder;

        [SetUp]
        public void SetUp()
        {
            SaveRegistry.Clear();
            m_Settings = ScriptableObject.CreateInstance<SaveSettings>();
            m_Settings.folderName = Path.Combine("AmberScreenEconomySaveTests", Guid.NewGuid().ToString("N"));
            m_Settings.ApplyAsDefault();
            m_Folder = SaveStore.Folder;
        }

        [TearDown]
        public void TearDown()
        {
            SaveRegistry.Clear();
            SaveSettings.ClearCache();
            UnityEngine.Object.DestroyImmediate(m_Settings);
            try
            {
                if (Directory.Exists(m_Folder)) Directory.Delete(m_Folder, true);
                string parent = Path.GetDirectoryName(m_Folder);
                if (Directory.Exists(parent) && Directory.GetFileSystemEntries(parent).Length == 0)
                    Directory.Delete(parent);
            }
            catch (IOException) { }
        }

        [Test]
        public void WalletRoundTripsThroughTheSavePackage()
        {
            var wallet = new Wallet();
            wallet.Add(12_345, "seed");
            Assert.IsTrue(wallet.TryReserve(200, "pending", out _));
            var provider = WalletSaveProvider.Register(wallet);
            Assert.IsNotNull(provider);
            Assert.AreEqual("wallet", provider.Key);

            Assert.IsTrue(SaveStore.Save(1, new SaveEnvelope(), out string error), error);

            wallet.RestoreState(0, null, null);
            Assert.AreEqual(0, wallet.Balance);

            var result = SaveStore.Load(1, null);
            Assert.IsTrue(result.ok, result.message);
            Assert.AreEqual(12_345, wallet.Balance);
            Assert.AreEqual(12_145, wallet.Available);
        }
    }
}
