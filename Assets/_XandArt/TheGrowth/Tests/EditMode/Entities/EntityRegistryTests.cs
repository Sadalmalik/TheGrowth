using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TheGrowth.Architecture.Events;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Saves;
using TheGrowth.Model;
using TheGrowth.Model.Entities;
using TheGrowth.Model.Slots;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Entities
{
    /// <summary>
    /// Реестр сущностей в сессии без сцены: создание из конфигов разных видов, компоненты, теги, переменные карты, модификаторы на
    /// владельце и их уничтожение, события, сейв и загрузка (ID, счётчик, переменные, состояния компонентов по ключу).
    /// </summary>
    public sealed class EntityRegistryTests
    {
        private EntityTestKit m_Kit;
        private Session m_Session;
        private EntityRegistry m_Entities;
        private SlotSystem m_Slots;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new EntityTestKit();
            Open(m_Kit.NewSession());
        }

        [TearDown]
        public void TearDown()
        {
            m_Session?.Dispose();
            m_Kit.Dispose();
        }

        private void Open(Session session)
        {
            m_Session?.Dispose();
            m_Session = session;
            m_Entities = session.Get<EntityRegistry>();
            m_Slots = session.Get<SlotSystem>();
        }

        private void Reload()
        {
            Open(m_Kit.Reload(m_Session));
        }

        private Entity Card(EntityModel model)
        {
            return m_Slots.Create(model, m_Slots.Inventory);
        }

#region Creation

        [Test]
        public void Create_KindsFromConfigs_IdsFromCounter_RequiredComponents()
        {
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);

            var hero = Card(m_Kit.Hero);
            var wall = m_Slots.Create(m_Kit.Wall, expedition.Field.GetLink("k1"));
            var poison = m_Entities.AddModifier(hero, m_Kit.Poison);

            Assert.AreEqual((1, 2, 3), (hero.InstanceId, wall.InstanceId, poison.InstanceId));
            Assert.AreEqual(4, m_Entities.NextInstanceId);
            Assert.AreEqual((EntityKind.Card, EntityKind.Mechanic, EntityKind.Modifier), (hero.Kind, wall.Kind, poison.Kind));
            Assert.AreSame(m_Kit.Hero, hero.Model);
            Assert.AreSame(hero, m_Entities.Get(1));
            Assert.IsTrue(m_Entities.TryGet(3, out var found) && found == poison);
            Assert.IsFalse(m_Entities.TryGet(4, out _));
            Assert.IsTrue(hero.GetRequired<CardBrain>().IsFaceUp, "the brain of a card, face up by its settings");
            Assert.AreEqual("card_brain", hero.GetRequired<CardBrain>().Key);
            Assert.IsNull(wall.Get<CardBrain>());
            Assert.Throws<InvalidOperationException>(() => wall.GetRequired<CardBrain>());
            Assert.IsFalse(hero.IsDestroyed);
        }

        [Test]
        public void Create_Components_FromSettings_OnCreated()
        {
            var counter = Card(m_Kit.Counter);

            Assert.That(counter.Components.Select(component => component.Key), Is.EqualTo(new[] { "card_brain", "test_counter" }));
            Assert.AreEqual(5, counter.GetRequired<CounterBehaviour>().Value, "OnCreated reads the settings");
            Assert.AreSame(counter, counter.Find("test_counter").Entity);
            Assert.AreSame(m_Kit.Counter.Components[1], counter.Find("test_counter").Settings);
        }

        [Test]
        public void Tags_FromConfig()
        {
            var monster = Card(m_Kit.Monster);

            Assert.That(monster.Tags, Is.EqualTo(new[] { "Monster" }));
            Assert.IsTrue(monster.HasTag("Monster"));
            Assert.IsFalse(monster.HasTag("Hero"));
        }

        [Test]
        public void Created_Events_AfterPlacement()
        {
            var events = m_Kit.Global.Get<EventBus>();
            var created = new List<string>();
            Action<EntityCreated> handler = signal => created.Add($"{signal.Entity.Model.Id}:{m_Slots.GetSlot(signal.Entity)?.Id ?? signal.Entity.Owner?.Model.Id}");
            var local = new List<Entity>();
            m_Entities.Created += signal => local.Add(signal.Entity);
            events.Subscribe(handler);
            try
            {
                var hero = Card(m_Kit.Hero);
                m_Entities.AddModifier(hero, m_Kit.Poison);
            }
            finally
            {
                events.Unsubscribe(handler);
            }

            Assert.That(created, Is.EqualTo(new[] { "Hero:hub/inventory", "Poison:Hero" }));
            Assert.That(local.Select(entity => entity.InstanceId), Is.EqualTo(new[] { 1, 2 }));
        }

#endregion

#region Variables

        [Test]
        public void Variables_ModelDefault_DeclarationDefault_ForMissing()
        {
            var monster = Card(m_Kit.Monster);
            var hero = Card(m_Kit.Hero);
            var charges = m_Kit.Declaration("charges");
            var mood = m_Kit.Declaration("mood");

            Assert.AreEqual(3, monster.Variables.Get(charges), "default of the model");
            Assert.AreEqual("calm", monster.Variables.Get(mood), "on the model without a value: default of the declaration");
            Assert.AreEqual(2, hero.Variables.Get(charges), "not on the model: default of the declaration");
            Assert.IsTrue(monster.Variables.Contains(charges));
            Assert.IsFalse(hero.Variables.Contains(charges));
            Assert.AreSame(monster, monster.Variables.Owner);

            monster.Variables.Set(charges, 7);
            Assert.AreEqual(7, monster.Variables.Get(charges));
            var error = Assert.Throws<InvalidOperationException>(() => hero.Variables.Set(charges, 5));
            StringAssert.Contains("'charges' is not a variable of EntityModel 'Hero'", error.Message);
            Assert.AreEqual(2, hero.Variables.Get(charges));
            Assert.Throws<InvalidOperationException>(() => monster.Variables.Set(m_Kit.Declaration("coins"), 1), "not a card variable");
        }

        [Test]
        public void Variables_SavedWithEntity()
        {
            var monster = Card(m_Kit.Monster);
            monster.Variables.Set(m_Kit.Declaration("charges"), 9);
            monster.Variables.Set(m_Kit.Declaration("mood"), "angry");

            Reload();

            var loaded = m_Entities.Get(monster.InstanceId);
            Assert.AreEqual(9, loaded.Variables.Get(m_Kit.Declaration("charges")));
            Assert.AreEqual("angry", loaded.Variables.Get(m_Kit.Declaration("mood")));
        }

        [Test]
        public void Variables_InSaveNotOnModel_Ignored()
        {
            var save = new SaveGame();
            var hero = new EntitySave { Id = 1, Model = "Hero", Variables = new Dictionary<string, JToken> { ["charges"] = 4 } };
            save.AddSection(SaveSection.FromObject(EntityRegistry.Key, 1, new EntitiesSave { NextId = 2, Entities = { hero } }));
            save.AddSection(SaveSection.FromObject(SlotSystem.Key, 1, new SlotsSave { Slots = { new SlotSave { Id = SlotIds.Inventory, Entities = { 1 } } } }));

            LogAssert.Expect(LogType.Warning, new Regex("variable 'charges' is not a variable of EntityModel 'Hero' any more"));
            Open(m_Kit.Load(save));

            Assert.AreEqual(2, m_Entities.Get(1).Variables.Get(m_Kit.Declaration("charges")));
        }

#endregion

#region Modifiers

        [Test]
        public void Modifiers_OnOwner_Order_Rules()
        {
            var monster = Card(m_Kit.Monster);
            var poison = m_Entities.AddModifier(monster, m_Kit.Poison);
            var burn = m_Entities.AddModifier(monster, m_Kit.Burn, 0);

            Assert.That(monster.Modifiers, Is.EqualTo(new[] { burn, poison }));
            Assert.AreSame(monster, poison.Owner);
            Assert.IsNull(monster.Owner);
            Assert.IsNull(m_Slots.GetSlot(poison), "modifiers are not in slots");
            Assert.IsEmpty(poison.Modifiers);

            var count = m_Entities.Count;
            Assert.Throws<InvalidOperationException>(() => m_Entities.AddModifier(monster, m_Kit.Hero), "not a modifier config");
            Assert.Throws<InvalidOperationException>(() => m_Entities.AddModifier(poison, m_Kit.Burn), "no modifiers on modifiers");
            Assert.Throws<ArgumentOutOfRangeException>(() => m_Entities.AddModifier(monster, m_Kit.Burn, 3));
            Assert.AreEqual(count, m_Entities.Count, "failed calls create nothing");
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Modifiers_OnMechanic()
        {
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            var wall = m_Slots.Create(m_Kit.Wall, expedition.Field.GetLink("k2"));

            var burn = m_Entities.AddModifier(wall, m_Kit.Burn);

            Assert.AreSame(wall, burn.Owner);
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Destroy_Owner_DestroysModifiersFirst()
        {
            var events = m_Kit.Global.Get<EventBus>();
            var journal = new List<string>();
            Action<EntityDestroyed> handler = signal => journal.Add($"{signal.Entity.Model.Id}<{signal.Owner?.Model.Id}>");
            var monster = Card(m_Kit.Monster);
            var poison = m_Entities.AddModifier(monster, m_Kit.Poison);
            var burn = m_Entities.AddModifier(monster, m_Kit.Burn);
            var hero = Card(m_Kit.Hero);
            events.Subscribe(handler);
            try
            {
                m_Entities.Destroy(monster);
            }
            finally
            {
                events.Unsubscribe(handler);
            }

            Assert.That(journal, Is.EqualTo(new[] { "Burn<Monster>", "Poison<Monster>", "Monster<>" }));
            Assert.IsTrue(monster.IsDestroyed && poison.IsDestroyed && burn.IsDestroyed);
            Assert.IsNull(poison.Owner);
            Assert.IsEmpty(monster.Modifiers);
            Assert.That(m_Slots.Inventory.Entities, Is.EqualTo(new[] { hero }), "removed from its slot");
            Assert.IsNull(m_Slots.GetSlot(monster));
            Assert.AreEqual(1, m_Entities.Count);
            Assert.Throws<InvalidOperationException>(() => m_Entities.Destroy(monster), "already destroyed");
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Destroy_Modifier_RemovedFromOwner()
        {
            var monster = Card(m_Kit.Monster);
            var poison = m_Entities.AddModifier(monster, m_Kit.Poison);
            var burn = m_Entities.AddModifier(monster, m_Kit.Burn);

            m_Entities.Destroy(poison);

            Assert.That(monster.Modifiers, Is.EqualTo(new[] { burn }));
            Assert.IsFalse(monster.IsDestroyed);
            EntityTestKit.AssertPlacement(m_Session);
        }

#endregion

#region Save

        [Test]
        public void Save_ComponentStatesByStableKey_NoTypeNames()
        {
            var counter = Card(m_Kit.Counter);
            counter.GetRequired<CounterBehaviour>().Value = 9;
            counter.GetRequired<CardBrain>().SetFaceUp(false);
            m_Entities.AddModifier(counter, m_Kit.Poison);

            var section = m_Session.Capture().GetSection(EntityRegistry.Key);
            var entity = (JObject)section.Data["entities"][0];

            Assert.AreEqual(1, section.Version);
            Assert.AreEqual("Counter", (string)entity["model"]);
            Assert.IsTrue(JToken.DeepEquals(JObject.Parse("{\"card_brain\": {\"faceUp\": false}, \"test_counter\": {\"value\": 9}}"), entity["components"]), entity.ToString());
            Assert.AreEqual("Poison", (string)entity["modifiers"][0]["model"]);
            var text = section.Data.ToString();
            StringAssert.DoesNotContain("CardBrain", text);
            StringAssert.DoesNotContain("CounterBehaviour", text);
            StringAssert.DoesNotContain("$type", text);

            Reload();

            var loaded = m_Entities.Get(counter.InstanceId);
            Assert.AreEqual(9, loaded.GetRequired<CounterBehaviour>().Value);
            Assert.IsFalse(loaded.GetRequired<CardBrain>().IsFaceUp);
            Assert.AreEqual("Poison", loaded.Modifiers.Single().Model.Id);
        }

        [Test]
        public void Load_RestoresIdsAndCounter_NewIdsContinue()
        {
            var hero = Card(m_Kit.Hero);
            var monster = Card(m_Kit.Monster);
            var poison = m_Entities.AddModifier(monster, m_Kit.Poison);
            m_Entities.Destroy(hero);

            Reload();

            Assert.AreEqual(2, m_Entities.Count);
            Assert.AreEqual(m_Kit.Monster, m_Entities.Get(monster.InstanceId).Model);
            Assert.AreSame(m_Entities.Get(monster.InstanceId), m_Entities.Get(poison.InstanceId).Owner);
            Assert.IsFalse(m_Entities.TryGet(hero.InstanceId, out _));
            Assert.AreEqual(4, Card(m_Kit.Herb).InstanceId, "the counter is saved: ids are not reused");
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Load_UnknownModelOrKind_EntityNotRestored()
        {
            var save = new SaveGame();
            save.AddSection(SaveSection.FromObject(EntityRegistry.Key, 1, new EntitiesSave
            {
                NextId = 2,
                Entities =
                {
                    new EntitySave { Id = 1, Model = "Monster", Modifiers = new List<EntitySave> { new EntitySave { Id = 5, Model = "Hero" } } },
                    new EntitySave { Id = 7, Model = "Removed" },
                    new EntitySave { Id = 8, Model = "Poison" },
                },
            }));
            save.AddSection(SaveSection.FromObject(SlotSystem.Key, 1, new SlotsSave { Slots = { new SlotSave { Id = SlotIds.Inventory, Entities = { 1, 7, 8 } } } }));

            LogAssert.Expect(LogType.Warning, new Regex("modifier 5 of card 1 'Monster': config 'Hero' is now a card"));
            LogAssert.Expect(LogType.Warning, new Regex("config 'Removed' of entity 7 is not in the content"));
            LogAssert.Expect(LogType.Warning, new Regex("entity 8: config 'Poison' is now a modifier"));
            LogAssert.Expect(LogType.Warning, new Regex("entity 7 in slot 'hub/inventory' is not restored"));
            LogAssert.Expect(LogType.Warning, new Regex("entity 8 in slot 'hub/inventory' is not restored"));
            Open(m_Kit.Load(save));

            Assert.AreEqual(1, m_Entities.Count);
            Assert.IsEmpty(m_Entities.Get(1).Modifiers);
            Assert.AreEqual(9, m_Entities.NextInstanceId, "ids of skipped entities are not reused");
        }

        [Test]
        public void Load_UnknownComponentState_Ignored()
        {
            var save = new SaveGame();
            var hero = new EntitySave { Id = 1, Model = "Hero", Components = new Dictionary<string, JToken> { ["card_brain"] = JObject.Parse("{\"faceUp\": false}"), ["removed"] = new JObject() } };
            save.AddSection(SaveSection.FromObject(EntityRegistry.Key, 1, new EntitiesSave { NextId = 2, Entities = { hero } }));
            save.AddSection(SaveSection.FromObject(SlotSystem.Key, 1, new SlotsSave { Slots = { new SlotSave { Id = SlotIds.Inventory, Entities = { 1 } } } }));

            LogAssert.Expect(LogType.Warning, new Regex("component 'removed' has no saved state on the entity any more"));
            Open(m_Kit.Load(save));

            Assert.IsFalse(m_Entities.Get(1).GetRequired<CardBrain>().IsFaceUp);
        }

#endregion
    }
}
