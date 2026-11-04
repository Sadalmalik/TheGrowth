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
using TheGrowth.Tests.EditMode.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Slots
{
    /// <summary>
    /// Система слотов без сцены: инвентарь, создание в слоте, атомарное перемещение (правила, ёмкость, индекс, события,
    /// переворот), инвариант «сущность ровно в одном слоте» (серия перемещений, загрузка), поле из графа (ID, смежность),
    /// слоты экспедиции (создание, уничтожение, восстановление при загрузке), сейв и ошибки сейва.
    /// </summary>
    public sealed class SlotSystemTests
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

        private Entity Create(EntityModel model, Slot slot = null, int index = -1)
        {
            return m_Slots.Create(model, slot ?? m_Slots.Inventory, index);
        }

        private static bool IsFaceUp(Entity entity)
        {
            return entity.GetRequired<CardBrain>().IsFaceUp;
        }

        /// <summary>События перемещения: C# системы и шина, до <see cref="IDisposable.Dispose"/>.</summary>
        private IDisposable Record(List<EntityMoved> local, List<EntityMoved> bus)
        {
            var events = m_Kit.Global.Get<EventBus>();
            Action<EntityMoved> localHandler = local.Add;
            Action<EntityMoved> busHandler = bus.Add;
            m_Slots.Moved += localHandler;
            events.Subscribe(busHandler);
            return new Unsubscribe(() =>
            {
                m_Slots.Moved -= localHandler;
                events.Unsubscribe(busHandler);
            });
        }

        private sealed class Unsubscribe : IDisposable
        {
            private readonly Action m_Action;

            public Unsubscribe(Action action)
            {
                m_Action = action;
            }

            public void Dispose()
            {
                m_Action();
            }
        }

#region Inventory and creation

        [Test]
        public void NewGame_OnlyInventory()
        {
            Assert.That(m_Slots.Slots.Select(slot => slot.Id), Is.EqualTo(new[] { SlotIds.Inventory }));
            Assert.AreSame(m_Slots.Inventory, m_Slots.Get("hub/inventory"));
            Assert.IsNull(m_Slots.Expedition);
            Assert.IsFalse(m_Slots.TryGet(SlotIds.Deck, out _));
            Assert.Throws<KeyNotFoundException>(() => m_Slots.Get(SlotIds.Deck));
        }

        [Test]
        public void Create_InSlot_AtIndex()
        {
            var hero = Create(m_Kit.Hero);
            var herb = Create(m_Kit.Herb);
            var exit = Create(m_Kit.Exit, null, 1);

            Assert.That(m_Slots.Inventory.Entities, Is.EqualTo(new[] { hero, exit, herb }));
            Assert.AreSame(m_Slots.Inventory, m_Slots.GetSlot(exit));
            Assert.AreEqual(1, m_Slots.Inventory.IndexOf(exit));
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Create_RulesViolated_NothingCreated()
        {
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            var link = expedition.Field.GetLink("k1");
            Create(m_Kit.Wall, link);
            var next = m_Entities.NextInstanceId;
            var count = m_Entities.Count;

            Assert.IsFalse(m_Slots.CanCreate(m_Kit.Poison, m_Slots.Inventory, out var reason));
            StringAssert.Contains("modifiers are created on their owner", reason);
            Assert.Throws<InvalidOperationException>(() => Create(m_Kit.Poison));
            Assert.Throws<InvalidOperationException>(() => Create(m_Kit.Wall), "the inventory takes cards");
            Assert.Throws<InvalidOperationException>(() => Create(m_Kit.Hero, link), "a link slot takes mechanics");
            Assert.Throws<InvalidOperationException>(() => Create(m_Kit.Wall, link), "a link slot takes one mechanic");
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(m_Kit.Hero, null, 1));

            Assert.AreEqual(next, m_Entities.NextInstanceId);
            Assert.AreEqual(count, m_Entities.Count);
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Rules_KindsAndTags()
        {
            var resources = new SlotRules(new[] { EntityKind.Card }, 0, SlotFacing.Keep, new[] { "Resource", "Exit" });

            Assert.IsTrue(resources.Accepts(m_Kit.Herb, out _));
            Assert.IsTrue(resources.Accepts(m_Kit.Exit, out _));
            Assert.IsFalse(resources.Accepts(m_Kit.Hero, out var reason));
            StringAssert.Contains("none of the tags Resource, Exit", reason);
            Assert.IsFalse(resources.Accepts(m_Kit.Wall, out reason));
            StringAssert.Contains("is a mechanic", reason);
            Assert.Throws<ArgumentException>(() => new SlotRules(new[] { EntityKind.Modifier }));
            Assert.Throws<ArgumentException>(() => new SlotRules(Array.Empty<EntityKind>()));
        }

#endregion

#region Move

        [Test]
        public void Move_BetweenSlots_EventsAndFacing()
        {
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            var monster = Create(m_Kit.Monster, expedition.Deck);
            Assert.IsFalse(IsFaceUp(monster), "the deck turns cards face down");
            var local = new List<EntityMoved>();
            var bus = new List<EntityMoved>();

            using (Record(local, bus))
            {
                m_Slots.Move(monster, expedition.Hand);
                Assert.IsTrue(IsFaceUp(monster), "the hand turns cards face up");
                m_Slots.Move(monster, expedition.Deck);
                m_Slots.Move(monster, expedition.Field.GetCell("s2"));
            }

            Assert.IsFalse(IsFaceUp(monster), "a field cell keeps the facing");
            Assert.That(local.Select(moved => $"{moved.From.Id}[{moved.FromIndex}]>{moved.To.Id}[{moved.ToIndex}]"), Is.EqualTo(new[]
            {
                "exp/deck[0]>exp/hand[0]",
                "exp/hand[0]>exp/deck[0]",
                "exp/deck[0]>exp/field/s2[0]",
            }));
            Assert.That(bus, Is.EqualTo(local));
            Assert.That(local.Select(moved => moved.Entity), Is.All.SameAs(monster));
            Assert.IsTrue(expedition.Deck.IsEmpty && expedition.Hand.IsEmpty);
            Assert.AreSame(expedition.Field.GetCell("s2"), m_Slots.GetSlot(monster));
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Move_Index_InsertAndReorder()
        {
            var hero = Create(m_Kit.Hero);
            var herb = Create(m_Kit.Herb);
            var exit = Create(m_Kit.Exit);
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            var monster = Create(m_Kit.Monster, expedition.Backpack);
            var local = new List<EntityMoved>();

            using (Record(local, new List<EntityMoved>()))
            {
                m_Slots.Move(exit, m_Slots.Inventory, 0);
                m_Slots.Move(monster, m_Slots.Inventory, 1);
                m_Slots.Move(hero, m_Slots.Inventory);
                m_Slots.Move(herb, m_Slots.Inventory, 3);
                m_Slots.Move(herb, m_Slots.Inventory, 3);
            }

            Assert.That(m_Slots.Inventory.Entities, Is.EqualTo(new[] { exit, monster, hero, herb }));
            Assert.That(local.Select(moved => (moved.From.Id, moved.FromIndex, moved.To.Id, moved.ToIndex)), Is.EqualTo(new[]
            {
                (SlotIds.Inventory, 2, SlotIds.Inventory, 0),
                (SlotIds.Backpack, 0, SlotIds.Inventory, 1),
                (SlotIds.Inventory, 2, SlotIds.Inventory, 3),
                (SlotIds.Inventory, 2, SlotIds.Inventory, 3),
            }), "the last move keeps the place: no event");
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Move_Violation_IsAtomic()
        {
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            var hero = Create(m_Kit.Hero);
            var herb = Create(m_Kit.Herb);
            var wall = Create(m_Kit.Wall, expedition.Field.GetLink("k1"));
            Create(m_Kit.Wall, expedition.Field.GetLink("k2"));
            var poison = m_Entities.AddModifier(hero, m_Kit.Poison);
            var before = EntityTestKit.Describe(m_Slots);
            var local = new List<EntityMoved>();

            using (Record(local, new List<EntityMoved>()))
            {
                Assert.Throws<InvalidOperationException>(() => m_Slots.Move(hero, expedition.Field.GetLink("k1")), "kind");
                Assert.Throws<InvalidOperationException>(() => m_Slots.Move(wall, expedition.Field.GetLink("k2")), "capacity");
                Assert.Throws<InvalidOperationException>(() => m_Slots.Move(wall, m_Slots.Inventory), "kind");
                Assert.Throws<InvalidOperationException>(() => m_Slots.Move(poison, m_Slots.Inventory), "modifiers are not in slots");
                Assert.Throws<ArgumentOutOfRangeException>(() => m_Slots.Move(hero, expedition.Hand, 1), "index");
                Assert.Throws<ArgumentOutOfRangeException>(() => m_Slots.Move(hero, m_Slots.Inventory, 2), "index after taking out");
                m_Entities.Destroy(herb);
                Assert.Throws<InvalidOperationException>(() => m_Slots.Move(herb, expedition.Hand), "destroyed");
            }

            Assert.IsEmpty(local);
            Assert.AreEqual(before.Replace($",Herb#{herb.InstanceId}", string.Empty), EntityTestKit.Describe(m_Slots));
            Assert.IsTrue(m_Slots.CanMove(wall, expedition.Field.GetLink("k1"), out _), "the same slot: the place is free after taking out");
            m_Slots.Move(wall, expedition.Field.GetLink("k1"), 0);
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Invariant_SeriesOfMoves_AndAfterLoad()
        {
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            var cards = new[] { m_Kit.Hero, m_Kit.Monster, m_Kit.Herb, m_Kit.Exit, m_Kit.Counter, m_Kit.Monster, m_Kit.Herb };
            foreach (var model in cards)
                Create(model);

            Create(m_Kit.Wall, expedition.Field.GetLink("k1"));
            Create(m_Kit.Wall, expedition.Field.GetLink("k2"));
            m_Entities.AddModifier(m_Slots.Inventory.Entities[1], m_Kit.Poison);
            var random = new System.Random(73);
            var moved = 0;
            var refused = 0;
            for (var step = 0; step < 400; step++)
            {
                var entities = m_Entities.Entities.OrderBy(entity => entity.InstanceId).ToList();
                var entity = entities[random.Next(entities.Count)];
                var slot = m_Slots.Slots[random.Next(m_Slots.Slots.Count)];
                var index = random.Next(-1, slot.Count + 1);
                if (m_Slots.CanMove(entity, slot, out _) && index <= (m_Slots.GetSlot(entity) == slot ? slot.Count - 1 : slot.Count))
                {
                    m_Slots.Move(entity, slot, index);
                    moved++;
                }
                else
                {
                    var before = EntityTestKit.Describe(m_Slots);
                    Assert.That(() => m_Slots.Move(entity, slot, index), Throws.InstanceOf<Exception>());
                    Assert.AreEqual(before, EntityTestKit.Describe(m_Slots), "a refused move changes nothing");
                    refused++;
                }

                EntityTestKit.AssertPlacement(m_Session);
            }

            Assert.That(moved, Is.GreaterThan(100));
            Assert.That(refused, Is.GreaterThan(10));
            var layout = EntityTestKit.Describe(m_Slots);

            Reload();

            Assert.AreEqual(layout, EntityTestKit.Describe(m_Slots));
            EntityTestKit.AssertPlacement(m_Session);
        }

#endregion

#region Field and expedition

        [Test]
        public void Field_FromGraph_IdsAndAdjacency()
        {
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            var field = expedition.Field;

            Assert.AreSame(m_Kit.Graph, field.Graph);
            Assert.That(field.Cells.Select(slot => slot.Id), Is.EqualTo(new[] { "exp/field/s1", "exp/field/s2", "exp/field/s3", "exp/field/s4" }));
            Assert.That(field.Links.Select(slot => slot.Id), Is.EqualTo(new[] { "exp/link/k1", "exp/link/k2" }));
            var (s1, s2, s3, s4) = (field.GetCell("s1"), field.GetCell("s2"), field.GetCell("s3"), field.GetCell("s4"));
            Assert.That(field.GetNeighbours(s1), Is.EqualTo(new[] { s2 }));
            Assert.That(field.GetNeighbours(s2), Is.EqualTo(new[] { s1, s3 }));
            Assert.IsEmpty(field.GetNeighbours(s4));
            Assert.AreSame(field.GetLink("k2"), field.GetLinkBetween(s3, s2));
            Assert.IsNull(field.GetLinkBetween(s1, s3));
            Assert.AreSame(s1, field.GetOther(field.GetLink("k1"), s2));
            Assert.That(field.GetLinks(s2), Is.EqualTo(new[] { field.GetLink("k1"), field.GetLink("k2") }));
            Assert.IsTrue(s4.HasTag("PlayerSpawn"));
            Assert.AreEqual("s4", s4.GraphSlot.Id);
            Assert.AreEqual("k1", field.GetLink("k1").GraphLink.Id);
            Assert.AreSame(SlotSystem.CellRules, s1.Rules);
            Assert.AreSame(SlotSystem.LinkRules, field.GetLink("k1").Rules);
            Assert.AreSame(s3, m_Slots.Get("exp/field/s3"));
        }

        [Test]
        public void Expedition_Create_ServiceSlots()
        {
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);

            Assert.AreSame(expedition, m_Slots.Expedition);
            Assert.AreSame(m_Kit.Location, expedition.Location);
            Assert.That(new[] { expedition.Deck, expedition.Hand, expedition.Discard, expedition.Backpack }.Select(slot => slot.Id),
                Is.EqualTo(new[] { "exp/deck", "exp/hand", "exp/discard", "exp/backpack" }));
            Assert.AreEqual(1 + 4 + 4 + 2, m_Slots.Slots.Count, "inventory, service slots, cells, links");
            Assert.That(expedition.All.Select(slot => slot.Id), Is.All.StartsWith(SlotIds.ExpeditionPrefix));
            Assert.Throws<InvalidOperationException>(() => m_Slots.CreateExpedition(m_Kit.Location), "one expedition");
        }

        [Test]
        public void Expedition_LocationWithoutGraph_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => m_Slots.CreateExpedition(m_Kit.NoField));
            Assert.IsNull(m_Slots.Expedition);
            Assert.AreEqual(1, m_Slots.Slots.Count);
        }

        [Test]
        public void Expedition_Destroy_RemovesSlotsAndTheirEntities()
        {
            var hero = Create(m_Kit.Hero);
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            var monster = Create(m_Kit.Monster, expedition.Field.GetCell("s1"));
            var poison = m_Entities.AddModifier(monster, m_Kit.Poison);
            var wall = Create(m_Kit.Wall, expedition.Field.GetLink("k1"));
            m_Slots.Move(hero, expedition.Hand);
            m_Slots.Move(hero, m_Slots.Inventory);
            var deck = expedition.Deck;

            m_Slots.DestroyExpedition();

            Assert.IsNull(m_Slots.Expedition);
            Assert.That(m_Slots.Slots, Is.EqualTo(new[] { m_Slots.Inventory }));
            Assert.IsTrue(monster.IsDestroyed && poison.IsDestroyed && wall.IsDestroyed);
            Assert.IsFalse(hero.IsDestroyed);
            Assert.AreEqual(1, m_Entities.Count);
            Assert.IsFalse(m_Slots.Contains(deck));
            Assert.Throws<InvalidOperationException>(() => m_Slots.Move(hero, deck), "a slot of the destroyed expedition");
            Assert.Throws<InvalidOperationException>(() => m_Slots.DestroyExpedition());
            EntityTestKit.AssertPlacement(m_Session);

            m_Slots.CreateExpedition(m_Kit.Location);
            Assert.AreEqual(11, m_Slots.Slots.Count, "a new expedition after the end");
        }

#endregion

#region Save

        [Test]
        public void Save_ExpeditionSlotsRestoredFromLocation()
        {
            var hero = Create(m_Kit.Hero);
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            var monster = Create(m_Kit.Monster, expedition.Deck);
            var herb = Create(m_Kit.Herb, expedition.Field.GetCell("s3"));
            var exit = Create(m_Kit.Exit, expedition.Field.GetCell("s3"), 0);
            Create(m_Kit.Wall, expedition.Field.GetLink("k2"));
            m_Entities.AddModifier(herb, m_Kit.Burn);
            m_Slots.Move(hero, expedition.Field.GetCell("s4"));
            monster.Variables.Set(m_Kit.Declaration("charges"), 1);
            var layout = EntityTestKit.Describe(m_Slots);

            var section = m_Session.Capture().GetSection(SlotSystem.Key);
            Assert.AreEqual("Field", (string)section.Data["expedition"]);
            Assert.That(section.Data["slots"].Select(slot => (string)slot["id"]), Is.EqualTo(new[] { "exp/deck", "exp/field/s3", "exp/field/s4", "exp/link/k2" }),
                "only non-empty slots");

            Reload();

            Assert.IsNotNull(m_Slots.Expedition);
            Assert.AreSame(m_Kit.Location, m_Slots.Expedition.Location);
            Assert.AreEqual(layout, EntityTestKit.Describe(m_Slots));
            var field = m_Slots.Expedition.Field;
            Assert.That(field.GetNeighbours(field.GetCell("s2")), Is.EqualTo(new[] { field.GetCell("s1"), field.GetCell("s3") }));
            Assert.AreEqual(new[] { exit.InstanceId, herb.InstanceId }, field.GetCell("s3").Entities.Select(entity => entity.InstanceId).ToArray());
            var loadedMonster = m_Slots.Expedition.Deck.Entities.Single();
            Assert.IsFalse(IsFaceUp(loadedMonster));
            Assert.AreEqual(1, loadedMonster.Variables.Get(m_Kit.Declaration("charges")));
            Assert.AreEqual("Burn", m_Entities.Get(herb.InstanceId).Modifiers.Single().Model.Id);
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Load_EntityInTwoSlots_StaysInFirst()
        {
            var save = Save("Field", ("hub/inventory", new[] { 1, 2 }), ("exp/deck", new[] { 1 }));

            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("card 1 'Hero' is in two slots (hub/inventory, exp/deck), it stays in hub/inventory")));
            Open(m_Kit.Load(save));

            Assert.That(m_Slots.Inventory.Entities.Select(entity => entity.InstanceId), Is.EqualTo(new[] { 1, 2 }));
            Assert.IsTrue(m_Slots.Expedition.Deck.IsEmpty);
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Load_EntityWithoutSlot_Removed_UnknownSlotWarned()
        {
            var save = Save(null, ("hub/inventory", new[] { 1 }), ("hub/removed", new[] { 2 }));

            LogAssert.Expect(LogType.Warning, new Regex("slot 'hub/removed' does not exist any more, its 1 entities have no slot"));
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape("card 2 'Monster' is in no slot, it is removed")));
            Open(m_Kit.Load(save));

            Assert.AreEqual(1, m_Entities.Count);
            Assert.IsFalse(m_Entities.TryGet(2, out _));
            EntityTestKit.AssertPlacement(m_Session);
        }

        [Test]
        public void Load_ExpeditionLocationMissing_SlotsNotRestored()
        {
            var save = Save("Removed", ("hub/inventory", new[] { 1 }), ("exp/deck", new[] { 2 }));

            LogAssert.Expect(LogType.Warning, new Regex("expedition location 'Removed' is not in the content"));
            LogAssert.Expect(LogType.Warning, new Regex("slot 'exp/deck' does not exist any more"));
            LogAssert.Expect(LogType.Warning, new Regex("card 2 'Monster' is in no slot, it is removed"));
            Open(m_Kit.Load(save));

            Assert.IsNull(m_Slots.Expedition);
            Assert.AreEqual(1, m_Entities.Count);
            EntityTestKit.AssertPlacement(m_Session);
        }

#region Hero of expedition

        [Test]
        public void Hero_SetOnlyForCardInExpeditionSlot()
        {
            var inInventory = Create(m_Kit.Hero);
            Assert.Throws<InvalidOperationException>(() => m_Slots.SetHero(inInventory), "no expedition");

            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            Assert.IsNull(expedition.Hero);
            Assert.Throws<InvalidOperationException>(() => m_Slots.SetHero(inInventory), "not in an expedition slot");
            var wall = Create(m_Kit.Wall, expedition.Field.GetLink("k1"));
            Assert.Throws<InvalidOperationException>(() => m_Slots.SetHero(wall), "not a card");

            var hero = Create(m_Kit.Hero, expedition.Field.GetCell("s4"));
            m_Slots.SetHero(hero);
            Assert.AreSame(hero, expedition.Hero);
            m_Slots.SetHero(null);
            Assert.IsNull(expedition.Hero);
        }

        [Test]
        public void Hero_Reset_MovedOutOfExpedition_Destroyed_WithExpedition()
        {
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            var hero = Create(m_Kit.Hero, expedition.Field.GetCell("s4"));
            m_Slots.SetHero(hero);

            m_Slots.Move(hero, expedition.Field.GetCell("s1"));
            m_Slots.Move(hero, expedition.Backpack);
            Assert.AreSame(hero, expedition.Hero, "moves inside the expedition keep the hero");

            m_Slots.Move(hero, m_Slots.Inventory);
            Assert.IsNull(expedition.Hero, "moved out of the expedition");
            m_Slots.Move(hero, expedition.Deck);
            Assert.IsNull(expedition.Hero, "the hero is set only explicitly");

            m_Slots.SetHero(hero);
            m_Entities.Destroy(hero);
            Assert.IsNull(expedition.Hero, "destroyed");

            m_Slots.SetHero(Create(m_Kit.Hero, expedition.Deck));
            m_Slots.DestroyExpedition();
            Assert.IsNull(m_Slots.CreateExpedition(m_Kit.Location).Hero, "a new expedition has no hero");
        }

        [Test]
        public void Save_HeroOfExpedition_Restored()
        {
            var expedition = m_Slots.CreateExpedition(m_Kit.Location);
            var hero = Create(m_Kit.Hero, expedition.Field.GetCell("s4"));
            m_Slots.SetHero(hero);

            var section = m_Session.Capture().GetSection(SlotSystem.Key);
            Assert.AreEqual(hero.InstanceId, (int)section.Data["hero"]);

            Reload();

            Assert.AreEqual(hero.InstanceId, m_Slots.Expedition.Hero.InstanceId);
            Assert.AreSame(m_Slots.Expedition.Field.GetCell("s4"), m_Slots.GetSlot(m_Slots.Expedition.Hero));
        }

        [Test]
        public void Save_NoHero_NullAndRestoredWithoutHero()
        {
            m_Slots.CreateExpedition(m_Kit.Location);

            var section = m_Session.Capture().GetSection(SlotSystem.Key);
            Assert.AreEqual(JTokenType.Null, section.Data["hero"].Type);

            Reload();
            Assert.IsNull(m_Slots.Expedition.Hero);
        }

        [Test]
        public void Load_OldSaveWithoutHeroField_NoHero()
        {
            var save = Save("Field", ("exp/deck", new[] { 1, 2 }));
            Assert.IsTrue(save.GetSection(SlotSystem.Key).Data.Remove("hero"));

            Open(m_Kit.Load(save));

            Assert.IsNull(m_Slots.Expedition.Hero);
            Assert.AreEqual(2, m_Slots.Expedition.Deck.Count);
        }

        [TestCase(1, "Inventory", "hero card 1 'Hero' is not in an expedition slot, the expedition has no hero")]
        [TestCase(9, "Field", "hero 9 is not a restored card, the expedition has no hero")]
        [TestCase(3, "Field", "hero 3 is not a restored card, the expedition has no hero")]
        [TestCase(1, "NoExpedition", "hero 1: there are no expedition slots, the hero is not restored")]
        public void Load_HeroAnomalies_WarningAndNoHero(int hero, string layout, string warning)
        {
            var save = layout switch
            {
                "Field" => Save("Field", hero, ("exp/deck", new[] { 1, 2 })),
                "NoExpedition" => Save(null, hero, ("hub/inventory", new[] { 1, 2 })),
                _ => Save("Field", hero, ("hub/inventory", new[] { 1 }), ("exp/deck", new[] { 2 })),
            };

            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(warning)));
            Open(m_Kit.Load(save));

            Assert.IsNull(m_Slots.Expedition?.Hero);
            EntityTestKit.AssertPlacement(m_Session);
        }

#endregion

        /// <summary>Сейв: герой (1) и монстр (2) с модификатором (3), секция слотов из пар «слот — ID», без героя экспедиции.</summary>
        private static SaveGame Save(string expedition, params (string Slot, int[] Ids)[] slots)
        {
            return Save(expedition, null, slots);
        }

        /// <summary>Как <see cref="Save(string, (string, int[])[])"/>, с <c>InstanceId</c> героя экспедиции.</summary>
        private static SaveGame Save(string expedition, int? hero, params (string Slot, int[] Ids)[] slots)
        {
            var save = new SaveGame();
            save.AddSection(SaveSection.FromObject(EntityRegistry.Key, 1, new EntitiesSave
            {
                NextId = 4,
                Entities =
                {
                    new EntitySave { Id = 1, Model = "Hero" },
                    new EntitySave { Id = 2, Model = "Monster", Modifiers = new List<EntitySave> { new EntitySave { Id = 3, Model = "Poison" } } },
                },
            }));

            var slotsSave = new SlotsSave { Expedition = expedition, Hero = hero };
            foreach (var (slot, ids) in slots)
                slotsSave.Slots.Add(new SlotSave { Id = slot, Entities = ids.ToList() });

            save.AddSection(SaveSection.FromObject(SlotSystem.Key, 1, slotsSave));
            return save;
        }

#endregion
    }
}
