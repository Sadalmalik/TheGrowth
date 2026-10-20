using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Saves;
using TheGrowth.Integrations.SaveFiles;

namespace TheGrowth.Tests.EditMode.Saves
{
    /// <summary>Файловое хранилище во временной папке: файлы слотов, атомарная запись, связка с <see cref="SaveService"/>.</summary>
    public sealed class SaveFileStorageTests
    {
        private string m_Folder;
        private SaveFileStorage m_Storage;

        [SetUp]
        public void SetUp()
        {
            m_Folder = Path.Combine(Path.GetTempPath(), "TheGrowthSaveTests_" + Guid.NewGuid().ToString("N"));
            m_Storage = new SaveFileStorage(m_Folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_Folder))
                Directory.Delete(m_Folder, true);
        }

        private void Write(string slotId, byte[] bytes)
        {
            SaveTestKit.Sync(m_Storage.WriteAsync(slotId, stream => stream.Write(bytes, 0, bytes.Length), CancellationToken.None));
        }

        private byte[] Read(string slotId)
        {
            using (var stream = SaveTestKit.Sync(m_Storage.OpenReadAsync(slotId, CancellationToken.None)))
            {
                if (stream == null)
                    return null;

                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    return buffer.ToArray();
                }
            }
        }

        [Test]
        public void WriteListReadDelete_SlotFiles()
        {
            Assert.IsEmpty(SaveTestKit.Sync(m_Storage.ListAsync(CancellationToken.None)), "no folder yet");
            Assert.IsNull(Read("slot_a"));

            Write("slot_a", new byte[] { 1, 2, 3 });
            Write("slot_b", new byte[] { 4 });
            Write("slot_a", new byte[] { 5, 6 });

            Assert.IsTrue(File.Exists(Path.Combine(m_Folder, "slot_a" + SaveFileStorage.Extension)));
            Assert.That(SaveTestKit.Sync(m_Storage.ListAsync(CancellationToken.None)), Is.EquivalentTo(new[] { "slot_a", "slot_b" }));
            Assert.That(Read("slot_a"), Is.EqualTo(new byte[] { 5, 6 }), "overwritten");

            SaveTestKit.Sync(m_Storage.DeleteAsync("slot_a", CancellationToken.None));
            SaveTestKit.Sync(m_Storage.DeleteAsync("missing", CancellationToken.None));

            Assert.IsNull(Read("slot_a"));
            Assert.That(SaveTestKit.Sync(m_Storage.ListAsync(CancellationToken.None)), Is.EqualTo(new[] { "slot_b" }));
        }

        [Test]
        public void FailedWrite_KeepsPreviousFile_NoTemporaryLeft()
        {
            Write("slot", new byte[] { 1, 2, 3 });

            Assert.Throws<IOException>(() => m_Storage.WriteAsync("slot", stream =>
            {
                stream.Write(new byte[] { 9, 9, 9, 9, 9 }, 0, 5);
                throw new IOException("disk full");
            }, CancellationToken.None));

            Assert.That(Read("slot"), Is.EqualTo(new byte[] { 1, 2, 3 }), "the previous file is intact");
            Assert.That(Directory.GetFiles(m_Folder), Is.EqualTo(new[] { Path.Combine(m_Folder, "slot" + SaveFileStorage.Extension) }), "no temporary file left");

            Assert.Throws<IOException>(() => m_Storage.WriteAsync("new_slot", stream => throw new IOException("disk full"), CancellationToken.None));
            Assert.IsNull(Read("new_slot"), "a failed first write creates no slot");
        }

        [Test]
        public void SaveService_WithFiles_FailedWriteKeepsLoadableSlot()
        {
            var global = new LifetimeScope("Global");
            global.Container.Add(m_Storage);
            var service = global.Container.Add(new SaveService());
            global.Container.Init();
            try
            {
                var save = new SaveGame(new SaveMeta { Location = "hub" });
                save.AddSection(SaveSection.FromObject(CounterSystem.Key, CounterSystem.Version, new CounterSave { Value = 3 }));
                var slot = SaveTestKit.Sync(service.SaveAsync(save, SaveSlotKind.Manual));

                Assert.Throws<IOException>(() => m_Storage.WriteAsync(slot.SlotId, stream =>
                {
                    stream.WriteByte(0);
                    throw new IOException("disk full");
                }, CancellationToken.None));

                var slots = SaveTestKit.Sync(service.ListAsync());
                Assert.AreEqual(1, slots.Count);
                Assert.IsFalse(slots[0].IsBroken);
                var loaded = SaveTestKit.Sync(service.LoadAsync(slot.SlotId));
                Assert.AreEqual(3, loaded.GetSection(CounterSystem.Key).Get<CounterSave>(CounterSystem.Version).Value);
            }
            finally
            {
                global.Dispose();
            }
        }
    }
}
