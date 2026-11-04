using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Core.Entities;
using TheGrowth.Model.Decks;
using TheGrowth.Model.Entities;
using TheGrowth.Model.Slots;
using TheGrowth.Tests.EditMode.Cards;
using TheGrowth.Tests.EditMode.Scripting;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Decks
{
    /// <summary>
    /// Начальная раскладка (<see cref="DealSystem"/>) на поле 3 × 3 из <see cref="CardTestKit"/> (клетки a1..c3 и x; a1 —
    /// <c>PlayerSpawn</c>, c3 — <c>Exit</c>): повторяемость по seed и после загрузки, «не раскладывать», карт больше и меньше
    /// клеток, приоритет <c>SpawnSlot</c>, клетки <c>PlayerSpawn</c>, герой, <c>OnPlacedFirstTime</c>, запуск исполнителя.
    /// </summary>
    public sealed class DealTests
    {
        private CardTestKit m_Kit;
        private readonly List<CardSession> m_Sessions = new List<CardSession>();

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var session in m_Sessions)
                session.Dispose();

            m_Sessions.Clear();
            m_Kit.Dispose();
        }

        private CardSession Open(ulong seed = CardTestKit.DefaultSeed)
        {
            var cards = new CardSession(m_Kit, seed);
            m_Sessions.Add(cards);
            return cards;
        }

        /// <summary>Карта героя в инвентаре.</summary>
        private Entity Hero(CardSession cards, EntityModel model = null)
        {
            return cards.Slots.Create(model ?? m_Kit.Hero, cards.Slots.Inventory);
        }

        private Entity DealHero(CardSession cards, EntityModel model = null)
        {
            var hero = Hero(cards, model);
            CardSession.Complete(cards.Dealer.Deal(hero));
            return hero;
        }

        private string DealLayout(ulong seed)
        {
            var cards = Open(seed);
            DealHero(cards);
            return cards.Describe();
        }

        private static IEnumerable<Slot> Cells(CardSession cards)
        {
            return cards.Slots.Expedition.Field.Cells;
        }

        private static string[] Names(Slot slot)
        {
            return slot.Entities.Select(CardTrace.Name).ToArray();
        }

        private void MixedDeck()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Alpha), new DeckEntry(m_Kit.Beta), new DeckEntry(m_Kit.Gamma), new DeckEntry(m_Kit.Herb, 2),
                new DeckEntry(m_Kit.Monster, 2), new DeckEntry(m_Kit.Exit), new DeckEntry(m_Kit.Plain, 6));
        }

#region Repeatability

        [Test]
        public void SameSeed_SameLayout_OtherSeeds_OtherLayouts()
        {
            MixedDeck();

            var layout = DealLayout(1);

            Assert.AreEqual(layout, DealLayout(1));
            var layouts = new ulong[] { 2, 3, 4, 5 }.Select(DealLayout).Append(layout).Distinct().Count();
            Assert.That(layouts, Is.GreaterThan(1), "five seeds give the same layout");
        }

        [Test]
        public void Deck_Shuffled_BySeed()
        {
            m_Kit.Deck(new[] { m_Kit.Alpha, m_Kit.Beta, m_Kit.Gamma, m_Kit.Plain, m_Kit.Rock }.Select(model => new DeckEntry(model, 1, true)).ToArray());

            var orders = new ulong[] { 1, 2, 3, 4, 5, 6 }.Select(seed =>
            {
                var cards = Open(seed);
                DealHero(cards);
                return Names(cards.Slots.Expedition.Deck);
            }).ToList();

            Assert.IsTrue(orders.All(order => order.OrderBy(name => name).SequenceEqual(new[] { "Alpha", "Beta", "Gamma", "Plain", "Rock" })));
            Assert.That(orders.Select(order => string.Join(",", order)).Distinct().Count(), Is.GreaterThan(1), "the deck is not shuffled");
        }

        [Test]
        public void SaveAndLoad_AfterDeal_SameLayout_SameNextRandomResults()
        {
            MixedDeck();
            var cards = Open(9);
            DealHero(cards);

            var loaded = new CardSession(m_Kit, m_Kit.Reload(cards.Session));
            m_Sessions.Add(loaded);

            Assert.AreEqual(cards.Describe(), loaded.Describe());
            Assert.AreEqual(cards.Slots.GetSlot(cards.Slots.Expedition.Hero).Id, loaded.Slots.GetSlot(loaded.Slots.Expedition.Hero).Id);
            var lines = Enumerable.Repeat(new[]
            {
                "trace slot [random slot of [field slots]]",
                "trace [random card of [cards in [field slots]]] random",
                "if [chance 50%]\n        trace [hero] chance",
            }, 10).SelectMany(group => group).ToArray();
            Assert.That(loaded.RunTrace(null, lines), Is.EqualTo(cards.RunTrace(null, lines)));
        }

#endregion

#region Cards and cells

        [Test]
        public void KeepInDeck_StaysInDeck()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Alpha, 2, true), new DeckEntry(m_Kit.Beta));
            var cards = Open();

            DealHero(cards);

            Assert.That(Names(cards.Slots.Expedition.Deck), Is.EqualTo(new[] { "Alpha", "Alpha" }));
            Assert.AreEqual(1, Cells(cards).Count(cell => cell.Entities.Any(card => card.Model == m_Kit.Beta)));
            Assert.IsFalse(Cells(cards).Any(cell => cell.Entities.Any(card => card.Model == m_Kit.Alpha)));
        }

        [Test]
        public void MoreCardsThanCells_EveryCellFilled_RestInDeck()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Plain, 20));
            var cards = Open();

            var hero = DealHero(cards);

            // Старый баг T022: при достаточном числе карт клетки оставались пустыми.
            foreach (var cell in Cells(cards))
            {
                if (cell == cards.Cell("a1"))
                    Assert.That(cell.Entities, Is.EqualTo(new[] { hero }));
                else
                    Assert.That(Names(cell), Is.EqualTo(new[] { "Plain" }), cell.Id);
            }

            Assert.AreEqual(20 - 9, cards.Slots.Expedition.Deck.Count);
        }

        [Test]
        public void FewerCardsThanCells_CellsStayEmpty()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Plain, 3));
            var cards = Open();

            DealHero(cards);

            Assert.AreEqual(3, Cells(cards).Count(cell => cell.Entities.Any(card => card.Model == m_Kit.Plain)));
            Assert.AreEqual(6, Cells(cards).Count(cell => cell.IsEmpty));
            Assert.IsTrue(cards.Slots.Expedition.Deck.IsEmpty);
        }

        [Test]
        public void NoDeck_OnlyHero()
        {
            var cards = Open();

            var hero = DealHero(cards);

            Assert.AreEqual("a1:[Hero]", cards.Describe());
            Assert.AreSame(hero, cards.Slots.Expedition.Hero);
        }

        [Test]
        public void SpawnSlot_First_ExitOnExitCell([Values(1UL, 2UL, 3UL, 4UL, 5UL, 6UL)] ulong seed)
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Monster, 20), new DeckEntry(m_Kit.Exit));
            var cards = Open(seed);

            DealHero(cards);

            // Старый баг T026: монстр ложился на клетку выхода.
            Assert.That(Names(cards.Cell("c3")), Is.EqualTo(new[] { "Exit" }));
            Assert.IsTrue(CardSession.IsFaceUp(cards.Cell("c3").Entities[0]), "the exit reveals itself in OnPlacedFirstTime");
        }

        [Test]
        public void SpawnSlot_NoFreeSlot_StaysInDeck_Warning()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Exit, 2));
            var cards = Open();

            LogAssert.Expect(LogType.Warning, new Regex("Exit.* has no free field slot in its SpawnSlot .*the card stays in the deck"));
            DealHero(cards);

            Assert.That(Names(cards.Cell("c3")), Is.EqualTo(new[] { "Exit" }));
            Assert.That(Names(cards.Slots.Expedition.Deck), Is.EqualTo(new[] { "Exit" }));
        }

        [Test]
        public void OccupiedCells_NoCardOnCard()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Exit), new DeckEntry(m_Kit.Plain, 20));
            var cards = Open();
            cards.Place(m_Kit.Rock, "c3");

            LogAssert.Expect(LogType.Warning, new Regex("has no free field slot in its SpawnSlot"));
            DealHero(cards);

            Assert.IsTrue(Cells(cards).All(cell => cell.Count == 1), cards.Describe());
            Assert.That(Names(cards.Cell("c3")), Is.EqualTo(new[] { "Rock" }));
            Assert.That(Names(cards.Slots.Expedition.Deck), Does.Contain("Exit"));
        }

#endregion

#region Hero

        [Test]
        public void Hero_Last_OnPlayerSpawn_FaceUp_SetHero_CardsFaceDown()
        {
            var leader = m_Kit.Model("Leader", CardTestKit.TracerScript);
            m_Kit.Deck(new DeckEntry(m_Kit.Plain, 4));
            var cards = Open();
            var hero = Hero(cards, leader);
            hero.GetRequired<CardBrain>().SetFaceUp(false);

            CardSession.Complete(cards.Dealer.Deal(hero));

            Assert.AreSame(cards.Cell("a1"), cards.Slots.GetSlot(hero));
            Assert.IsTrue(CardSession.IsFaceUp(hero));
            Assert.AreSame(hero, cards.Slots.Expedition.Hero);
            Assert.IsTrue(cards.Slots.Inventory.IsEmpty);
            Assert.IsFalse(Cells(cards).SelectMany(cell => cell.Entities).Where(card => card != hero).Any(CardSession.IsFaceUp), "dealt from the deck face down");
        }

        [Test]
        public void OrdinaryCards_SkipPlayerSpawn_HeroOnRandomSpawn()
        {
            m_Kit.Graph.FindSlot("b2").TagList.Add("PlayerSpawn");
            m_Kit.Deck(new DeckEntry(m_Kit.Plain, 20));
            var heroCells = new HashSet<string>();

            for (var seed = 1UL; seed <= 12; seed++)
            {
                var cards = Open(seed);
                var hero = DealHero(cards);
                var spawns = new[] { cards.Cell("a1"), cards.Cell("b2") };
                Assert.That(spawns.Select(cell => cell.Count), Is.EquivalentTo(new[] { 0, 1 }), cards.Describe());
                Assert.That(spawns, Does.Contain(cards.Slots.GetSlot(hero)));
                Assert.IsTrue(Cells(cards).Except(spawns).All(cell => Names(cell).SequenceEqual(new[] { "Plain" })), cards.Describe());
                heroCells.Add(cards.Slots.GetSlot(hero).Id);
            }

            Assert.AreEqual(2, heroCells.Count, "the hero cell is random among free PlayerSpawn cells");
        }

        [Test]
        public void SpawnSlotOnPlayerSpawn_DoesNotTakeHeroCell()
        {
            var ghost = m_Kit.Model("Ghost", "SpawnSlot -> [slots tagged PlayerSpawn]\n");
            m_Kit.Graph.FindSlot("b2").TagList.Add("PlayerSpawn");
            m_Kit.Deck(new DeckEntry(ghost));

            foreach (var seed in new ulong[] { 1, 2, 3, 4, 5, 6 })
            {
                var cards = Open(seed);
                DealHero(cards);
                Assert.That(new[] { cards.Cell("a1"), cards.Cell("b2") }.SelectMany(Names), Is.EquivalentTo(new[] { "Hero", "Ghost" }), cards.Describe());
            }
        }

        [Test]
        public void NoFreePlayerSpawn_Error_NothingChanged()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Alpha, 2));
            var cards = Open();
            cards.Place(m_Kit.Plain, "a1");
            var hero = Hero(cards);

            var probe = Probe.Watch(cards.Dealer.Deal(hero));

            Assert.IsTrue(probe.IsCompleted);
            Assert.IsInstanceOf<InvalidOperationException>(probe.Exception);
            StringAssert.Contains("no free field slot tagged PlayerSpawn for the hero", probe.Exception.Message);
            Assert.IsTrue(cards.Slots.Expedition.Deck.IsEmpty);
            Assert.AreSame(cards.Slots.Inventory, cards.Slots.GetSlot(hero));
            Assert.IsNull(cards.Slots.Expedition.Hero);
            Assert.AreEqual(0, cards.Runner.ActiveCount);
        }

        [Test]
        public void NoExpedition_Error()
        {
            var cards = Open();
            cards.Slots.DestroyExpedition();

            var probe = Probe.Watch(cards.Dealer.Deal(Hero(cards)));

            Assert.IsInstanceOf<InvalidOperationException>(probe.Exception);
            StringAssert.Contains("there are no expedition slots", probe.Exception.Message);
        }

#endregion

#region Hooks and runner

        [Test]
        public void OnPlacedFirstTime_InDealOrder_HeroLast_NoPlacementHooks()
        {
            var leader = m_Kit.Model("Leader", CardTestKit.TracerScript);
            var gate = m_Kit.Model("Gate", "SpawnSlot -> [slots tagged Exit]\n\n" + CardTestKit.TracerScript);
            m_Kit.Deck(new DeckEntry(m_Kit.Alpha), new DeckEntry(m_Kit.Beta), new DeckEntry(m_Kit.Gamma), new DeckEntry(gate));
            var cards = Open();
            var hero = Hero(cards, leader);
            var dealt = new List<Entity>();
            cards.Slots.Moved += moved =>
            {
                if (moved.From == cards.Slots.Expedition.Deck && moved.To.GraphSlot != null)
                    dealt.Add(moved.Entity);
            };
            cards.Trace.Clear();

            CardSession.Complete(cards.Dealer.Deal(hero));

            Assert.AreEqual(4, dealt.Count);
            Assert.AreSame(gate, dealt[0].Model, "cards with SpawnSlot are dealt first");
            Assert.That(cards.Trace.Entries, Is.EqualTo(dealt.Append(hero).Select(card => $"{card.Model.Id}: OnPlacedFirstTime")));
            Assert.AreEqual(5, cards.Trace.Executions.Distinct().Count(), "each hook is its own run");
        }

        [Test]
        public void Deal_IsOneRun_NoSavePointUntilEnd()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Alpha), new DeckEntry(m_Kit.Beta), new DeckEntry(m_Kit.Exit));
            var cards = Open();
            var hero = Hero(cards);
            var idle = 0;
            var busy = true;
            var moves = 0;
            cards.Runner.BecameIdle += () => idle++;
            cards.Slots.Moved += _ =>
            {
                moves++;
                busy &= !cards.Runner.IsIdle && cards.Runner.ActiveExecutions.Any(execution => execution.Name == "deal Field");
            };

            CardSession.Complete(cards.Dealer.Deal(hero));

            Assert.That(moves, Is.GreaterThan(3));
            Assert.IsTrue(busy, "the runner is busy while cards are dealt");
            Assert.AreEqual(1, idle, "hooks run inside the deal: the only save point is after it");
            Assert.IsTrue(cards.Runner.IsIdle);
            Assert.AreEqual(0, cards.Runner.ActiveCount);
        }

#endregion
    }
}
