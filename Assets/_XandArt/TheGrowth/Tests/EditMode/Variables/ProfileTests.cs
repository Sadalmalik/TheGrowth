using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Profile;
using TheGrowth.Core.Variables;
using TheGrowth.Integrations.SaveFiles;
using TheGrowth.Tests.EditMode.Content;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Variables
{
    /// <summary>
    /// Профиль игрока: чтение при старте, запись при изменении, ошибки чтения и записи; файл профиля во временной папке
    /// (папку и профиль игрока тесты не трогают), атомарная запись.
    /// </summary>
    public sealed class ProfileTests
    {
        private ContentTestKit m_Kit;
        private VariableCatalog m_Catalog;
        private string m_Folder;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Catalog = VariableTestKit.Catalog(m_Kit,
                new VariableDeclaration("ending_seen", VariableScope.Profile, VariableType.Bool),
                new VariableDeclaration("gallery_count", VariableScope.Profile, VariableType.Int, null, null, "pictures"),
                new VariableDeclaration("coins", VariableScope.Game, VariableType.Int));
            m_Folder = Path.Combine(Path.GetTempPath(), "TheGrowthProfileTests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_Folder))
                Directory.Delete(m_Folder, true);

            m_Kit.Dispose();
        }

        /// <summary>Область с профилем на хранилище <paramref name="storage"/>; уничтожает вызывающий.</summary>
        private LifetimeScope CreateScope<T>(T storage, out ProfileService profile) where T : IShared, IProfileStorage
        {
            var scope = new LifetimeScope("Global");
            scope.Container.Add(new EventBus());
            scope.Container.Add(m_Catalog);
            scope.Container.Add(storage);
            profile = scope.Container.Add(new ProfileService());
            scope.Container.Init();
            return scope;
        }

        private VariableDeclaration Declaration(string name)
        {
            return m_Catalog.Get(name);
        }

        [Test]
        public void NoProfile_Defaults_NothingWritten()
        {
            var storage = new MemoryProfileStorage();
            using (CreateScope(storage, out var profile))
            {
                Assert.AreEqual(false, profile.Variables.Get(Declaration("ending_seen")));
                Assert.AreEqual(0, storage.WriteCount);
            }

            Assert.IsNull(storage.Text, "no profile is written without changes");
        }

        [Test]
        public void Load_ReadsValues_ChangeWritesWholeProfile()
        {
            var storage = new MemoryProfileStorage { Text = "{ \"version\": 1, \"variables\": { \"ending_seen\": true } }" };
            using (CreateScope(storage, out var profile))
            {
                Assert.AreEqual(true, profile.Variables.Get(Declaration("ending_seen")));

                profile.Variables.Set(Declaration("gallery_count"), 3);
                profile.Variables.Set(Declaration("gallery_count"), 3);

                Assert.AreEqual(1, storage.WriteCount, "a write per change; the same value is no change");
            }

            Assert.AreEqual("{\n  \"version\": 1,\n  \"variables\": {\n    \"ending_seen\": true,\n    \"gallery_count\": 3\n  }\n}",
                storage.Text.Replace("\r\n", "\n"));
        }

        [Test]
        public void Load_RenamedAndUnknown()
        {
            var storage = new MemoryProfileStorage { Text = "{ \"version\": 1, \"variables\": { \"pictures\": 5, \"removed\": true, \"coins\": 3 } }" };

            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape("[Profile] Profile: variable 'removed' is not declared")));
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape("[Profile] Profile: variable 'coins' is now a game variable")));
            using (CreateScope(storage, out var profile))
            {
                Assert.AreEqual(5, profile.Variables.Get(Declaration("gallery_count")));
                Assert.AreEqual(1, profile.Variables.Count);
            }
        }

        [Test]
        public void Load_Corrupted_KeepsCopy_DefaultsAndNextChangeOverwrites()
        {
            const string corrupted = "{ \"version\": 1, \"variables\": { ";
            var storage = new MemoryProfileStorage { Text = corrupted };

            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("[Profile] Profile can't be read, default values are used")));
            using (CreateScope(storage, out var profile))
            {
                Assert.That(storage.Copies, Is.EqualTo(new[] { corrupted }), "the unreadable profile is copied before any write");
                Assert.IsFalse(profile.IsReadOnly);
                Assert.AreEqual(0, profile.Variables.Count);
                profile.Variables.Set(Declaration("ending_seen"), true);
            }

            StringAssert.Contains("\"ending_seen\": true", storage.Text);
        }

        [Test]
        public void Load_Corrupted_CopyFails_ReadOnly()
        {
            const string corrupted = "{ \"version\": 1, \"variables\": { ";
            var storage = new MemoryProfileStorage { Text = corrupted, FailCopies = true };

            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("[Profile] Profile can't be read, default values are used")));
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("[Profile] The unreadable profile can't be copied, the profile is not overwritten: access denied")));
            using (CreateScope(storage, out var profile))
            {
                Assert.IsTrue(profile.IsReadOnly);
                profile.Variables.Set(Declaration("ending_seen"), true);

                Assert.AreEqual(true, profile.Variables.Get(Declaration("ending_seen")), "the value works in this session");
                Assert.AreEqual(0, storage.WriteCount);
            }

            Assert.AreEqual(corrupted, storage.Text, "the unreadable profile is not overwritten");
        }

        [Test]
        public void Load_NewerVersion_ReadsKnown_ReadOnly()
        {
            const string newer = "{ \"version\": 2, \"variables\": { \"ending_seen\": true }, \"achievements\": [ 1 ] }";
            var storage = new MemoryProfileStorage { Text = newer };

            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape("[Profile] Profile version 2 is newer than supported 1")));
            using (CreateScope(storage, out var profile))
            {
                Assert.IsTrue(profile.IsReadOnly);
                Assert.AreEqual(true, profile.Variables.Get(Declaration("ending_seen")));

                profile.Variables.Set(Declaration("gallery_count"), 3);
                Assert.AreEqual(0, storage.WriteCount);
            }

            Assert.AreEqual(newer, storage.Text, "a newer profile is not overwritten");
            Assert.IsEmpty(storage.Copies);
        }

        [Test]
        public void WriteError_Logged_ValueKept()
        {
            var storage = new MemoryProfileStorage { FailWrites = true };
            using (CreateScope(storage, out var profile))
            {
                LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("[Profile] Profile is not written: disk full")));
                Assert.DoesNotThrow(() => profile.Variables.Set(Declaration("ending_seen"), true));

                Assert.AreEqual(true, profile.Variables.Get(Declaration("ending_seen")));
                Assert.IsNull(storage.Text);
            }
        }

        [Test]
        public void ProfileFile_WriteRead_InFolder()
        {
            var storage = new ProfileFileStorage(m_Folder);
            Assert.AreEqual(Path.Combine(m_Folder, ProfileFileStorage.FileName), storage.FilePath);
            Assert.IsNull(storage.OpenRead(), "no file yet");

            storage.Write(stream => stream.Write(new byte[] { 1, 2, 3 }, 0, 3));
            storage.Write(stream => stream.Write(new byte[] { 4, 5 }, 0, 2));

            Assert.That(File.ReadAllBytes(storage.FilePath), Is.EqualTo(new byte[] { 4, 5 }));
            using (var stream = storage.OpenRead())
                Assert.AreEqual(2, stream.Length);

            Assert.That(Directory.GetFiles(m_Folder), Is.EqualTo(new[] { storage.FilePath }), "no temporary file left");
        }

        [Test]
        public void ProfileFile_FailedWrite_KeepsPreviousFile_NoTemporaryLeft()
        {
            var storage = new ProfileFileStorage(m_Folder);
            storage.Write(stream => stream.Write(new byte[] { 1, 2, 3 }, 0, 3));

            Assert.Throws<IOException>(() => storage.Write(stream =>
            {
                stream.Write(new byte[] { 9, 9, 9, 9 }, 0, 4);
                throw new IOException("disk full");
            }));

            Assert.That(File.ReadAllBytes(storage.FilePath), Is.EqualTo(new byte[] { 1, 2, 3 }), "the previous profile is intact");
            Assert.That(Directory.GetFiles(m_Folder), Is.EqualTo(new[] { storage.FilePath }), "no temporary file left");

            var fresh = new ProfileFileStorage(Path.Combine(m_Folder, "fresh"));
            Assert.Throws<IOException>(() => fresh.Write(stream => throw new IOException("disk full")));
            Assert.IsNull(fresh.OpenRead(), "a failed first write creates no profile");
        }

        [Test]
        public void ProfileFile_KeepCopy_NextToProfile()
        {
            var storage = new ProfileFileStorage(m_Folder);
            Assert.IsNull(storage.KeepCopy("unreadable"), "no profile — no copy");

            storage.Write(stream => stream.Write(new byte[] { 1, 2, 3 }, 0, 3));
            var first = storage.KeepCopy("unreadable");
            var second = storage.KeepCopy("unreadable");

            Assert.AreNotEqual(first, second, "copies don't overwrite each other");
            foreach (var copy in new[] { first, second })
            {
                Assert.AreEqual(m_Folder, Path.GetDirectoryName(copy));
                StringAssert.StartsWith("profile.unreadable-", Path.GetFileName(copy));
                Assert.That(File.ReadAllBytes(copy), Is.EqualTo(new byte[] { 1, 2, 3 }));
            }

            Assert.That(File.ReadAllBytes(storage.FilePath), Is.EqualTo(new byte[] { 1, 2, 3 }), "the profile itself is unchanged");
        }

        [Test]
        public void ProfileService_WithFile_SurvivesRestart()
        {
            using (CreateScope(new ProfileFileStorage(m_Folder), out var profile))
                profile.Variables.Set(Declaration("ending_seen"), true);

            var text = File.ReadAllText(Path.Combine(m_Folder, ProfileFileStorage.FileName), Encoding.UTF8);
            StringAssert.Contains("\"ending_seen\": true", text);

            using (CreateScope(new ProfileFileStorage(m_Folder), out var restarted))
                Assert.AreEqual(true, restarted.Variables.Get(Declaration("ending_seen")));
        }

        [Test]
        public void DefaultFolder_IsPlayerDataFolder()
        {
            Assert.AreEqual(Application.persistentDataPath, ProfileFileStorage.DefaultFolder);
        }
    }
}
