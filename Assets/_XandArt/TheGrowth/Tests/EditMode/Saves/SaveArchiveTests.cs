using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TheGrowth.Core.Saves;

namespace TheGrowth.Tests.EditMode.Saves
{
    /// <summary>Упаковка слота в zip: формат записей, чтение только meta, ошибки формата.</summary>
    public sealed class SaveArchiveTests
    {
        private static SaveGame CreateSave()
        {
            var save = new SaveGame(new SaveMeta
            {
                GameVersion = "1.2.3",
                SavedAtUtc = new DateTime(2026, 10, 19, 12, 30, 15, DateTimeKind.Utc),
                PlayTimeSeconds = 3725.5,
                StoryStep = "intro",
                Location = "hub",
                Kind = SaveSlotKind.Auto,
            });
            save.AddSection(SaveSection.FromObject(CounterSystem.Key, CounterSystem.Version, new CounterSave { Value = 7, Tags = { "a", "b" } }));
            save.AddSection(new SaveSection("raw", 2, JObject.Parse("{ \"nested\": { \"Key_1\": 1 } }")));
            save.Thumbnail = new byte[] { 137, 80, 78, 71, 1, 2, 3 };
            return save;
        }

        [Test]
        public void WriteRead_RoundTrip_MetaSectionsThumbnail()
        {
            var bytes = SaveTestKit.Pack(CreateSave());
            var read = SaveTestKit.Unpack(bytes);

            var meta = read.Meta;
            Assert.AreEqual(SaveArchive.FormatVersion, meta.FormatVersion);
            Assert.AreEqual("1.2.3", meta.GameVersion);
            Assert.AreEqual(new DateTime(2026, 10, 19, 12, 30, 15, DateTimeKind.Utc), meta.SavedAtUtc);
            Assert.AreEqual(DateTimeKind.Utc, meta.SavedAtUtc.Kind);
            Assert.AreEqual(3725.5, meta.PlayTimeSeconds);
            Assert.AreEqual("intro", meta.StoryStep);
            Assert.AreEqual("hub", meta.Location);
            Assert.AreEqual(SaveSlotKind.Auto, meta.Kind);

            Assert.AreEqual(2, read.Sections.Count);
            var counter = read.GetSection(CounterSystem.Key);
            Assert.AreEqual(CounterSystem.Version, counter.Version);
            var counterSave = counter.Get<CounterSave>(CounterSystem.Version);
            Assert.AreEqual(7, counterSave.Value);
            Assert.That(counterSave.Tags, Is.EqualTo(new[] { "a", "b" }), "lists are replaced, not appended to defaults");
            Assert.IsTrue(JToken.DeepEquals(JObject.Parse("{ \"nested\": { \"Key_1\": 1 } }"), read.GetSection("raw").Data), "dictionary keys keep their case");
            Assert.That(read.Thumbnail, Is.EqualTo(new byte[] { 137, 80, 78, 71, 1, 2, 3 }));
        }

        [Test]
        public void Write_EntryPerSection_ReadableJsonWithoutTypeNames()
        {
            var bytes = SaveTestKit.Pack(CreateSave());

            using (var archive = new ZipArchive(new MemoryStream(bytes, false), ZipArchiveMode.Read))
                Assert.That(archive.Entries.Select(entry => entry.FullName), Is.EquivalentTo(new[] { "meta.json", "counter.json", "raw.json", "thumbnail.png" }));

            var meta = SaveTestKit.ReadEntry(bytes, "meta.json");
            StringAssert.Contains("\"savedAtUtc\": \"2026-10-19T12:30:15Z\"", meta);
            StringAssert.Contains("\"kind\": \"auto\"", meta);
            StringAssert.DoesNotContain("$type", meta);

            var counter = JObject.Parse(SaveTestKit.ReadEntry(bytes, "counter.json"));
            Assert.AreEqual(CounterSystem.Version, (int)counter["version"]);
            Assert.AreEqual(7, (int)counter["data"]["value"]);
        }

        [Test]
        public void ReadMeta_ReadsOnlyCentralDirectoryAndMeta()
        {
            var save = CreateSave();
            var noise = new byte[512 * 1024];
            new Random(42).NextBytes(noise);
            save.AddSection(new SaveSection("big", 1, new JObject { ["noise"] = Convert.ToBase64String(noise) }));
            var bytes = SaveTestKit.Pack(save);

            var metaOnly = new ReadTrackingStream(new MemoryStream(bytes, false));
            var meta = SaveArchive.ReadMeta(metaOnly);

            Assert.AreEqual("hub", meta.Location);
            Assert.Less(metaOnly.BytesRead, bytes.Length / 20, $"read {metaOnly.BytesRead} of {bytes.Length} bytes");

            var full = new ReadTrackingStream(new MemoryStream(bytes, false));
            SaveArchive.Read(full);
            Assert.Greater(full.BytesRead, bytes.Length / 2, "the full read reads the sections");
        }

        [Test]
        public void ReadMeta_DoesNotParseSections()
        {
            var bytes = SaveTestKit.Zip(("meta.json", SaveTestKit.MetaV1), ("counter.json", "not json {"));

            using (var stream = new MemoryStream(bytes, false))
                Assert.AreEqual("hub", SaveArchive.ReadMeta(stream).Location);

            using (var stream = new MemoryStream(bytes, false))
                Assert.Throws<SaveException>(() => SaveArchive.Read(stream));
        }

        [Test]
        public void ReadOldArchive_MetaAndSectionsAsWrittenByOldVersion()
        {
            var bytes = SaveTestKit.Zip(("meta.json", SaveTestKit.MetaV1), ("counter.json", SaveTestKit.CounterV1), ("unknown.bin", "skipped"));

            var save = SaveTestKit.Unpack(bytes);

            Assert.AreEqual("0.9.0", save.Meta.GameVersion);
            Assert.AreEqual(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), save.Meta.SavedAtUtc);
            Assert.AreEqual(SaveSlotKind.Manual, save.Meta.Kind);
            Assert.AreEqual(1, save.GetSection(CounterSystem.Key).Version);
            Assert.AreEqual(1, save.Sections.Count, "unknown entries are skipped");
        }

        [Test]
        public void Read_Broken_ThrowsSaveException()
        {
            Assert.Throws<SaveException>(() => SaveTestKit.Unpack(new byte[] { 1, 2, 3, 4, 5 }), "not a zip");
            Assert.Throws<SaveException>(() => SaveTestKit.Unpack(SaveTestKit.Zip(("counter.json", SaveTestKit.CounterV1))), "no meta");
            Assert.Throws<SaveException>(() => SaveTestKit.Unpack(SaveTestKit.Zip(("meta.json", "{ broken"))), "broken meta");
            Assert.Throws<SaveException>(() => SaveTestKit.Unpack(SaveTestKit.Zip(("meta.json", SaveTestKit.MetaV1), ("counter.json", "{ \"data\": {} }"))), "no section version");
            Assert.Throws<SaveException>(() => SaveTestKit.Unpack(SaveTestKit.Zip(("meta.json", SaveTestKit.MetaV1), ("Bad Key.json", SaveTestKit.CounterV1))), "invalid section key");

            var bytes = SaveTestKit.Pack(CreateSave());
            Array.Resize(ref bytes, bytes.Length / 2);
            Assert.Throws<SaveException>(() => SaveTestKit.Unpack(bytes), "truncated");
        }

        [Test]
        public void Read_NewerFormat_ThrowsSaveException()
        {
            var meta = SaveTestKit.MetaV1.Replace("\"formatVersion\": 1", "\"formatVersion\": 99");

            var exception = Assert.Throws<SaveException>(() => SaveTestKit.Unpack(SaveTestKit.Zip(("meta.json", meta))));
            StringAssert.Contains("newer game version", exception.Message);
        }

        [Test]
        public void SectionKey_Validated()
        {
            Assert.IsTrue(SaveSection.IsValidKey("cards"));
            Assert.IsTrue(SaveSection.IsValidKey("craft_2"));
            Assert.IsFalse(SaveSection.IsValidKey("meta"));
            Assert.IsFalse(SaveSection.IsValidKey("Cards"));
            Assert.IsFalse(SaveSection.IsValidKey("../cards"));
            Assert.IsFalse(SaveSection.IsValidKey(""));
            Assert.Throws<ArgumentException>(() => new SaveSection("meta", 1, new JObject()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SaveSection("cards", 0, new JObject()));
            Assert.Throws<SaveException>(() => SaveSection.FromObject("cards", 1, null));
        }

        [Test]
        public void FromObject_SnapshotsImmediately()
        {
            var dto = new CounterSave { Value = 1 };
            var section = SaveSection.FromObject(CounterSystem.Key, CounterSystem.Version, dto);

            dto.Value = 2;
            dto.Tags.Add("late");

            var read = section.Get<CounterSave>(CounterSystem.Version);
            Assert.AreEqual(1, read.Value);
            Assert.IsEmpty(read.Tags);
        }
    }
}
