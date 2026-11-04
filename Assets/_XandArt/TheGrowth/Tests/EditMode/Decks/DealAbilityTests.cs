using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Entities;
using TheGrowth.Model.Decks;
using TheGrowth.Model.Entities;
using TheGrowth.Model.Heroes;
using TheGrowth.Model.Slots;
using TheGrowth.Tests.EditMode.Cards;
using TheGrowth.Tests.EditMode.Scripting;

namespace TheGrowth.Tests.EditMode.Decks
{
    /// <summary>
    /// Раскладка со способностями (<see cref="DealSystem.Deal"/>): способности перемещаются в руку после раскладки поля и до
    /// <c>OnPlacedFirstTime</c>, без своих хуков; способность другого персонажа, больше ёмкости руки, не карта, уничтожена, без
    /// компонента способности, повтор, герой — ошибка до любых изменений.
    /// </summary>
    public sealed class DealAbilityTests
    {
        private CardTestKit m_Kit;
        private CharacterModel m_Lucia;
        private CharacterModel m_Stefan;
        private EntityModel m_LuciaCard;
        private EntityModel m_Vision;
        private EntityModel m_Confuse;
        private EntityModel m_Revolver;
        private EntityModel m_Talisman;
        private readonly List<CardSession> m_Sessions = new List<CardSession>();

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit();
            m_Lucia = m_Kit.Character("Lucia");
            m_Stefan = m_Kit.Character("Stefan");
            m_LuciaCard = m_Kit.HeroModel("LuciaCard", m_Lucia, CardTestKit.TracerScript);
            m_Vision = m_Kit.AbilityModel("Vision", m_Lucia, CardTestKit.TracerScript);
            m_Confuse = m_Kit.AbilityModel("Confuse", m_Lucia, CardTestKit.TracerScript);
            m_Revolver = m_Kit.AbilityModel("Revolver", m_Stefan);
            m_Talisman = m_Kit.AbilityModel("Talisman", null, CardTestKit.TracerScript);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var session in m_Sessions)
                session.Dispose();

            m_Sessions.Clear();
            m_Kit.Dispose();
        }

        private CardSession Open()
        {
            var cards = new CardSession(m_Kit);
            m_Sessions.Add(cards);
            return cards;
        }

        private static Entity InInventory(CardSession cards, EntityModel model)
        {
            return cards.Slots.Create(model, cards.Slots.Inventory, -1);
        }

        private static void AssertNothingChanged(CardSession cards, Exception exception, string message, Entity hero, params Entity[] abilities)
        {
            Assert.IsInstanceOf<InvalidOperationException>(exception);
            StringAssert.Contains(message, exception.Message);
            var expedition = cards.Slots.Expedition;
            Assert.IsTrue(expedition.Deck.IsEmpty && expedition.Hand.IsEmpty, cards.Describe());
            Assert.IsTrue(expedition.Field.Cells.All(cell => cell.IsEmpty), cards.Describe());
            Assert.IsNull(expedition.Hero);
            Assert.AreSame(cards.Slots.Inventory, cards.Slots.GetSlot(hero));
            foreach (var ability in abilities.Where(ability => !ability.IsDestroyed))
                Assert.AreNotSame(expedition.Hand, cards.Slots.GetSlot(ability));

            Assert.AreEqual(0, cards.Runner.ActiveCount);
        }

        [Test]
        public void Abilities_MovedToHand_InOrder_FaceUp_AfterFieldBeforeHooks()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Alpha), new DeckEntry(m_Kit.Beta));
            var cards = Open();
            var hero = InInventory(cards, m_LuciaCard);
            var talisman = InInventory(cards, m_Talisman);
            var vision = InInventory(cards, m_Vision);
            vision.GetRequired<CardBrain>().SetFaceUp(false);
            var created = cards.Entities.Count;
            var fieldAtHand = new List<int>();
            var heroAtHand = new List<Entity>();
            cards.Slots.Moved += moved =>
            {
                if (moved.To != cards.Slots.Expedition.Hand)
                    return;

                fieldAtHand.Add(cards.Slots.Expedition.Field.Cells.Count(cell => !cell.IsEmpty));
                heroAtHand.Add(cards.Slots.Expedition.Hero);
            };
            cards.Trace.Clear();

            CardSession.Complete(cards.Dealer.Deal(hero, new[] { vision, talisman }));

            var hand = cards.Slots.Expedition.Hand;
            Assert.That(hand.Entities, Is.EqualTo(new[] { vision, talisman }), "moved, not copied, in the given order");
            Assert.AreEqual(created + 2, cards.Entities.Count, "only the deck cards are created");
            Assert.IsTrue(cards.Slots.Inventory.IsEmpty);
            Assert.IsTrue(CardSession.IsFaceUp(vision), "the hand turns cards face up");
            Assert.That(fieldAtHand, Is.EqualTo(new[] { 3, 3 }), "the field is dealt (two cards and the hero) before the abilities");
            Assert.That(heroAtHand, Is.EqualTo(new[] { hero, hero }), "the hero is set before the abilities");
            Assert.AreEqual(3, cards.Trace.Entries.Count, cards.Trace.ToString());
            Assert.IsTrue(cards.Trace.Entries.All(entry => entry.EndsWith(": OnPlacedFirstTime")), cards.Trace.ToString());
            Assert.AreEqual("LuciaCard: OnPlacedFirstTime", cards.Trace.Entries.Last());
            Assert.IsFalse(cards.Trace.Entries.Any(entry => entry.StartsWith("Vision") || entry.StartsWith("Talisman")), "abilities have no OnPlacedFirstTime");
        }

        [Test]
        public void Hooks_SeeAbilitiesInHand()
        {
            var watcher = m_Kit.Model("Watcher", "OnPlacedFirstTime:\n    trace cards [cards in [hand]]\n");
            m_Kit.Deck(new DeckEntry(watcher));
            var cards = Open();
            var hero = InInventory(cards, m_LuciaCard);
            var vision = InInventory(cards, m_Vision);
            cards.Trace.Clear();

            CardSession.Complete(cards.Dealer.Deal(hero, new[] { vision }));

            Assert.That(cards.Trace.Entries.First(), Is.EqualTo("cards: Vision"));
        }

        [Test]
        public void PlayerAbility_FitsAnyHero_NullOrEmptyList_NoAbilities()
        {
            var cards = Open();
            var hero = InInventory(cards, m_Kit.Hero);
            var talisman = InInventory(cards, m_Talisman);

            CardSession.Complete(cards.Dealer.Deal(hero, new[] { talisman }));
            Assert.That(cards.Slots.Expedition.Hand.Entities, Is.EqualTo(new[] { talisman }));

            var other = Open();
            CardSession.Complete(other.Dealer.Deal(InInventory(other, m_Kit.Hero), Array.Empty<Entity>()));
            Assert.IsTrue(other.Slots.Expedition.Hand.IsEmpty);
        }

        [Test]
        public void AbilityOfOtherCharacter_Error_NothingChanged()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Alpha));
            var cards = Open();
            var hero = InInventory(cards, m_LuciaCard);
            var vision = InInventory(cards, m_Vision);
            var revolver = InInventory(cards, m_Revolver);

            var probe = Probe.Watch(cards.Dealer.Deal(hero, new[] { vision, revolver }));

            Assert.IsTrue(probe.IsCompleted);
            AssertNothingChanged(cards, probe.Exception, "the ability card ", hero, vision, revolver);
            StringAssert.Contains("of character 'Stefan' doesn't fit the hero", probe.Exception.Message);
            StringAssert.Contains("(character 'Lucia')", probe.Exception.Message);
        }

        [Test]
        public void OwnedAbility_HeroWithoutCharacter_Error()
        {
            var cards = Open();
            var hero = InInventory(cards, m_Kit.Plain);
            var vision = InInventory(cards, m_Vision);

            var probe = Probe.Watch(cards.Dealer.Deal(hero, new[] { vision }));

            AssertNothingChanged(cards, probe.Exception, "(character 'none')", hero, vision);
        }

        [Test]
        public void MoreAbilitiesThanHandCapacity_Error_NothingChanged()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Alpha, 2));
            var cards = Open();
            var hero = InInventory(cards, m_LuciaCard);
            var abilities = new[] { m_Vision, m_Confuse, m_Talisman, m_Talisman }.Select(model => InInventory(cards, model)).ToArray();

            var probe = Probe.Watch(cards.Dealer.Deal(hero, abilities));

            AssertNothingChanged(cards, probe.Exception, "4 abilities don't fit the hand: capacity 3", hero, abilities);

            CardSession.Complete(cards.Dealer.Deal(hero, abilities.Take(3).ToArray()));
            Assert.AreEqual(3, cards.Slots.Expedition.Hand.Count, "three fit the default hand");
        }

        [Test]
        public void HandCapacity_FromConfig()
        {
            m_Kit.AddSlotsConfig(0, 1);
            var cards = Open();
            var hero = InInventory(cards, m_LuciaCard);
            var abilities = new[] { InInventory(cards, m_Vision), InInventory(cards, m_Talisman) };

            var probe = Probe.Watch(cards.Dealer.Deal(hero, abilities));

            AssertNothingChanged(cards, probe.Exception, "2 abilities don't fit the hand: capacity 1", hero, abilities);
        }

        [Test]
        public void DestroyedNotCardNotAbilityRepeatedHero_Errors_NothingChanged()
        {
            var cards = Open();
            var hero = InInventory(cards, m_LuciaCard);
            var vision = InInventory(cards, m_Vision);
            var destroyed = InInventory(cards, m_Talisman);
            cards.Entities.Destroy(destroyed);
            var modifier = cards.Entities.AddModifier(vision, m_Kit.Poison);
            var plain = InInventory(cards, m_Kit.Plain);

            var cases = new (Entity[] Abilities, string Message)[]
            {
                (new[] { vision, destroyed }, "is destroyed, not from this session or not a card"),
                (new[] { vision, null }, "the ability null is destroyed"),
                (new[] { modifier }, "is destroyed, not from this session or not a card"),
                (new[] { plain }, "is not an ability card (no AbilityCard component)"),
                (new[] { vision, vision }, "is listed twice"),
                (new[] { hero }, "is the hero, not an ability"),
            };

            foreach (var (abilities, message) in cases)
            {
                var probe = Probe.Watch(cards.Dealer.Deal(hero, abilities));
                AssertNothingChanged(cards, probe.Exception, message, hero, vision, plain);
            }
        }
    }
}
