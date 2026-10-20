using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Saves;
using TheGrowth.Tests.EditMode.Scripting;

namespace TheGrowth.Tests.EditMode.Saves
{
    /// <summary><see cref="SaveService"/> на хранилище в памяти: ID ручных слотов, ротация автосейвов, список по meta, очередь операций.</summary>
    public sealed class SaveServiceTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 19, 10, 0, 0, DateTimeKind.Utc);

        private MemorySaveStorage m_Storage;
        private SaveService m_Service;
        private LifetimeScope m_Global;
        private DateTime m_Now;

        [SetUp]
        public void SetUp()
        {
            m_Now = Start;
            m_Global = new LifetimeScope("Global");
            m_Storage = m_Global.Container.Add(new MemorySaveStorage());
            m_Service = m_Global.Container.Add(new SaveService(() => m_Now));
            m_Global.Container.Init();
        }

        [TearDown]
        public void TearDown()
        {
            m_Global.Dispose();
        }

        private static SaveGame CreateSave(string location, int value = 0)
        {
            var save = new SaveGame(new SaveMeta { Location = location });
            save.AddSection(SaveSection.FromObject(CounterSystem.Key, CounterSystem.Version, new CounterSave { Value = value }));
            return save;
        }

        private SaveSlotInfo Save(SaveSlotKind kind, string location, string slotId = null)
        {
            return SaveTestKit.Sync(m_Service.SaveAsync(CreateSave(location), kind, slotId));
        }

        private SaveSlotInfo Autosave(string location, int minutes)
        {
            m_Now = Start.AddMinutes(minutes);
            return Save(SaveSlotKind.Auto, location);
        }

        [Test]
        public void Manual_NewSlots_UniqueIdsByTime_StampsMeta()
        {
            var first = Save(SaveSlotKind.Manual, "hub");
            var second = Save(SaveSlotKind.Manual, "hub");
            m_Now = Start.AddMinutes(5);
            var third = Save(SaveSlotKind.Manual, "expedition");

            Assert.AreEqual("manual_20261019_100000", first.SlotId);
            Assert.AreEqual("manual_20261019_100000_2", second.SlotId, "same second: a suffix");
            Assert.AreEqual("manual_20261019_100500", third.SlotId);
            Assert.AreEqual(SaveSlotKind.Manual, third.Meta.Kind);
            Assert.AreEqual(Start.AddMinutes(5), third.Meta.SavedAtUtc);
            Assert.AreEqual(3, m_Storage.Slots.Count);
        }

        [Test]
        public void Manual_Overwrite_ReplacesSlot()
        {
            var slot = Save(SaveSlotKind.Manual, "hub");
            m_Now = Start.AddMinutes(1);

            Save(SaveSlotKind.Manual, "expedition", slot.SlotId);

            Assert.AreEqual(1, m_Storage.Slots.Count);
            var loaded = SaveTestKit.Sync(m_Service.LoadAsync(slot.SlotId));
            Assert.AreEqual("expedition", loaded.Meta.Location);
            Assert.AreEqual(Start.AddMinutes(1), loaded.Meta.SavedAtUtc);
            Assert.Throws<ArgumentException>(() => Save(SaveSlotKind.Manual, "hub", "auto_1"), "autosave slots are not manual");
            Assert.Throws<ArgumentException>(() => Save(SaveSlotKind.Auto, "hub", "auto_1"), "autosave slot is chosen by rotation");
            Assert.Throws<ArgumentException>(() => Save(SaveSlotKind.Manual, "hub", "../x"));
        }

        [Test]
        public void Autosave_ThreeSlotsInCircle_OldestOverwritten_EmptyFirst()
        {
            Assert.AreEqual("auto_1", Autosave("a", 1).SlotId);
            Assert.AreEqual("auto_2", Autosave("b", 2).SlotId);
            Assert.AreEqual("auto_3", Autosave("c", 3).SlotId);
            Assert.AreEqual("auto_1", Autosave("d", 4).SlotId, "the oldest is overwritten");
            Assert.AreEqual("auto_2", Autosave("e", 5).SlotId);
            Assert.AreEqual(3, m_Storage.Slots.Count);

            SaveTestKit.Sync(m_Service.DeleteAsync("auto_1"));
            Assert.AreEqual("auto_1", Autosave("f", 6).SlotId, "an empty slot goes first");

            m_Storage.Slots["auto_2"] = new byte[] { 1, 2, 3 };
            Assert.AreEqual("auto_2", Autosave("g", 7).SlotId, "a broken slot goes first");

            var slots = SaveTestKit.Sync(m_Service.ListAsync());
            Assert.That(slots.Select(slot => slot.Meta.Location), Is.EqualTo(new[] { "g", "f", "c" }), "newest first");
            Assert.That(slots, Has.All.Matches<SaveSlotInfo>(slot => slot.Meta.Kind == SaveSlotKind.Auto));
        }

        [Test]
        public void List_NewestFirst_BrokenLast()
        {
            Save(SaveSlotKind.Manual, "first");
            m_Now = Start.AddMinutes(2);
            Save(SaveSlotKind.Auto, "auto");
            m_Now = Start.AddMinutes(1);
            Save(SaveSlotKind.Manual, "second");
            m_Storage.Slots["manual_broken"] = new byte[] { 1, 2, 3 };
            m_Storage.Slots["Not A Slot"] = new byte[] { 1 };

            var slots = SaveTestKit.Sync(m_Service.ListAsync());

            Assert.That(slots.Select(slot => slot.SlotId), Is.EqualTo(new[] { "auto_1", "manual_20261019_100100", "manual_20261019_100000", "manual_broken" }));
            Assert.IsTrue(slots[3].IsBroken);
            Assert.IsNotEmpty(slots[3].Error);
            Assert.AreEqual(SaveSlotKind.Auto, slots[0].Meta.Kind);
        }

        [Test]
        public void List_ReadsOnlyMeta_WithoutSections()
        {
            var noise = new byte[256 * 1024];
            new Random(7).NextBytes(noise);
            var save = CreateSave("hub");
            save.AddSection(new SaveSection("big", 1, new JObject { ["noise"] = Convert.ToBase64String(noise) }));
            var slot = SaveTestKit.Sync(m_Service.SaveAsync(save, SaveSlotKind.Manual));
            m_Storage.Slots["manual_bad_section"] = SaveTestKit.Zip(("meta.json", SaveTestKit.MetaV1), ("counter.json", "not json {"));
            m_Storage.Opened.Clear();

            var slots = SaveTestKit.Sync(m_Service.ListAsync());

            Assert.AreEqual(2, slots.Count);
            Assert.That(slots, Has.None.Matches<SaveSlotInfo>(info => info.IsBroken), "a broken section does not break the list");
            Assert.AreEqual(2, m_Storage.Opened.Count);
            var size = m_Storage.Slots[slot.SlotId].Length;
            Assert.That(m_Storage.Opened, Has.All.Matches<ReadTrackingStream>(stream => stream.BytesRead < Math.Max(4096, size / 4)));
            Assert.Throws<SaveException>(() => SaveTestKit.Sync(m_Service.LoadAsync("manual_bad_section")), "loading reads and checks the sections");
        }

        [Test]
        public void Load_MissingOrBroken_ThrowsSaveException()
        {
            m_Storage.Slots["manual_broken"] = new byte[] { 1, 2, 3 };

            Assert.Throws<SaveException>(() => SaveTestKit.Sync(m_Service.LoadAsync("manual_missing")));
            Assert.Throws<SaveException>(() => SaveTestKit.Sync(m_Service.LoadAsync("manual_broken")));
            Assert.Throws<SaveException>(() => SaveTestKit.Sync(m_Service.LoadAsync("../x")));
        }

        [Test]
        public void SaveLoadDelete_RoundTrip()
        {
            var slot = SaveTestKit.Sync(m_Service.SaveAsync(CreateSave("hub", 42), SaveSlotKind.Manual));

            var loaded = SaveTestKit.Sync(m_Service.LoadAsync(slot.SlotId));
            Assert.AreEqual(42, loaded.GetSection(CounterSystem.Key).Get<CounterSave>(CounterSystem.Version).Value);

            SaveTestKit.Sync(m_Service.DeleteAsync(slot.SlotId));
            Assert.IsEmpty(SaveTestKit.Sync(m_Service.ListAsync()));
        }

        [Test]
        public void Operations_RunOneAtATime()
        {
            m_Storage.WriteGate = new Gate();
            var save = Probe.Watch(m_Service.SaveAsync(CreateSave("hub"), SaveSlotKind.Auto).AsUniTask());
            var list = m_Service.ListAsync();
            var idle = Probe.Watch(m_Service.WaitIdleAsync());

            Assert.IsFalse(save.IsCompleted);
            Assert.IsFalse(list.Status.IsCompleted(), "the list waits for the write");
            Assert.IsFalse(idle.IsCompleted);

            m_Storage.WriteGate.Open();

            Assert.IsTrue(save.IsCompleted);
            Assert.IsNull(save.Exception);
            Assert.AreEqual(1, SaveTestKit.Sync(list).Count, "the list sees the finished write");
            Assert.IsTrue(idle.IsCompleted);
        }
    }
}
