using System.Linq;
using NUnit.Framework;
using TheGrowth.Model.Cards;
using TheGrowth.Model.Slots;

namespace TheGrowth.Tests.EditMode.Cards
{
    /// <summary>
    /// Скрипты примеров контента (<c>Content/Entities/*.script</c>, <c>Content/Scripts/Monsters.script</c>) на поле 3 × 3:
    /// монстр (заряды, шаг к герою в обход монстров и выхода, шаг на героя), ресурс (герой накрыл — в рюкзак), герой
    /// (открывает карты вокруг и под собой, <c>AllowedMoves</c>), выход (<c>SpawnSlot</c>).
    /// </summary>
    public sealed class CardContentTests
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

        private CardSystem Cards => m_Cards.Cards;

#region Monster

        [Test]
        public void Monster_WithCharges_SpendsChargeAndStays()
        {
            m_Cards.PlaceHero("a2");
            var monster = m_Cards.Place(m_Kit.Monster, "c3");

            for (var charges = 2; charges >= 0; charges--)
            {
                CardSession.Complete(Cards.InvokeHook(monster, CardHooks.OnStep));
                Assert.AreEqual(charges, m_Cards.Charges(monster));
                Assert.AreSame(m_Cards.Cell("c3"), m_Cards.Slots.GetSlot(monster));
            }
        }

        [Test]
        public void Monster_WithoutCharges_StepsToHero()
        {
            m_Cards.PlaceHero("a2");
            var monster = m_Cards.Place(m_Kit.Monster, "c3");
            m_Cards.Run(monster, "set [active card].charges to 0");

            // Около c3: b3 и c2, по 2 шага до героя на a2; b3 ближе по позиции.
            CardSession.Complete(Cards.InvokeHook(monster, CardHooks.OnStep));

            Assert.AreSame(m_Cards.Cell("b3"), m_Cards.Slots.GetSlot(monster));
            Assert.AreEqual(0, m_Cards.Charges(monster));
        }

        [Test]
        public void Monster_AvoidsCellsWithMonsterOrExit()
        {
            m_Cards.PlaceHero("a2");
            var monster = m_Cards.Place(m_Kit.Monster, "c3");
            m_Cards.Place(m_Kit.Exit, "b3");
            m_Cards.Run(monster, "set [active card].charges to 0");

            CardSession.Complete(Cards.InvokeHook(monster, CardHooks.OnStep));
            Assert.AreSame(m_Cards.Cell("c2"), m_Cards.Slots.GetSlot(monster), "b3 has the exit");

            var other = m_Cards.Place(m_Kit.Monster, "c3");
            m_Cards.Run(other, "set [active card].charges to 0");
            m_Cards.Place(m_Kit.Monster, "b3");
            CardSession.Complete(Cards.InvokeHook(other, CardHooks.OnStep));
            Assert.AreSame(m_Cards.Cell("c3"), m_Cards.Slots.GetSlot(other), "b3 and c2 have monsters: nowhere to go");
        }

        [Test]
        public void Monster_AvoidsObstacle()
        {
            m_Cards.PlaceHero("a2");
            var monster = m_Cards.Place(m_Kit.Monster, "c3");
            m_Cards.Place(m_Kit.Rock, "b3");
            m_Cards.Run(monster, "set [active card].charges to 0");

            CardSession.Complete(Cards.InvokeHook(monster, CardHooks.OnStep));

            Assert.AreSame(m_Cards.Cell("c2"), m_Cards.Slots.GetSlot(monster), "b3 has an obstacle");
        }

        [Test]
        public void Monster_NoHeroReference_StaysInPlace()
        {
            m_Cards.Place(m_Kit.Hero, "a2");
            var monster = m_Cards.Place(m_Kit.Monster, "c3");
            m_Cards.Run(monster, "set [active card].charges to 0");

            CardSession.Complete(Cards.InvokeHook(monster, CardHooks.OnStep));

            Assert.AreSame(m_Cards.Cell("c3"), m_Cards.Slots.GetSlot(monster), "a card tagged Hero is not the hero of the expedition");
        }

        [Test]
        public void Monster_StepsOntoHero_LiesOnTop()
        {
            var hero = m_Cards.PlaceHero("b2");
            var monster = m_Cards.Place(m_Kit.Monster, "b3");
            m_Cards.Run(monster, "set [active card].charges to 0");

            CardSession.Complete(Cards.InvokeHook(monster, CardHooks.OnStep));

            Assert.That(m_Cards.Cell("b2").Entities, Is.EqualTo(new[] { hero, monster }));
        }

        [Test]
        public void Monster_NoHero_StaysInPlace()
        {
            var monster = m_Cards.Place(m_Kit.Monster, "b2");
            m_Cards.Run(monster, "set [active card].charges to 0");

            CardSession.Complete(Cards.InvokeHook(monster, CardHooks.OnStep));

            Assert.AreSame(m_Cards.Cell("b2"), m_Cards.Slots.GetSlot(monster));
        }

#endregion

#region Resource

        [Test]
        public void Herb_CoveredByHero_GoesToBackpack()
        {
            var hero = m_Cards.PlaceHero("a1");
            var herb = m_Cards.Place(m_Kit.Herb, "a2");

            CardSession.Complete(Cards.Move(hero, m_Cards.Cell("a2")));

            Assert.AreSame(m_Cards.Slots.Expedition.Backpack, m_Cards.Slots.GetSlot(herb));
            Assert.AreEqual("backpack:[Herb] a2:[Hero]", m_Cards.Describe());
        }

        [Test]
        public void Herb_CoveredByMonster_Stays()
        {
            m_Cards.PlaceHero("a2");
            var herb = m_Cards.Place(m_Kit.Herb, "b3");
            var monster = m_Cards.Place(m_Kit.Monster, "c3");
            m_Cards.Run(monster, "set [active card].charges to 0");

            CardSession.Complete(Cards.InvokeHook(monster, CardHooks.OnStep));

            Assert.That(m_Cards.Cell("b3").Entities, Is.EqualTo(new[] { herb, monster }));
        }

#endregion

#region Hero

        [Test]
        public void Hero_Placed_RevealsCardsAroundAndUnder()
        {
            var hero = m_Cards.PlaceHero("a1");
            var under = m_Cards.Place(m_Kit.Monster, "b2", false);
            var around = new[] { "a2", "b1", "b3", "c2" }.Select(cell => m_Cards.Place(m_Kit.Exit, cell, false)).ToArray();
            var far = m_Cards.Place(m_Kit.Exit, "c3", false);

            CardSession.Complete(Cards.Move(hero, m_Cards.Cell("b2")));

            Assert.IsTrue(CardSession.IsFaceUp(under), "the card under the hero");
            Assert.IsTrue(around.All(CardSession.IsFaceUp), "the cards around the hero");
            Assert.IsFalse(CardSession.IsFaceUp(far));
        }

        [Test]
        public void Hero_PlacedFirstTime_RevealsAround()
        {
            var hero = m_Cards.PlaceHero("a1");
            var near = m_Cards.Place(m_Kit.Herb, "a2", false);

            CardSession.Complete(Cards.InvokeHook(hero, CardHooks.OnPlacedFirstTime));

            Assert.IsTrue(CardSession.IsFaceUp(near));
        }

        [Test]
        public void Hero_AllowedMoves_CellsAroundWithCards_ExceptMonstersAndObstacles()
        {
            var hero = m_Cards.PlaceHero("b2");
            m_Cards.Place(m_Kit.Herb, "a2");
            m_Cards.Place(m_Kit.Monster, "b1");
            m_Cards.Place(m_Kit.Rock, "b3");
            m_Cards.Place(m_Kit.Exit, "c3");

            // c2 свободна, c3 не рядом.
            Assert.IsTrue(Cards.TryEvaluate<SlotSet>(hero, CardProperties.AllowedMoves, out var moves));
            Assert.That(moves.Select(CardTrace.Name), Is.EqualTo(new[] { "a2" }));

            m_Cards.Place(m_Kit.Exit, "c2");
            Assert.IsTrue(Cards.TryEvaluate(hero, CardProperties.AllowedMoves, out moves));
            Assert.That(moves.Select(CardTrace.Name), Is.EqualTo(new[] { "a2", "c2" }));
        }

        [Test]
        public void Hero_AllowedMoves_MonsterUnderOtherCard_Excluded()
        {
            var hero = m_Cards.PlaceHero("a1");
            m_Cards.Place(m_Kit.Monster, "a2");
            m_Cards.Place(m_Kit.Herb, "a2");
            m_Cards.Place(m_Kit.Herb, "b1");

            // Отличие от прототипа: там учитывались только верхние карты (OnTop) и a2 с травой сверху была бы доступна.
            Assert.IsTrue(Cards.TryEvaluate<SlotSet>(hero, CardProperties.AllowedMoves, out var moves));
            Assert.That(moves.Select(CardTrace.Name), Is.EqualTo(new[] { "b1" }));
        }

#endregion

#region Exit

        [Test]
        public void Exit_SpawnSlot_SlotsTaggedExit_RevealedWhenPlacedFirstTime()
        {
            var exit = m_Cards.Place(m_Kit.Exit, m_Cards.Slots.Expedition.Deck, false);

            Assert.IsTrue(Cards.TryEvaluate<SlotSet>(exit, CardProperties.SpawnSlot, out var spawn));
            Assert.That(spawn.Select(CardTrace.Name), Is.EqualTo(new[] { "c3" }));

            m_Cards.Slots.Move(exit, spawn[0]);
            CardSession.Complete(Cards.InvokeHook(exit, CardHooks.OnPlacedFirstTime));
            Assert.IsTrue(CardSession.IsFaceUp(exit));
        }

#endregion
    }
}
