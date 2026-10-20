using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
using TheGrowth.Model;
using TheGrowth.Model.Locations;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Locations;

namespace TheGrowth.Tests.EditMode.Saves
{
    /// <summary>Миграции секций: цепочка при чтении, фикстуры старых сейвов, версия новее кода, нет шага.</summary>
    public sealed class SaveMigrationTests
    {
        private ContentTestKit m_Kit;
        private LifetimeScope m_Global;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Global = m_Kit.Global(m_Kit.Registry(new ContentAsset[] { LocationTestKit.Location(m_Kit, "hub", "HubScene") }));
        }

        [TearDown]
        public void TearDown()
        {
            m_Global.Dispose();
            m_Kit.Dispose();
        }

        [Test]
        public void Apply_RunsStepsInOrder_OnCopy()
        {
            var steps = new List<int>();
            var migrations = new SaveMigrations("cards")
                .Add(2, data => { steps.Add(2); data["b"] = 2; return data; })
                .Add(1, data => { steps.Add(1); data["a"] = 1; return data; })
                .Add(3, data => { steps.Add(3); return new JObject { ["wrapped"] = data }; });
            var source = new JObject { ["x"] = 0 };

            var result = migrations.Apply(source, 1, 4);

            Assert.That(steps, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.IsTrue(JToken.DeepEquals(JObject.Parse("{ \"wrapped\": { \"x\": 0, \"a\": 1, \"b\": 2 } }"), result));
            Assert.IsTrue(JToken.DeepEquals(new JObject { ["x"] = 0 }, source), "the source is not changed");
            Assert.IsTrue(JToken.DeepEquals(source, migrations.Apply(source, 2, 2)), "same version: no steps");
        }

        [Test]
        public void Apply_MissingStep_Throws()
        {
            var migrations = new SaveMigrations("cards").Add(2, data => data);

            var exception = Assert.Throws<SaveException>(() => migrations.Apply(new JObject(), 1, 3));
            StringAssert.Contains("no migration from version 1 to 2", exception.Message);
            Assert.Throws<ArgumentException>(() => migrations.Add(2, data => data), "a step is registered once");
        }

        [Test]
        public void Get_SectionNewerThanCode_Throws()
        {
            var section = new SaveSection(CounterSystem.Key, CounterSystem.Version + 1, new JObject { ["value"] = 1 });

            var exception = Assert.Throws<SaveException>(() => section.Get<CounterSave>(CounterSystem.Version));
            StringAssert.Contains("newer game version", exception.Message);
        }

        [Test]
        public void Get_OldVersionWithoutMigrations_Throws()
        {
            var section = new SaveSection(CounterSystem.Key, 1, new JObject { ["count"] = 1 });

            Assert.Throws<SaveException>(() => section.Get<CounterSave>(CounterSystem.Version));
        }

        [Test]
        public void OldSaveFixture_MigratedOnSessionRead_SaveNotChanged()
        {
            var save = SaveTestKit.Unpack(SaveTestKit.Zip(("meta.json", SaveTestKit.MetaV1), ("counter.json", SaveTestKit.CounterV1)));
            var counter = new CounterSystem();

            var session = Session.Create(m_Global, save, created => created.Container.Add(counter));

            Assert.That(counter.Steps, Is.EqualTo(new[] { "1->2", "2->3" }));
            Assert.AreEqual(5, counter.Value, "count renamed to value");
            Assert.That(counter.Tags, Is.EqualTo(new[] { "migrated" }));
            Assert.AreEqual(1, save.GetSection(CounterSystem.Key).Version);
            Assert.AreEqual(5, (int)save.GetSection(CounterSystem.Key).Data["count"], "migrations work on a copy");

            var written = session.Capture().GetSection(CounterSystem.Key);
            Assert.AreEqual(CounterSystem.Version, written.Version, "the next save is written in the current version");
            Assert.AreEqual(5, (int)written.Data["value"]);
            session.Dispose();
        }

        [Test]
        public void OldSaveFixture_Locations_ReadByLocationSystem()
        {
            var save = SaveTestKit.Unpack(SaveTestKit.Zip(("meta.json", SaveTestKit.MetaV1), ("locations.json", SaveTestKit.LocationsV1)));

            var session = Session.Create(m_Global, save);

            Assert.AreEqual("hub", session.Get<LocationSystem>().Current.Id);
            session.Dispose();
        }

        [Test]
        public void SessionCreate_SectionNewerThanCode_Throws_DisposesSession()
        {
            var save = new SaveGame();
            save.AddSection(new SaveSection(CounterSystem.Key, CounterSystem.Version + 1, new JObject { ["value"] = 1 }));

            var exception = Assert.Throws<SaveException>(() => Session.Create(m_Global, save, created => created.Container.Add(new CounterSystem())));
            StringAssert.Contains("newer game version", exception.Message);
            Assert.IsEmpty(m_Global.Children);
        }
    }
}
