using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
using TheGrowth.Model;
using TheGrowth.Model.Locations;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Locations;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Saves
{
    /// <summary>Сессия из сейва: порядок Read по LoadOrder, значения по умолчанию, ISessionReady после Read, снимок секций.</summary>
    public sealed class SessionSaveTests
    {
        private ContentTestKit m_Kit;
        private LocationModel m_Hub;
        private LifetimeScope m_Global;
        private List<string> m_Journal;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Hub = LocationTestKit.Location(m_Kit, "hub", "HubScene");
            m_Global = m_Kit.Global(m_Kit.Registry(new ContentAsset[] { m_Hub }));
            m_Journal = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            m_Global.Dispose();
            m_Kit.Dispose();
        }

        private static SaveGame CreateSave(params (string Key, string Value)[] sections)
        {
            var save = new SaveGame();
            foreach (var (key, value) in sections)
                save.AddSection(SaveSection.FromObject(key, 1, new ProbeSave { Value = value }));

            return save;
        }

        [Test]
        public void Create_ReadsByLoadOrder_DefaultsWithoutSection_ReadyAfterAllReads()
        {
            var save = CreateSave(("late", "saved-late"), ("early", "saved-early"));

            var session = Session.Create(m_Global, save, created =>
            {
                created.Container.Add(new FirstProbe("late", 10, m_Journal));
                created.Container.Add(new SecondProbe("early", -5, m_Journal));
                created.Container.Add(new ThirdProbe("middle", 0, m_Journal));
            });

            Assert.That(m_Journal, Is.EqualTo(new[]
            {
                "late:init", "early:init", "middle:init",
                "early:read:saved-early", "middle:read:default", "late:read:saved-late",
                "late:ready", "early:ready", "middle:ready",
            }));
            Assert.IsNull(session.Get<LocationSystem>().Current, "no section: defaults");
            session.Dispose();
        }

        [Test]
        public void Capture_CollectsAllSaveables_NewSessionRestoresState()
        {
            var first = Session.Create(m_Global, new SaveGame(), created =>
            {
                created.Container.Add(new FirstProbe("first", 0, m_Journal));
                created.Container.Add(new CounterSystem());
            });
            first.Container.Get<FirstProbe>().Value = "changed";
            first.Container.Get<CounterSystem>().Value = 9;
            first.Get<LocationSystem>().Enter(m_Hub);

            var captured = first.Capture();
            first.Dispose();

            // first, counter и системы сессии: locations, variables.
            Assert.That(captured.Sections, Has.Count.EqualTo(4));
            Assert.AreEqual(CounterSystem.Version, captured.GetSection(CounterSystem.Key).Version);
            Assert.AreEqual("hub", (string)captured.GetSection(LocationSystem.Key).Data["current"]);

            var counter = new CounterSystem();
            var probe = new FirstProbe("first", 0, m_Journal);
            var second = Session.Create(m_Global, SaveTestKit.Unpack(SaveTestKit.Pack(captured)), created =>
            {
                created.Container.Add(probe);
                created.Container.Add(counter);
            });

            Assert.AreEqual("changed", probe.Value);
            Assert.AreEqual(9, counter.Value);
            Assert.IsEmpty(counter.Steps, "current version: no migrations");
            Assert.AreSame(m_Hub, second.Get<LocationSystem>().Current);
            second.Dispose();
        }

        [Test]
        public void Create_EmptySave_ReadDefaults()
        {
            var counter = new CounterSystem();

            var session = Session.Create(m_Global, new SaveGame(), created => created.Container.Add(counter));

            Assert.IsTrue(counter.ReadDefaultsCalled);
            session.Dispose();
        }

        [Test]
        public void Create_DuplicateSaveKey_Throws_DisposesSession()
        {
            Assert.Throws<InvalidOperationException>(() => Session.Create(m_Global, new SaveGame(), created =>
            {
                created.Container.Add(new FirstProbe("same", 0, m_Journal));
                created.Container.Add(new SecondProbe("same", 1, m_Journal));
            }));
            Assert.IsEmpty(m_Global.Children);
        }

        [Test]
        public void Create_SectionWithoutSystem_IgnoredWithWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Section 'removed_system' has no system"));

            var session = Session.Create(m_Global, CreateSave(("removed_system", "x")));

            session.Dispose();
        }

        [Test]
        public void LocationSystem_LocationMissingInContent_CurrentNull()
        {
            var save = new SaveGame();
            save.AddSection(SaveSection.FromObject(LocationSystem.Key, 1, new LocationSave { Current = "removed_location" }));

            LogAssert.Expect(LogType.Warning, new Regex("Location 'removed_location' from the save is not in the content"));
            var session = Session.Create(m_Global, save);

            Assert.IsNull(session.Get<LocationSystem>().Current);
            session.Dispose();
        }
    }
}
