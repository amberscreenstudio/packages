using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AmberScreen.Save.Tests
{
    public sealed class SaveStoreTests
    {
        [Serializable]
        public sealed class CounterState
        {
            public bool hasCount;
            public int count;
            public string note = "";
        }

        sealed class CounterProvider : ISaveStateProvider
        {
            public int count;
            public string note = "";
            public int restores;
            readonly string m_Key;
            public CounterProvider(string key = "test.counter") { m_Key = key; }
            public string Key => m_Key;
            public object Capture() => new CounterState { hasCount = true, count = count, note = note };
            public void Restore(object state)
            {
                var s = (CounterState)state;
                if (s.hasCount) count = s.count;
                note = s.note;
                restores++;
            }
        }

        SaveSettings m_Settings;
        string m_Folder;

        [SetUp]
        public void SetUp()
        {
            SaveRegistry.Clear();
            m_Settings = ScriptableObject.CreateInstance<SaveSettings>();
            m_Settings.folderName = Path.Combine("AmberScreenSaveTests", Guid.NewGuid().ToString("N"));
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

        [TestCase("0.3.4", 3)]
        [TestCase("0.1.0", 1)]
        [TestCase("0.12", 12)]
        [TestCase("v0.2.0", 2)]
        [TestCase("1.2.0", 1)]
        [TestCase("2", 2)]
        [TestCase("", 0)]
        [TestCase(null, 0)]
        [TestCase("abc", 0)]
        public void MajorOfReadsTheMajorPart(string version, int expected)
        {
            Assert.That(SaveStore.MajorOf(version), Is.EqualTo(expected));
        }

        [Test]
        public void MajorMismatchUsesTheSecondNumberUnderZeroDotMajor()
        {
            Assert.That(SaveStore.IsMajorMismatch("0.3.4", "0.4.0"), Is.True, "0.3 and 0.4 differ");
            Assert.That(SaveStore.IsMajorMismatch("0.3.1", "0.3.9"), Is.False, "same MAJOR 3");
            Assert.That(SaveStore.IsMajorMismatch("0.9.0", "1.0.0"), Is.True);
        }

        [Test]
        public void SettingsAppliedAsDefaultAreUsed()
        {
            Assert.That(SaveSettings.LoadOrDefault(), Is.SameAs(m_Settings));
            Assert.That(SaveStore.SlotCount, Is.EqualTo(4));
            StringAssert.StartsWith(Application.persistentDataPath, SaveStore.Folder);
            StringAssert.EndsWith("slot2.json", SaveStore.PathFor(2));
        }

        [Test]
        public void SaveThenLoadRestoresProviders()
        {
            var p = new CounterProvider { count = 7, note = "hello" };
            Assert.That(SaveRegistry.Register(p, typeof(CounterState)), Is.True);

            Assert.That(SaveStore.Save(1, new SaveEnvelope(), out string error), Is.True, error);
            Assert.That(SaveStore.Exists(1), Is.True);

            p.count = 0;
            p.note = "";
            var result = SaveStore.Load(1, null);
            Assert.That(result.ok, Is.True, result.message);
            Assert.That(p.count, Is.EqualTo(7));
            Assert.That(p.note, Is.EqualTo("hello"));
            Assert.That(result.savedVersion, Is.EqualTo(Application.version));
            Assert.That(result.versionMismatch, Is.False);
        }

        [Test]
        public void OverwriteKeepsABakAndLoadFallsBackToIt()
        {
            var p = new CounterProvider { count = 1 };
            SaveRegistry.Register(p, typeof(CounterState));
            Assert.That(SaveStore.Save(2, new SaveEnvelope(), out _), Is.True);
            p.count = 2;
            Assert.That(SaveStore.Save(2, new SaveEnvelope(), out _), Is.True);
            Assert.That(File.Exists(SaveStore.BakPath(2)), Is.True, "second save keeps the previous file as .bak");
            Assert.That(File.Exists(SaveStore.TempPath(2)), Is.False, "temp file is gone after the swap");

            File.Delete(SaveStore.PathFor(2));
            p.count = 0;
            var result = SaveStore.Load(2, null);
            Assert.That(result.ok, Is.True);
            Assert.That(p.count, Is.EqualTo(1), "loaded the older copy from .bak");
        }

        [Test]
        public void UnknownKeysAreReportedAndKeptOnResave()
        {
            var env = new SaveEnvelope();
            env.Set("other.system", "{\"x\":1}");
            Assert.That(SaveStore.Save(1, env, out _), Is.True);

            var p = new CounterProvider();
            SaveRegistry.Register(p, typeof(CounterState));
            var result = SaveStore.Load(1, null);
            Assert.That(result.ok, Is.True);
            CollectionAssert.Contains(result.skippedKeys, "other.system");

            var peek = SaveStore.Peek(1, null);
            Assert.That(peek, Is.Not.Null);
            Assert.That(SaveStore.Save(1, peek, out _), Is.True);
            var again = SaveStore.Peek(1, null);
            Assert.That(again.Find("other.system"), Is.Not.Null);
            Assert.That(again.Find("test.counter"), Is.Not.Null);
        }

        [Test]
        public void MissingFieldsKeepDefaultsWithHasFlags()
        {
            var p = new CounterProvider { count = 42 };
            SaveRegistry.Register(p, typeof(CounterState));
            Directory.CreateDirectory(SaveStore.Folder);
            File.WriteAllText(SaveStore.PathFor(1),
                "{\"gameVersion\":\"" + Application.version + "\",\"saveFormat\":3,\"entries\":[{\"key\":\"test.counter\",\"json\":\"{\\\"note\\\":\\\"old save\\\"}\"}]}");
            var result = SaveStore.Load(1, null);
            Assert.That(result.ok, Is.True, result.message);
            Assert.That(p.count, Is.EqualTo(42), "hasCount was false, so the default stays");
            Assert.That(p.note, Is.EqualTo("old save"));
        }

        [Test]
        public void DifferentMajorWarnsButStillLoads()
        {
            var p = new CounterProvider();
            SaveRegistry.Register(p, typeof(CounterState));
            Directory.CreateDirectory(SaveStore.Folder);
            File.WriteAllText(SaveStore.PathFor(3),
                "{\"gameVersion\":\"0.999.0\",\"saveFormat\":3,\"entries\":[{\"key\":\"test.counter\",\"json\":\"{\\\"hasCount\\\":true,\\\"count\\\":5}\"}]}");
            var result = SaveStore.Load(3, null);
            Assert.That(result.ok, Is.True);
            Assert.That(result.versionMismatch, Is.True);
            Assert.That(result.savedVersion, Is.EqualTo("0.999.0"));
            StringAssert.Contains("0.999.0", result.message);
            Assert.That(p.count, Is.EqualTo(5));
        }

        [Test]
        public void LegacyMigrationRunsForOldFiles()
        {
            Directory.CreateDirectory(SaveStore.Folder);
            File.WriteAllText(SaveStore.PathFor(1), "{\"version\":2,\"money\":12}");
            var p = new CounterProvider();
            SaveRegistry.Register(p, typeof(CounterState));
            int migrations = 0;
            var result = SaveStore.Load(1, json =>
            {
                migrations++;
                var env = new SaveEnvelope { gameVersion = "0.2.0", saveFormat = 3 };
                env.Set("test.counter", JsonUtility.ToJson(new CounterState { hasCount = true, count = 12 }));
                return env;
            });
            Assert.That(result.ok, Is.True);
            Assert.That(migrations, Is.EqualTo(1));
            Assert.That(p.count, Is.EqualTo(12));
        }

        [Test]
        public void DuplicateKeysAreRejected()
        {
            Assert.That(SaveRegistry.Register(new CounterProvider("dup"), typeof(CounterState)), Is.True);
            LogAssert.Expect(LogType.Error, "[Save] Duplicate provider key \"dup\" rejected.");
            Assert.That(SaveRegistry.Register(new CounterProvider("dup"), typeof(CounterState)), Is.False);
        }

        [Test]
        public void BadSlotsFailWithoutThrowing()
        {
            Assert.That(SaveStore.Save(-1, new SaveEnvelope(), out string error), Is.False);
            StringAssert.Contains("Bad slot", error);
            Assert.That(SaveStore.Save(SaveStore.SlotCount, new SaveEnvelope(), out _), Is.False);
            Assert.That(SaveStore.Load(99, null).ok, Is.False);
            Assert.That(SaveStore.Load(1, null).ok, Is.False, "empty slot");
            Assert.That(SaveStore.Delete(-3), Is.False);
        }

        [Test]
        public void DeleteAndNewestSlot()
        {
            Assert.That(SaveStore.NewestSlot(), Is.EqualTo(-1));
            Assert.That(SaveStore.Save(1, new SaveEnvelope(), out _), Is.True);
            Assert.That(SaveStore.Save(1, new SaveEnvelope(), out _), Is.True);
            Assert.That(SaveStore.NewestSlot(), Is.EqualTo(1));
            Assert.That(SaveStore.Delete(1), Is.True);
            Assert.That(SaveStore.Exists(1), Is.False);
            Assert.That(File.Exists(SaveStore.BakPath(1)), Is.False);
            Assert.That(SaveStore.NewestSlot(), Is.EqualTo(-1));
        }
    }
}
