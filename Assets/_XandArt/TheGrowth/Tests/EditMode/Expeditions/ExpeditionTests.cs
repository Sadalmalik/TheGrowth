using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TheGrowth.Architecture.Events;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Variables;
using TheGrowth.Model;
using TheGrowth.Model.Decks;
using TheGrowth.Model.Expeditions;
using TheGrowth.Model.Slots;
using TheGrowth.Tests.EditMode.Cards;
using TheGrowth.Tests.EditMode.Scripting;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Expeditions
{
    /// <summary>
    /// Ход экспедиции без сцены (<see cref="ExpeditionSystem"/>) на поле 3 × 3 с шагом 1,5 × 2 (как карта; клетки a1..c3 и x без
    /// связей, a1 — <c>PlayerSpawn</c>, c3 — <c>Exit</c>) с героями и способностями контента (Lucia, Stefan, Vision, Talisman,
    /// Teleport, Revolver), монстром, травой и выходом контента: старт (раскладка, ошибки до изменений), действие игрока, ход
    /// монстров, конец хода (порядок <c>OnStep</c>, пропуски, повторный <c>end turn</c>), пропуск хода, конец экспедиции (победа,
    /// поражение, итоги), отказ действий, сейв секции <c>expedition</c>. После каждого шага — инвариант «карта ровно в одном
    /// слоте». Колода по умолчанию — одна трава «не раскладывать»: на поле только герой, карты кладутся тестом без хуков.
    /// </summary>
    public sealed class ExpeditionTests
    {
        private CardTestKit m_Kit;
        private EntityModel m_Lucia;
        private EntityModel m_Stefan;
        private EntityModel m_Vision;
        private EntityModel m_Talisman;
        private EntityModel m_Teleport;
        private EntityModel m_Revolver;
        private CardSession m_Cards;
        private readonly List<CardSession> m_Others = new List<CardSession>();
        private readonly List<ExpeditionEnded> m_Ended = new List<ExpeditionEnded>();
        private readonly List<ExpeditionEnded> m_BusEnded = new List<ExpeditionEnded>();

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit(3, 1.5f, 2f);
            m_Lucia = m_Kit.ContentModel("Entities/Heroes/Lucia.asset");
            m_Stefan = m_Kit.ContentModel("Entities/Heroes/Stefan.asset");
            m_Vision = m_Kit.ContentModel("Entities/Abilities/Vision.asset");
            m_Talisman = m_Kit.ContentModel("Entities/Abilities/Talisman.asset");
            m_Teleport = m_Kit.ContentModel("Entities/Abilities/Teleport.asset");
            m_Revolver = m_Kit.ContentModel("Entities/Abilities/Revolver.asset");
            m_Ended.Clear();
            m_BusEnded.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var other in m_Others)
                other.Dispose();

            m_Others.Clear();
            m_Cards?.Dispose();
            m_Cards = null;
            m_Kit.Dispose();
        }

        private ExpeditionSystem Expedition => m_Cards.Expedition;

        private SlotSystem Slots => m_Cards.Slots;

        /// <summary>Сессия без слотов экспедиции; колода локации — строки (нет строк — одна трава «не раскладывать»).</summary>
        private void Open(params DeckEntry[] entries)
        {
            m_Kit.Deck(entries.Length > 0 ? entries : new[] { new DeckEntry(m_Kit.Herb, 1, true) });
            OpenWithoutDeck();
        }

        private void OpenWithoutDeck()
        {
            m_Cards = new CardSession(m_Kit, m_Kit.NewSession(CardTestKit.DefaultSeed, false));
            Expedition.Ended += m_Ended.Add;
            m_Kit.Global.Get<EventBus>().Subscribe<ExpeditionEnded>(m_BusEnded.Add);
        }

        /// <summary>Герой и способности — в инвентарь, затем старт экспедиции в локации набора.</summary>
        private Entity Start(EntityModel hero = null, params EntityModel[] abilities)
        {
            var card = Slots.Create(hero ?? m_Lucia, Slots.Inventory);
            var list = abilities.Select(model => Slots.Create(model, Slots.Inventory)).ToArray();
            CardSession.Complete(Expedition.Start(m_Kit.Location, card, list));
            AssertOneSlotEach();
            return card;
        }

        private Entity Place(EntityModel model, string cell, bool faceUp = true)
        {
            return m_Cards.Place(model, cell, faceUp);
        }

        private Entity InHand(EntityModel model)
        {
            return Slots.Expedition.Hand.Entities.Single(card => card.Model == model);
        }

        /// <summary>Действие завершилось синхронно (анимаций нет): принято ли оно.</summary>
        private static bool Done(UniTask<bool> task)
        {
            var awaiter = task.GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted, "the action is not completed synchronously");
            return awaiter.GetResult();
        }

        private bool Play(Entity card, string cell)
        {
            var accepted = Done(Expedition.Play(card, m_Cards.Cell(cell)));
            AssertOneSlotEach();
            return accepted;
        }

        private string[] Moves(Entity card)
        {
            return Expedition.GetAllowedMoves(card).Select(CardTrace.Name).ToArray();
        }

        private static string[] Names(Slot slot)
        {
            return slot.Entities.Select(card => card.Model.Id).ToArray();
        }

        /// <summary>Каждая живая карта и объект механики — ровно в одном слоте; в слотах только живые сущности.</summary>
        private void AssertOneSlotEach()
        {
            var places = new Dictionary<Entity, Slot>();
            foreach (var slot in Slots.Slots)
            {
                foreach (var entity in slot.Entities)
                {
                    Assert.IsFalse(entity.IsDestroyed, $"{entity} in {slot} is destroyed");
                    Assert.IsFalse(places.TryGetValue(entity, out var other), $"{entity} is in {other} and {slot}");
                    places.Add(entity, slot);
                    Assert.AreSame(slot, Slots.GetSlot(entity), $"{entity}");
                }
            }

            foreach (var entity in m_Cards.Entities.Entities)
            {
                if (entity.Kind != EntityKind.Modifier)
                    Assert.IsTrue(places.ContainsKey(entity), $"{entity} is in no slot");
            }
        }

        private void AssertEnded(ExpeditionResult result)
        {
            Assert.That(m_Ended.Select(ended => ended.Result), Is.EqualTo(new[] { result }));
            Assert.That(m_BusEnded.Select(ended => ended.Result), Is.EqualTo(new[] { result }), "the event is in the bus too");
            Assert.AreSame(m_Kit.Location, m_Ended[0].Location);
            Assert.IsFalse(Expedition.IsActive);
            Assert.IsNull(Expedition.Slots);
            Assert.IsNull(Slots.Expedition, "the expedition slots are destroyed");
            Assert.AreEqual(0, Expedition.TurnsLeft);
            Assert.AreEqual(0, Expedition.TurnNumber);
            Assert.IsNull(Expedition.Variables);
            Assert.AreEqual(0, m_Cards.Runner.ActiveCount);
            AssertOneSlotEach();
        }

#region Start

        [Test]
        public void Start_DealsDeck_HeroOnSpawn_AbilitiesInHand_HeroSetsTurns()
        {
            Open(new DeckEntry(m_Kit.Exit), new DeckEntry(m_Kit.Monster), new DeckEntry(m_Kit.Herb, 2), new DeckEntry(m_Kit.Herb, 3, true));

            var hero = Start(m_Lucia, m_Vision, m_Talisman);

            var expedition = Expedition.Slots;
            Assert.IsTrue(Expedition.IsActive);
            Assert.AreSame(Slots.Expedition, expedition);
            Assert.AreSame(m_Kit.Location, Expedition.Location);
            Assert.AreSame(m_Cards.Cell("a1"), Slots.GetSlot(hero), "the hero is on PlayerSpawn");
            Assert.AreSame(hero, expedition.Hero);
            Assert.IsTrue(CardSession.IsFaceUp(hero));
            Assert.That(Names(expedition.Hand), Is.EqualTo(new[] { "Vision", "Talisman" }), "the abilities are moved, not copied");
            Assert.IsTrue(Slots.Inventory.IsEmpty);
            Assert.That(Names(m_Cards.Cell("c3")), Is.EqualTo(new[] { "Exit" }), "SpawnSlot of the exit");
            Assert.AreEqual(4, expedition.Field.Cells.Where(cell => cell != m_Cards.Cell("a1")).Sum(cell => cell.Count), m_Cards.Describe());
            Assert.That(Names(expedition.Deck), Is.EqualTo(new[] { "Herb", "Herb", "Herb" }), "the rows to keep in the deck");
            Assert.AreEqual(25, Expedition.TurnsLeft, "OnPlacedFirstTime of Lucia");
            Assert.AreEqual(1, Expedition.TurnNumber);
            Assert.IsNotNull(Expedition.Variables);
            Assert.IsTrue(Expedition.CanAct);
            Assert.AreEqual(0, m_Cards.Runner.ActiveCount);
        }

        [Test]
        public void Start_Errors_NothingChanged()
        {
            Open();
            var hero = Slots.Create(m_Lucia, Slots.Inventory);
            var revolver = Slots.Create(m_Revolver, Slots.Inventory);
            var entities = m_Cards.Entities.Count;

            void AssertNotStarted(UniTask task, string message)
            {
                var probe = Probe.Watch(task);
                Assert.IsTrue(probe.IsCompleted);
                Assert.IsInstanceOf<InvalidOperationException>(probe.Exception);
                StringAssert.Contains(message, probe.Exception.Message);
                Assert.IsFalse(Expedition.IsActive);
                Assert.AreSame(Slots.Inventory, Slots.GetSlot(hero));
                Assert.AreSame(Slots.Inventory, Slots.GetSlot(revolver));
                Assert.AreEqual(entities, m_Cards.Entities.Count, "nothing is created");
                Assert.AreEqual(0, m_Cards.Runner.ActiveCount);
            }

            AssertNotStarted(Expedition.Start(m_Kit.Location, null), "the hero null is destroyed, not from this session or not a card");
            Assert.IsNull(Slots.Expedition);

            AssertNotStarted(Expedition.Start(m_Kit.Location, hero, new[] { revolver }), "doesn't fit the hero");
            Assert.IsNull(Slots.Expedition, "the expedition slots are removed");

            Slots.CreateExpedition(m_Kit.Location);
            AssertNotStarted(Expedition.Start(m_Kit.Location, hero), "expedition slots of");
            Assert.IsTrue(Slots.Expedition.All.All(slot => slot.IsEmpty));
            Slots.DestroyExpedition();

            Assert.IsInstanceOf<ArgumentNullException>(Probe.Watch(Expedition.Start(null, hero)).Exception);

            CardSession.Complete(Expedition.Start(m_Kit.Location, hero));
            var layout = m_Cards.Describe();
            var probe = Probe.Watch(Expedition.Start(m_Kit.Location, revolver));
            Assert.IsInstanceOf<InvalidOperationException>(probe.Exception);
            StringAssert.Contains("is active", probe.Exception.Message);
            Assert.IsTrue(Expedition.IsActive);
            Assert.AreEqual(layout, m_Cards.Describe());
            Assert.AreEqual(25, Expedition.TurnsLeft);
            AssertOneSlotEach();
        }

        [Test]
        public void Start_LocationWithoutDeck_Warning_OnlyHero()
        {
            OpenWithoutDeck();

            LogAssert.Expect(LogType.Warning, new Regex("has no deck, only the hero is dealt"));
            var hero = Start();

            Assert.AreEqual("a1:[Lucia]", m_Cards.Describe(), "no deck: nothing else is created");
            Assert.AreSame(hero, Slots.Expedition.Hero);
        }

#endregion

#region Turn

        [Test]
        public void Scenario_HeroMoves_MonsterSteps_SkipTurn_Victory()
        {
            Open();
            var hero = Start(m_Lucia, m_Vision);
            var herb = Place(m_Kit.Herb, "a2");
            var exit = Place(m_Kit.Exit, "b2");
            var monster = Place(m_Kit.Monster, "c3");
            m_Cards.Run(monster, "set [active card].charges to 1");

            // Ход 1: герой на траву — трава в рюкзак, конец хода из OnPlaced героя, монстр тратит заряд.
            Assert.That(Moves(hero), Is.EqualTo(new[] { "a2" }), "b1 is free");
            Assert.IsTrue(Play(hero, "a2"));
            Assert.AreEqual("deck:[Herb] hand:[Vision] backpack:[Herb] a2:[Lucia] b2:[Exit] c3:[Monster]", m_Cards.Describe());
            Assert.AreEqual(0, m_Cards.Charges(monster));
            Assert.AreEqual(24, Expedition.TurnsLeft);
            Assert.AreEqual(2, Expedition.TurnNumber);
            Assert.IsTrue(Expedition.CanAct);

            // Ход 2: пропуск — монстр идёт к герою в обход выхода.
            Assert.IsTrue(Done(Expedition.SkipTurn()));
            AssertOneSlotEach();
            Assert.AreEqual("deck:[Herb] hand:[Vision] backpack:[Herb] a2:[Lucia] b2:[Exit] b3:[Monster]", m_Cards.Describe());
            Assert.AreEqual(24, Expedition.TurnsLeft, "the end of the turn does not change the counter, only scripts do");
            Assert.AreEqual(3, Expedition.TurnNumber);

            // Ход 3: герой на выход — победа: герой, способность и рюкзак — в инвентарь, остальное уничтожено.
            Assert.That(Moves(hero), Is.EqualTo(new[] { "b2" }));
            Assert.IsTrue(Play(hero, "b2"));

            AssertEnded(ExpeditionResult.Victory);
            Assert.That(Slots.Inventory.Entities, Is.EqualTo(new[] { hero, InventoryCard(m_Vision), herb }));
            Assert.IsTrue(exit.IsDestroyed && monster.IsDestroyed);
        }

        private Entity InventoryCard(EntityModel model)
        {
            return Slots.Inventory.Entities.Single(card => card.Model == model);
        }

        [Test]
        public void EndTurn_OnStepOfTopCards_InGraphOrder_SkipsLeftAndDestroyed_EachOnce()
        {
            var kicker = m_Kit.Model("Kicker", string.Join("\n",
                "OnStep:",
                "    trace [active card] step",
                "    for each card in [cards tagged Resource]",
                "        move [iterating card] to [backpack]",
                "    for each card in [cards tagged Obstacle]",
                "        destroy [iterating card]",
                ""));
            var walkerModel = m_Kit.Model("Walker", "OnStep:\n    trace [active card] step\n    move [active card] to [random slot of [slots tagged Exit]]\n");
            var leafModel = m_Kit.Model("Leaf", "OnStep:\n    trace [active card] step\n", "Resource");
            var stoneModel = m_Kit.Model("Stone", "OnStep:\n    trace [active card] step\n", "Obstacle");
            Open();
            var hero = Start();
            Place(kicker, "a2");
            var walker = Place(walkerModel, "a3");
            var leaf = Place(leafModel, "b1");
            var stone = Place(stoneModel, "b2");
            Place(m_Kit.Alpha, "c1");
            Place(m_Kit.Beta, "c1");
            m_Cards.Trace.Clear();

            Assert.IsTrue(Done(Expedition.SkipTurn()));

            Assert.That(m_Cards.Trace.Entries, Is.EqualTo(new[] { "Kicker: step", "Walker: step", "Beta: OnStep" }),
                "a card that left the field or is destroyed before its turn is skipped; the covered card has no step; the walker steps once");
            Assert.AreSame(Slots.Expedition.Backpack, Slots.GetSlot(leaf));
            Assert.IsTrue(stone.IsDestroyed);
            Assert.AreSame(m_Cards.Cell("c3"), Slots.GetSlot(walker));
            Assert.AreSame(m_Cards.Cell("a1"), Slots.GetSlot(hero), "skip turn: no action of the hero");
            Assert.AreEqual(25, Expedition.TurnsLeft, "the end of the turn does not change the counter, only scripts do");
            Assert.AreEqual(2, Expedition.TurnNumber);
            AssertOneSlotEach();
        }

        [Test]
        public void EndTurn_RepeatedInOneAction_OneEndOfTurn()
        {
            var doubler = m_Kit.Model("Doubler", "OnCovered:\n    end turn\n    end turn\n");
            Open();
            var hero = Start();
            Place(doubler, "a2");
            Place(m_Kit.Alpha, "b3");
            m_Cards.Trace.Clear();

            Assert.IsTrue(Play(hero, "a2"));

            Assert.That(m_Cards.Trace.Entries, Is.EqualTo(new[] { "Alpha: OnStep" }));
            Assert.AreEqual(24, Expedition.TurnsLeft);
            Assert.AreEqual(2, Expedition.TurnNumber);
        }

        [Test]
        public void EndRequests_OutsideOfPlayerAction_Warning_Ignored()
        {
            Open();
            Start();

            LogAssert.Expect(LogType.Warning, new Regex(@"end turn: no player action is in progress.*the command does nothing"));
            LogAssert.Expect(LogType.Warning, new Regex(@"end expedition with victory: no player action is in progress.*the command does nothing"));
            m_Cards.Run(null, "end turn", "end expedition with victory");

            Assert.IsTrue(Expedition.IsActive);
            Assert.AreEqual(1, Expedition.TurnNumber);
            Assert.AreEqual(25, Expedition.TurnsLeft);
            Assert.IsEmpty(m_Ended);
        }

#endregion

#region Abilities

        [Test]
        public void Talisman_AddsTurns_DoesNotEndTurn()
        {
            Open();
            var hero = Start(m_Lucia, m_Talisman);
            var talisman = InHand(m_Talisman);
            var monster = Place(m_Kit.Monster, "c3");

            Assert.That(Moves(talisman), Is.EqualTo(new[] { "a1" }));
            Assert.IsTrue(Play(talisman, "a1"));

            Assert.AreEqual(28, Expedition.TurnsLeft);
            Assert.AreEqual(1, Expedition.TurnNumber, "the talisman does not end the turn");
            Assert.AreEqual(3, m_Cards.Charges(monster), "no step of monsters");
            Assert.AreSame(Slots.Expedition.Backpack, Slots.GetSlot(talisman));
            Assert.IsFalse(CardSession.IsFaceUp(talisman));
            Assert.That(m_Cards.Cell("a1").Entities, Is.EqualTo(new[] { hero }));
            Assert.IsTrue(Expedition.CanAct);
        }

        [Test]
        public void Revolver_RemovesMonster_EndsTurn()
        {
            Open();
            Start(m_Stefan, m_Revolver);
            var revolver = InHand(m_Revolver);
            var target = Place(m_Kit.Monster, "b2");
            var other = Place(m_Kit.Monster, "c3");

            Assert.IsTrue(Play(revolver, "b2"));

            Assert.AreSame(Slots.Expedition.Deck, Slots.GetSlot(target));
            Assert.AreEqual(1, m_Cards.Charges(revolver));
            Assert.AreSame(Slots.Expedition.Hand, Slots.GetSlot(revolver));
            Assert.AreEqual(2, m_Cards.Charges(other), "the revolver ends the turn: the other monster steps");
            Assert.AreEqual(25, Expedition.TurnsLeft, "the revolver ends the turn without spending a turn, as in the prototype");
            Assert.AreEqual(2, Expedition.TurnNumber);
        }

        [Test]
        public void Teleport_CardUnderMovedWithoutHooks_HeroWithHooks_EndsTurn()
        {
            Open();
            var hero = Start(m_Lucia, m_Teleport);
            var teleport = InHand(m_Teleport);
            var alpha = Place(m_Kit.Alpha, "b2");
            m_Cards.Trace.Clear();

            Assert.That(Moves(teleport), Does.Contain("b2"));
            Assert.IsTrue(Play(teleport, "b2"));

            Assert.That(m_Cards.Trace.Entries, Is.EqualTo(new[] { "Alpha: OnCovered <- Teleport", "Alpha: OnStep" }),
                "the card under the teleport moves without hooks (no OnPlaced), the hero ends the turn");
            Assert.That(m_Cards.Cell("a1").Entities, Is.EqualTo(new[] { alpha }));
            Assert.That(m_Cards.Cell("b2").Entities, Is.EqualTo(new[] { hero }));
            Assert.AreSame(hero, Slots.Expedition.Hero);
            Assert.AreEqual(1, m_Cards.Charges(teleport));
            Assert.AreSame(Slots.Expedition.Hand, Slots.GetSlot(teleport));
            Assert.AreEqual(2, Expedition.TurnNumber);
        }

#endregion

#region End

        [Test]
        public void Victory_HeroAndAbilitiesFromAnySlot_BackpackToInventory_ChargesKept_RestOfTurnSkipped()
        {
            Open();
            var hero = Start(m_Lucia, m_Vision, m_Talisman);
            var vision = InHand(m_Vision);
            var talisman = InHand(m_Talisman);
            var herb = Slots.Create(m_Kit.Herb, Slots.Expedition.Backpack);
            Slots.Move(talisman, Slots.Expedition.Backpack);
            m_Cards.Run(talisman, "set [active card].charges to 0");
            var exit = Place(m_Kit.Exit, "a2");
            var alpha = Place(m_Kit.Alpha, "b3");
            var deck = Slots.Expedition.Deck.Entities.ToList();
            m_Cards.Trace.Clear();

            Assert.IsTrue(Play(hero, "a2"));

            AssertEnded(ExpeditionResult.Victory);
            Assert.That(Slots.Inventory.Entities, Is.EqualTo(new[] { hero, vision, talisman, herb }), "hero, abilities by slots, then the backpack");
            Assert.AreEqual(0, m_Cards.Charges(talisman), "charges are not restored");
            Assert.AreEqual(4, m_Cards.Charges(vision));
            Assert.IsTrue(exit.IsDestroyed && alpha.IsDestroyed && deck.All(card => card.IsDestroyed));
            Assert.IsFalse(m_Cards.Trace.Entries.Contains("Alpha: OnStep"), "the rest of the turn is skipped");
        }

        [Test]
        public void Defeat_MonsterEatsHero_BackpackLost_HeroAndAbilitiesReturn_RestOfStepsSkipped()
        {
            Open();
            var hero = Start(m_Lucia, m_Vision);
            var vision = InHand(m_Vision);
            var herb = Place(m_Kit.Herb, "a2");
            Place(m_Kit.Beta, "a3");
            var monster = Place(m_Kit.Monster, "b2");
            m_Cards.Run(monster, "set [active card].charges to 0");
            Place(m_Kit.Alpha, "c1");
            m_Cards.Trace.Clear();

            Assert.IsTrue(Play(hero, "a2"));

            AssertEnded(ExpeditionResult.Defeat);
            Assert.That(Slots.Inventory.Entities, Is.EqualTo(new[] { hero, vision }));
            Assert.IsTrue(herb.IsDestroyed, "the backpack is lost");
            Assert.IsTrue(monster.IsDestroyed);
            Assert.That(m_Cards.Trace.Entries, Is.EqualTo(new[] { "Beta: OnStep" }), "the steps after the defeat are skipped");
        }

        [Test]
        public void Defeat_NoTurnsLeft()
        {
            Open();
            var hero = Start();
            Place(m_Kit.Herb, "a2");
            Expedition.SetTurnsLeft(0);

            Assert.IsTrue(Play(hero, "a2"));

            AssertEnded(ExpeditionResult.Defeat);
            Assert.That(Slots.Inventory.Entities, Is.EqualTo(new[] { hero }));
        }

        [Test]
        public void EndExpedition_Repeated_FirstResultStays_Warning()
        {
            var judge = m_Kit.Model("Judge", "OnCovered:\n    end expedition with victory\n    end expedition with defeat\n");
            Open();
            var hero = Start();
            Place(judge, "a2");

            LogAssert.Expect(LogType.Warning, new Regex("end expedition with defeat: the expedition already ends with victory, the first result stays"));
            Assert.IsTrue(Play(hero, "a2"));

            AssertEnded(ExpeditionResult.Victory);
        }

#endregion

#region Rejected actions

        [Test]
        public void Rejected_NotHeroOnFieldOrHandCard_NothingChanged()
        {
            Open();
            var hero = Start(m_Lucia, m_Talisman);
            var monster = Place(m_Kit.Monster, "a2");
            Place(m_Kit.Herb, "b1");
            var herbInHand = Slots.Create(m_Kit.Herb, Slots.Expedition.Hand);
            var inInventory = Slots.Create(m_Kit.Herb, Slots.Inventory);
            var inDeck = Slots.Expedition.Deck.Entities[0];
            var moved = 0;
            Slots.Moved += _ => moved++;
            m_Cards.Trace.Clear();

            Assert.That(Moves(hero), Is.EqualTo(new[] { "b1" }), "a2 has a monster");
            Assert.IsEmpty(Moves(monster), "a card on the field that is not the hero");
            Assert.IsFalse(Play(monster, "b1"));
            Assert.IsEmpty(Moves(herbInHand), "a card in the hand without AllowedMoves");
            Assert.IsFalse(Play(herbInHand, "b1"));
            Assert.IsFalse(Play(inInventory, "b1"));
            Assert.IsFalse(Play(inDeck, "b1"));
            Assert.IsFalse(Done(Expedition.Play(null, m_Cards.Cell("b1"))));

            m_Cards.Cards.SetFaceUp(hero, false).Forget();
            Assert.IsEmpty(Moves(hero), "the hero face down");
            Assert.IsFalse(Play(hero, "b1"));
            m_Cards.Cards.SetFaceUp(hero, true).Forget();

            Assert.AreEqual(0, moved);
            Assert.IsEmpty(m_Cards.Trace.Entries);
            Assert.AreEqual(1, Expedition.TurnNumber);
            Assert.AreEqual(25, Expedition.TurnsLeft);
            Assert.That(Moves(hero), Is.EqualTo(new[] { "b1" }));
        }

        [Test]
        public void Rejected_SlotNotInAllowedMoves_NothingChanged()
        {
            Open();
            var hero = Start();
            Place(m_Kit.Herb, "a2");
            Place(m_Kit.Herb, "c3");
            var moved = 0;
            Slots.Moved += _ => moved++;

            Assert.That(Moves(hero), Is.EqualTo(new[] { "a2" }));
            Assert.IsFalse(Play(hero, "b1"), "a free cell");
            Assert.IsFalse(Play(hero, "a1"), "the own cell");
            Assert.IsFalse(Play(hero, "c3"), "too far");
            Assert.IsFalse(Done(Expedition.Play(hero, Slots.Inventory)));
            Assert.IsFalse(Done(Expedition.Play(hero, null)));

            Assert.AreEqual(0, moved);
            Assert.AreEqual(1, Expedition.TurnNumber);
        }

        [Test]
        public void Rejected_DuringAction_ThenActionCompletes()
        {
            var waiter = m_Kit.Model("Waiter", "OnCovered:\n    wait for gate\n");
            Open();
            var hero = Start(m_Lucia, m_Talisman);
            var talisman = InHand(m_Talisman);
            Place(waiter, "a2");

            var play = Expedition.Play(hero, m_Cards.Cell("a2"));

            Assert.IsFalse(play.GetAwaiter().IsCompleted);
            Assert.IsTrue(Expedition.IsActing);
            Assert.IsFalse(Expedition.CanAct);
            Assert.IsFalse(m_Cards.Runner.IsIdle, "the action is one activity: no save point");
            Assert.IsEmpty(Moves(hero));
            Assert.IsFalse(Done(Expedition.Play(talisman, m_Cards.Cell("a1"))));
            Assert.IsFalse(Done(Expedition.SkipTurn()));
            AssertOneSlotEach();

            m_Cards.Gate.Open();

            Assert.IsTrue(Done(play));
            Assert.IsFalse(Expedition.IsActing);
            Assert.IsTrue(Expedition.CanAct);
            Assert.AreEqual(2, Expedition.TurnNumber, "the hero ends the turn after the wait");
            Assert.AreEqual(0, m_Cards.Runner.ActiveCount);
            AssertOneSlotEach();
        }

        [Test]
        public void Rejected_WhileOtherScriptRuns()
        {
            Open();
            var hero = Start();
            Place(m_Kit.Herb, "a2");
            var run = m_Cards.Runner.Run(m_Cards.Parse("wait for gate"), "Start");

            Assert.IsFalse(Expedition.CanAct);
            Assert.IsFalse(Play(hero, "a2"));
            Assert.IsFalse(Done(Expedition.SkipTurn()));

            m_Cards.Gate.Open();
            CardSession.Complete(run);

            Assert.IsTrue(Expedition.CanAct);
            Assert.IsTrue(Play(hero, "a2"));
        }

        [Test]
        public void Rejected_AfterEndAndBeforeStart()
        {
            Open();
            Assert.IsFalse(Expedition.CanAct);
            Assert.IsFalse(Done(Expedition.SkipTurn()));

            var hero = Start();
            Place(m_Kit.Exit, "a2");
            Assert.IsTrue(Play(hero, "a2"));
            AssertEnded(ExpeditionResult.Victory);

            Assert.IsFalse(Expedition.CanAct);
            Assert.IsEmpty(Expedition.GetAllowedMoves(hero));
            Assert.IsFalse(Done(Expedition.SkipTurn()));
        }

#endregion

#region Save

        [Test]
        public void SaveAndLoad_ActiveExpedition_CounterTurnVariablesHero()
        {
            Open();
            var hero = Start(m_Lucia, m_Talisman);
            Place(m_Kit.Herb, "a2");
            Place(m_Kit.Herb, "b2");
            Assert.IsTrue(Play(hero, "a2"));
            m_Cards.Run(null, "set $clues to 5");

            var save = m_Cards.Session.Capture();
            var data = save.GetSection(ExpeditionSystem.Key).Data;
            Assert.AreEqual(true, (bool)data["active"]);
            Assert.AreEqual(24, (int)data["turnsLeft"]);
            Assert.AreEqual(2, (int)data["turnNumber"]);
            Assert.AreEqual(5, (int)data["variables"]["clues"]);

            var loaded = new CardSession(m_Kit, m_Kit.Reload(m_Cards.Session));
            m_Others.Add(loaded);
            var expedition = loaded.Expedition;
            Assert.IsTrue(expedition.IsActive);
            Assert.AreEqual(24, expedition.TurnsLeft);
            Assert.AreEqual(2, expedition.TurnNumber);
            Assert.AreEqual(5, expedition.Variables.Get<int>(m_Kit.Global.Get<VariableCatalog>().Get("clues")));
            Assert.AreEqual(m_Cards.Describe(), loaded.Describe());

            var loadedHero = loaded.Slots.Expedition.Hero;
            Assert.IsNotNull(loadedHero);
            Assert.That(expedition.GetAllowedMoves(loadedHero).Select(CardTrace.Name), Is.EqualTo(new[] { "b2" }));
            Assert.IsTrue(Done(expedition.Play(loadedHero, loaded.Cell("b2"))));
            Assert.AreEqual(3, expedition.TurnNumber);
        }

        [Test]
        public void Load_ActiveWithoutExpeditionSlots_Warning_NoExpedition()
        {
            Open();
            var save = new SaveGame();
            save.AddSection(SaveSection.FromObject(ExpeditionSystem.Key, 1, new ExpeditionSave { Active = true, TurnsLeft = 7, TurnNumber = 3 }));

            LogAssert.Expect(LogType.Warning, new Regex("Section 'expedition': the expedition is active, but its slots are not restored"));
            var session = Session.Create(m_Kit.Global, save, configure => configure.Container.Add(new CardTrace()));
            try
            {
                var expedition = session.Get<ExpeditionSystem>();
                Assert.IsFalse(expedition.IsActive);
                Assert.AreEqual(0, expedition.TurnsLeft);
                Assert.IsNull(expedition.Variables);
            }
            finally
            {
                session.Dispose();
            }
        }

        [Test]
        public void SaveAndLoad_AfterEnd_NoExpedition()
        {
            Open();
            var hero = Start();
            Place(m_Kit.Exit, "a2");
            Assert.IsTrue(Play(hero, "a2"));

            var loaded = new CardSession(m_Kit, m_Kit.Reload(m_Cards.Session));
            m_Others.Add(loaded);

            Assert.IsFalse(loaded.Expedition.IsActive);
            Assert.IsNull(loaded.Slots.Expedition);
            Assert.That(loaded.Slots.Inventory.Entities.Select(card => card.Model.Id), Is.EqualTo(new[] { "Lucia" }));
        }

#endregion
    }
}
