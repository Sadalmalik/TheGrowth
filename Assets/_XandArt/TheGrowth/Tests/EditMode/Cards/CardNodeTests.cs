using NUnit.Framework;
using TheGrowth.Core.Entities;

namespace TheGrowth.Tests.EditMode.Cards
{
    /// <summary>
    /// Эвалуаторы, условия и команды карт на поле 3 × 3 (<see cref="CardTestKit"/>): порядок наборов (слоты графа, затем
    /// индекс в слоте; операции — порядок левого операнда), близость по графу, пустые значения.
    /// </summary>
    public sealed class CardNodeTests
    {
        private CardTestKit m_Kit;
        private CardSession m_Cards;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit();
            m_Cards = new CardSession(m_Kit);
        }

        [TearDown]
        public void TearDown()
        {
            m_Cards.Dispose();
            m_Kit.Dispose();
        }

        private string[] Trace(Entity active, params string[] lines)
        {
            return m_Cards.RunTrace(active, lines);
        }

        /// <summary>a2: Monster, Herb (сверху); b2: Hero; c1: Herb; c3: Exit.</summary>
        private Entity Layout()
        {
            m_Cards.Place(m_Kit.Monster, "a2");
            m_Cards.Place(m_Kit.Herb, "a2");
            var hero = m_Cards.Place(m_Kit.Hero, "b2");
            m_Cards.Place(m_Kit.Herb, "c1");
            m_Cards.Place(m_Kit.Exit, "c3");
            return hero;
        }

#region Sets

        [Test]
        public void CardSets_SlotOrderThenIndex_OperationsKeepLeftOrder()
        {
            var hero = Layout();

            Assert.That(Trace(hero,
                "trace cards [cards in [field slots]]",
                "trace cards [cards tagged Resource]",
                "trace cards [[cards in [field slots]] except [cards tagged Resource]]",
                "trace cards [[cards tagged Exit] or [cards tagged Monster]]",
                "trace cards [[cards in [field slots]] and [[cards tagged Exit] or [cards tagged Hero]]]",
                "trace cards [cards in [slots around [active card] within 1]]",
                "trace cards [active card]"), Is.EqualTo(new[]
            {
                "cards: Monster,Herb,Hero,Herb,Exit",
                "cards: Herb,Herb",
                "cards: Monster,Hero,Exit",
                "cards: Exit,Monster",
                "cards: Hero,Exit",
                "cards: Monster,Herb",
                "cards: Hero",
            }));
        }

        [Test]
        public void SlotSets_GraphOrder_OperationsKeepLeftOrder()
        {
            var hero = Layout();

            Assert.That(Trace(hero,
                "trace slots [field slots]",
                "trace slots [free slots]",
                "trace slots [slots tagged Exit]",
                "trace slots [slots around [active card] within 1]",
                "trace slots [slots around [active card] within 2]",
                "trace slots [slots of [cards tagged Resource]]",
                "trace slots [[field slots] and [slots tagged PlayerSpawn]]",
                "trace slots [[slots tagged Exit] or [free slots]]",
                "trace slots [[slots around [active card] within 1] except [slots of [cards tagged Monster]]]",
                "trace slots [slot of [active card]]"), Is.EqualTo(new[]
            {
                "slots: a1,a2,a3,b1,b2,b3,c1,c2,c3,x",
                "slots: a1,a3,b1,b3,c2,x",
                "slots: c3",
                "slots: a2,b1,b3,c2",
                "slots: a1,a2,a3,b1,b3,c1,c2,c3",
                "slots: a2,c1",
                "slots: a1",
                "slots: c3,a1,a3,b1,b3,c2,x",
                "slots: b1,b3,c2",
                "slots: b2",
            }));
        }

        [Test]
        public void SlotsAround_NotOnFieldOrIsolated_Empty()
        {
            var inHand = m_Cards.Place(m_Kit.Alpha, m_Cards.Slots.Expedition.Hand);
            var isolated = m_Cards.Place(m_Kit.Beta, "x");

            Assert.That(Trace(inHand, "trace slots [slots around [active card] within 1]"), Is.EqualTo(new[] { "slots: " }));
            Assert.That(Trace(isolated, "trace slots [slots around [active card] within 3]", "trace slots [slots around [active card] within 0]"),
                Is.EqualTo(new[] { "slots: ", "slots: " }));
        }

#endregion

#region Cards and slots

        [Test]
        public void CardEvaluators()
        {
            Layout();
            var herb = m_Cards.Cell("a2").Entities[1];

            Assert.That(Trace(herb,
                "trace [active card] active",
                "trace [card under [active card]] under",
                "trace [card under [card under [active card]]] under2",
                "trace [top card of [slot of [hero]]] top",
                "trace [top card of [nearest of [free slots] to [active card]]] free",
                "trace [hero] hero",
                "trace [covering card] covering",
                "trace [iterating card] iterating"), Is.EqualTo(new[]
            {
                "Herb: active",
                "Monster: under",
                "none: under2",
                "Hero: top",
                "none: free",
                "Hero: hero",
                "none: covering",
                "none: iterating",
            }));
        }

        [Test]
        public void SlotEvaluators()
        {
            var hero = Layout();

            Assert.That(Trace(hero,
                "trace slot [deck]",
                "trace slot [hand]",
                "trace slot [discard]",
                "trace slot [backpack]",
                "trace slot [inventory]",
                "trace slot [slot of [covering card]]"), Is.EqualTo(new[]
            {
                "slot: deck", "slot: hand", "slot: discard", "slot: backpack", "slot: inventory", "slot: none",
            }));
        }

        [Test]
        public void Hero_FirstCardTaggedHero_InExpeditionSlots()
        {
            var deckHero = m_Cards.Place(m_Kit.Hero, m_Cards.Slots.Expedition.Deck);
            m_Cards.Place(m_Kit.Hero, "a1");

            Assert.That(Trace(null, "trace [hero] hero", "trace slot [slot of [hero]]"), Is.EqualTo(new[] { "Hero: hero", "slot: deck" }));

            m_Cards.Entities.Destroy(deckHero);
            Assert.That(Trace(null, "trace slot [slot of [hero]]"), Is.EqualTo(new[] { "slot: a1" }));
        }

        [Test]
        public void Nearest_ByGraphSteps()
        {
            m_Cards.Place(m_Kit.Hero, "a2");
            var monster = m_Cards.Place(m_Kit.Monster, "c2");

            // Около монстра: b2 (1 шаг до героя), c1 и c3 (по 3).
            Assert.That(Trace(monster, "trace slot [nearest of [slots around [active card] within 1] to [hero]]"), Is.EqualTo(new[] { "slot: b2" }));
        }

        [Test]
        public void Nearest_EqualSteps_ByDistanceOfPositions()
        {
            m_Cards.Place(m_Kit.Hero, "a1");
            var monster = m_Cards.Place(m_Kit.Monster, "b3");

            // a3 и b2 — по 2 шага до героя; b2 ближе по позиции (√2 против 2), хотя в наборе a3 раньше.
            Assert.That(Trace(monster, "trace slot [nearest of [slots around [active card] within 1] to [hero]]"), Is.EqualTo(new[] { "slot: b2" }));
        }

        [Test]
        public void Nearest_EqualStepsAndDistance_ByOrderOfSet()
        {
            m_Cards.Place(m_Kit.Hero, "a1");
            var monster = m_Cards.Place(m_Kit.Monster, "c3");

            // b3 и c2 — по 3 шага и √5 до героя: первый в наборе.
            Assert.That(Trace(monster,
                "trace slot [nearest of [slots around [active card] within 1] to [hero]]",
                "trace slot [nearest of [[slots around [active card] within 1] except [slots around [hero] within 0]] to [active card]]"),
                Is.EqualTo(new[] { "slot: b3", "slot: b3" }));
        }

        [Test]
        public void Nearest_IncludesOwnSlot_SkipsUnreachable()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, "b2");
            var isolated = m_Cards.Place(m_Kit.Beta, "x");
            var inHand = m_Cards.Place(m_Kit.Gamma, m_Cards.Slots.Expedition.Hand);

            Assert.That(Trace(alpha, "trace slot [nearest of [field slots] to [active card]]"), Is.EqualTo(new[] { "slot: b2" }));
            Assert.That(Trace(isolated, "trace slot [nearest of [slots tagged Exit] to [active card]]"), Is.EqualTo(new[] { "slot: none" }));
            Assert.That(Trace(inHand, "trace slot [nearest of [field slots] to [active card]]"), Is.EqualTo(new[] { "slot: none" }));
        }

#endregion

#region Conditions and numbers

        [Test]
        public void Conditions()
        {
            var hero = Layout();

            Assert.That(Trace(hero,
                "if [[active card] has tag Hero]",
                "    trace [active card] card tag",
                "if [[slot of [active card]] has tag PlayerSpawn]",
                "    trace [active card] wrong slot tag",
                "if [[nearest of [slots tagged Exit] to [active card]] has tag Exit]",
                "    trace [active card] slot tag",
                "if [[slot of [active card]] is empty]",
                "    trace [active card] wrong empty",
                "if [[nearest of [free slots] to [active card]] is empty]",
                "    trace [active card] empty",
                "if [[active card] == [hero]]",
                "    trace [active card] equal",
                "if [[slot of [active card]] != [slot of [card under [top card of [nearest of [slots tagged Exit] to [hero]]]]]]",
                "    trace [active card] wrong not equal of empty",
                "if [[count of [cards tagged Resource]] == 2]",
                "    trace [active card] count",
                "if [[count of slots in [free slots]] == 6]",
                "    trace [active card] count slots"), Is.EqualTo(new[]
            {
                "Hero: card tag",
                "Hero: slot tag",
                "Hero: empty",
                "Hero: equal",
                "Hero: count",
                "Hero: count slots",
            }));
        }

        [Test]
        public void EmptyValues_ConditionsFalse_ExistsChecks()
        {
            var hero = Layout();

            Assert.That(Trace(hero,
                "if [[covering card] has tag Hero]",
                "    trace [active card] wrong tag",
                "if [[slot of [covering card]] is empty]",
                "    trace [active card] wrong empty",
                "if [[covering card] == [covering card]]",
                "    trace [active card] wrong equal",
                "if [[covering card] != [active card]]",
                "    trace [active card] wrong not equal",
                "if [not [[covering card] exists]]",
                "    trace [active card] no covering",
                "if [[active card] exists]",
                "    trace [active card] active exists",
                "if [[slot of [active card]] exists]",
                "    trace [active card] slot exists"), Is.EqualTo(new[]
            {
                "Hero: no covering",
                "Hero: active exists",
                "Hero: slot exists",
            }));
        }

#endregion

#region Commands

        [Test]
        public void TurnCommands()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");

            Assert.That(Trace(alpha, "hide [active card]", "hide [active card]", "flip [active card]", "reveal [active card]", "flip [active card]"),
                Is.EqualTo(new[] { "Alpha: OnFlipped", "Alpha: OnFlipped", "Alpha: OnFlipped" }));
            Assert.IsFalse(CardSession.IsFaceUp(alpha));
        }

        [Test]
        public void CreateAndDestroy()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");

            Assert.That(Trace(alpha, "create Beta in [slot of [active card]]", "destroy [top card of [slot of [active card]]]"), Is.EqualTo(new[]
            {
                "Alpha: OnCovered <- Beta",
                "Beta: OnPlaced",
                "Alpha: OnUnCovered <- Beta",
            }));
            Assert.AreEqual("a1:[Alpha]", m_Cards.Describe());

            Trace(alpha, "create Herb in [inventory]", "create Herb in [backpack]");
            Assert.AreEqual(1, m_Cards.Slots.Inventory.Count);
            Assert.AreEqual("backpack:[Herb] a1:[Alpha]", m_Cards.Describe());
        }

        [Test]
        public void ForEach_IteratingCard_SkipsDestroyed()
        {
            Layout();

            Assert.That(Trace(null,
                "for each card in [cards tagged Resource]",
                "    trace [iterating card] it",
                "    for each card in [cards tagged Resource]",
                "        destroy [iterating card]",
                "for each card in [cards tagged Monster]",
                "    trace [iterating card] monster",
                "    trace [iterating card] again"), Is.EqualTo(new[] { "Herb: it", "Monster: monster", "Monster: again" }));
        }

        [Test]
        public void EmptyValues_CommandsDoNothing()
        {
            var hero = Layout();
            var before = m_Cards.Describe();

            Assert.IsEmpty(Trace(hero,
                "move [covering card] to [hand]",
                "move [active card] to [slot of [covering card]]",
                "flip [card under [active card]]",
                "reveal [iterating card]",
                "hide [covering card]",
                "destroy [card under [active card]]",
                "create Herb in [nearest of [free slots] to [covering card]]",
                "for each card in [cards tagged PlayerSpawn]",
                "    destroy [iterating card]"));
            Assert.AreEqual(before, m_Cards.Describe());
        }

        [Test]
        public void NoExpedition_EmptySlotsAndSets()
        {
            var card = m_Cards.Place(m_Kit.Alpha, m_Cards.Slots.Inventory);
            m_Cards.Slots.DestroyExpedition();

            Assert.That(Trace(card,
                "trace slot [deck]",
                "trace slot [inventory]",
                "trace slots [field slots]",
                "trace cards [cards tagged Hero]",
                "trace [hero] hero",
                "move [active card] to [hand]",
                "create Herb in [backpack]"), Is.EqualTo(new[] { "slot: none", "slot: inventory", "slots: ", "cards: ", "none: hero" }));
            Assert.AreEqual(1, m_Cards.Slots.Inventory.Count);
        }

#endregion
    }
}
