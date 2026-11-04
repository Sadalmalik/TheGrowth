using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Entities;
using TheGrowth.Model.Cards;
using TheGrowth.Model.Heroes;
using TheGrowth.Model.Slots;
using TheGrowth.Tests.EditMode.Cards;

namespace TheGrowth.Tests.EditMode.Heroes
{
    /// <summary>
    /// Примеры контента проекта — карты героев (<c>Content/Entities/Heroes</c>) и способностей (<c>Content/Entities/Abilities</c>),
    /// сами ассеты и их скрипты — на поле 5 × 5 с шагом 1,5 × 2 (как карта 1,5 × 2; клетки <c>a1..e5</c>, герой — в <c>c3</c>):
    /// ходы героев (крест и ромб на 1), раскрытие карт, <c>AllowedMoves</c> способностей фигурами от героя и сценарии
    /// прототипа: Vision открывает карты, Revolver убирает монстра, Teleport меняет героя местами с картой, Confuse задаёт заряды
    /// монстру, Talisman тратит заряд; заряды кончились — в рюкзак рубашкой вверх, иначе — в руку. Розыгрыш способности (шаг 7) в
    /// тестах — перемещение карты из руки в клетку через <see cref="CardSystem.Move"/>.
    /// </summary>
    public sealed class AbilityContentTests
    {
        private static readonly string[] AllCells = "abcde".SelectMany(row => Enumerable.Range(1, 5).Select(column => $"{row}{column}")).ToArray();

        private CardTestKit m_Kit;
        private EntityModel m_Lucia;
        private EntityModel m_Alfredo;
        private EntityModel m_Stefan;
        private EntityModel m_Vision;
        private EntityModel m_Revolver;
        private EntityModel m_Teleport;
        private EntityModel m_Confuse;
        private EntityModel m_Talisman;
        private CardSession m_Cards;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit(5, 1.5f, 2f);
            m_Lucia = m_Kit.ContentModel("Entities/Heroes/Lucia.asset");
            m_Alfredo = m_Kit.ContentModel("Entities/Heroes/Alfredo.asset");
            m_Stefan = m_Kit.ContentModel("Entities/Heroes/Stefan.asset");
            m_Vision = m_Kit.ContentModel("Entities/Abilities/Vision.asset");
            m_Revolver = m_Kit.ContentModel("Entities/Abilities/Revolver.asset");
            m_Teleport = m_Kit.ContentModel("Entities/Abilities/Teleport.asset");
            m_Confuse = m_Kit.ContentModel("Entities/Abilities/Confuse.asset");
            m_Talisman = m_Kit.ContentModel("Entities/Abilities/Talisman.asset");
            m_Cards = new CardSession(m_Kit);
        }

        [TearDown]
        public void TearDown()
        {
            m_Cards.Dispose();
            m_Kit.Dispose();
        }

        private CardSystem Cards => m_Cards.Cards;

        private Slot Hand => m_Cards.Slots.Expedition.Hand;

        private Entity PlaceHero(EntityModel model, string cell = "c3")
        {
            var hero = m_Cards.Place(model, cell);
            m_Cards.Slots.SetHero(hero);
            return hero;
        }

        private Entity InHand(EntityModel ability)
        {
            return m_Cards.Slots.Create(ability, Hand);
        }

        private string[] Moves(Entity card)
        {
            Assert.IsTrue(Cards.TryEvaluate<SlotSet>(card, CardProperties.AllowedMoves, out var moves), $"{card} has no {CardProperties.AllowedMoves}");
            return moves.Select(CardTrace.Name).ToArray();
        }

        /// <summary>Розыгрыш: карта способности из руки — в клетку, с хуками.</summary>
        private void Play(Entity ability, string cell)
        {
            Assert.That(Moves(ability), Does.Contain(cell), $"{cell} is not an allowed move of {ability}");
            CardSession.Complete(Cards.Move(ability, m_Cards.Cell(cell)));
        }

        private static string[] Except(params string[] cells)
        {
            return AllCells.Except(cells).ToArray();
        }

#region Heroes

        [Test]
        public void LuciaAndAlfredo_CrossOne_WithoutMonstersObstaclesAndFree()
        {
            foreach (var model in new[] { m_Lucia, m_Alfredo })
            {
                var hero = PlaceHero(model);
                m_Cards.Place(m_Kit.Herb, "b3");
                m_Cards.Place(m_Kit.Monster, "c2");
                m_Cards.Place(m_Kit.Rock, "c4");
                m_Cards.Place(m_Kit.Herb, "b2");
                m_Cards.Place(m_Kit.Herb, "a3");

                // d3 свободна; b2 — по диагонали, a3 — через клетку.
                Assert.That(Moves(hero), Is.EqualTo(new[] { "b3" }), model.Id);

                m_Cards.Place(m_Kit.Exit, "d3");
                Assert.That(Moves(hero), Is.EqualTo(new[] { "b3", "d3" }), model.Id);

                m_Cards.Slots.DestroyExpedition();
                m_Cards.Slots.CreateExpedition(m_Kit.Location);
            }
        }

        [Test]
        public void Stefan_RhombusOne_WithoutFree_MonstersAllowed()
        {
            var hero = PlaceHero(m_Stefan);
            m_Cards.Place(m_Kit.Monster, "b3");
            m_Cards.Place(m_Kit.Herb, "c4");
            m_Cards.Place(m_Kit.Herb, "b4");
            m_Cards.Place(m_Kit.Herb, "c5");

            Assert.That(Moves(hero), Is.EqualTo(new[] { "b3", "c4" }));
        }

        [Test]
        public void Heroes_Placed_RevealAroundAndUnder()
        {
            foreach (var model in new[] { m_Lucia, m_Alfredo, m_Stefan })
            {
                var hero = PlaceHero(model, "b2");
                var under = m_Cards.Place(m_Kit.Monster, "c3", false);
                var around = new[] { "b3", "c2", "c4", "d3" }.Select(cell => m_Cards.Place(m_Kit.Exit, cell, false)).ToArray();
                var diagonal = m_Cards.Place(m_Kit.Exit, "b4", false);
                var far = m_Cards.Place(m_Kit.Exit, "e3", false);

                CardSession.Complete(Cards.Move(hero, m_Cards.Cell("c3")));

                Assert.IsTrue(CardSession.IsFaceUp(under), model.Id);
                Assert.IsTrue(around.All(CardSession.IsFaceUp), model.Id);
                Assert.IsTrue(CardSession.IsFaceUp(diagonal), $"{model.Id}: reveal is square 1 as in the prototype");
                Assert.IsFalse(CardSession.IsFaceUp(far), model.Id);

                var near = m_Cards.Place(m_Kit.Exit, "a2", false);
                CardSession.Complete(Cards.Move(hero, m_Cards.Cell("b2")));
                Assert.IsTrue(CardSession.IsFaceUp(near), model.Id);

                m_Cards.Slots.DestroyExpedition();
                m_Cards.Slots.CreateExpedition(m_Kit.Location);
            }
        }

        [Test]
        public void Heroes_PlacedFirstTime_RevealAround()
        {
            var hero = PlaceHero(m_Lucia);
            var near = m_Cards.Place(m_Kit.Herb, "c4", false);

            CardSession.Complete(Cards.InvokeHook(hero, CardHooks.OnPlacedFirstTime));

            Assert.IsTrue(CardSession.IsFaceUp(near));
        }

#endregion

#region Allowed moves of abilities

        [Test]
        public void Abilities_AllowedMoves_ShapesAroundHero()
        {
            PlaceHero(m_Lucia);
            var vision = InHand(m_Vision);
            var revolver = InHand(m_Revolver);
            var confuse = InHand(m_Confuse);

            Assert.That(Moves(vision), Is.EqualTo(AllCells), "Vision: square 2 around the hero");
            Assert.That(Moves(confuse), Is.EqualTo(AllCells), "Confuse: square 2 around the hero");
            Assert.That(Moves(revolver), Is.EqualTo(new[] { "b2", "b3", "b4", "c2", "c4", "d2", "d3", "d4" }), "Revolver: square 1 without the hero cell");

            m_Cards.Slots.Move(revolver, m_Cards.Slots.Inventory);
            var talisman = InHand(m_Talisman);
            Assert.That(Moves(talisman), Is.EqualTo(new[] { "c3" }), "Talisman: the hero cell");
        }

        [Test]
        public void Teleport_AllowedMoves_RhombusThree_WithoutObstaclesExitAndFree()
        {
            PlaceHero(m_Lucia);
            var teleport = InHand(m_Teleport);
            foreach (var cell in AllCells.Where(cell => cell != "c3"))
                m_Cards.Place(m_Kit.Herb, cell);

            Assert.That(Moves(teleport), Is.EqualTo(Except("a1", "a5", "c3", "e1", "e5")), "rhombus 3 without the hero cell");

            m_Cards.Place(m_Kit.Rock, "a3");
            m_Cards.Place(m_Kit.Exit, "e3");
            m_Cards.Place(m_Kit.Monster, "c5");
            m_Cards.Slots.Move(m_Cards.Cell("b3").Entities[0], m_Cards.Slots.Expedition.Discard);
            Assert.That(Moves(teleport), Is.EqualTo(Except("a1", "a5", "c3", "e1", "e5", "a3", "e3", "b3")), "monsters are allowed");
        }

        [Test]
        public void Abilities_NoHero_NoMoves()
        {
            var vision = InHand(m_Vision);

            Assert.That(Moves(vision), Is.Empty);
        }

#endregion

#region Scenarios

        [Test]
        public void Vision_RevealsAroundAndUnder_SpendsCharge_BackToHand()
        {
            PlaceHero(m_Lucia);
            var vision = InHand(m_Vision);
            var under = m_Cards.Place(m_Kit.Monster, "a1", false);
            var around = new[] { "a2", "b1", "b2" }.Select(cell => m_Cards.Place(m_Kit.Herb, cell, false)).ToArray();
            var far = m_Cards.Place(m_Kit.Herb, "a3", false);

            Play(vision, "a1");

            Assert.IsTrue(CardSession.IsFaceUp(under));
            Assert.IsTrue(around.All(CardSession.IsFaceUp));
            Assert.IsFalse(CardSession.IsFaceUp(far));
            Assert.AreEqual(3, m_Cards.Charges(vision));
            Assert.AreSame(Hand, m_Cards.Slots.GetSlot(vision));
            Assert.IsTrue(CardSession.IsFaceUp(vision));
            Assert.That(m_Cards.Cell("a1").Entities, Is.EqualTo(new[] { under }));
        }

        [Test]
        public void Vision_LastCharge_ToBackpackFaceDown()
        {
            PlaceHero(m_Lucia);
            var vision = InHand(m_Vision);
            m_Cards.Run(vision, "set [active card].charges to 1");
            m_Cards.Place(m_Kit.Herb, "a1", false);

            Play(vision, "a1");

            Assert.AreEqual(0, m_Cards.Charges(vision));
            Assert.AreSame(m_Cards.Slots.Expedition.Backpack, m_Cards.Slots.GetSlot(vision));
            Assert.IsFalse(CardSession.IsFaceUp(vision), "a spent ability lies face down");
        }

        [Test]
        public void Revolver_RemovesMonster_SpendsCharge()
        {
            PlaceHero(m_Stefan);
            var revolver = InHand(m_Revolver);
            var herb = m_Cards.Place(m_Kit.Herb, "b2");
            var monster = m_Cards.Place(m_Kit.Monster, "b2");

            Play(revolver, "b2");

            Assert.AreSame(m_Cards.Slots.Expedition.Deck, m_Cards.Slots.GetSlot(monster), "the monster is removed into the deck");
            Assert.IsFalse(CardSession.IsFaceUp(monster));
            Assert.That(m_Cards.Cell("b2").Entities, Is.EqualTo(new[] { herb }));
            Assert.AreEqual(1, m_Cards.Charges(revolver));
            Assert.AreSame(Hand, m_Cards.Slots.GetSlot(revolver));
        }

        [Test]
        public void Revolver_NoMonster_BackToHand_NoChargeSpent()
        {
            PlaceHero(m_Stefan);
            var revolver = InHand(m_Revolver);
            var herb = m_Cards.Place(m_Kit.Herb, "b3");

            Play(revolver, "b3");

            Assert.AreEqual(2, m_Cards.Charges(revolver));
            Assert.AreSame(Hand, m_Cards.Slots.GetSlot(revolver));
            Assert.That(m_Cards.Cell("b3").Entities, Is.EqualTo(new[] { herb }));
        }

        [Test]
        public void Teleport_SwapsHeroWithCard()
        {
            var hero = PlaceHero(m_Lucia);
            var teleport = InHand(m_Teleport);
            var herb = m_Cards.Place(m_Kit.Herb, "a2", false);
            var near = m_Cards.Place(m_Kit.Herb, "a1", false);

            Play(teleport, "a2");

            Assert.That(m_Cards.Cell("a2").Entities, Is.EqualTo(new[] { hero }), m_Cards.Describe());
            Assert.That(m_Cards.Cell("c3").Entities, Is.EqualTo(new[] { herb }), m_Cards.Describe());
            Assert.AreSame(hero, m_Cards.Slots.Expedition.Hero);
            Assert.IsTrue(CardSession.IsFaceUp(herb), "the card under the teleport is revealed");
            Assert.IsTrue(CardSession.IsFaceUp(near), "the hero placed by the teleport reveals around");
            Assert.AreEqual(1, m_Cards.Charges(teleport));
            Assert.AreSame(Hand, m_Cards.Slots.GetSlot(teleport));
        }

        [Test]
        public void Confuse_SetsMonsterCharges_SpendsCharge()
        {
            PlaceHero(m_Lucia);
            var confuse = InHand(m_Confuse);
            var monster = m_Cards.Place(m_Kit.Monster, "e5");
            m_Cards.Run(monster, "set [active card].charges to 0");

            Play(confuse, "e5");

            Assert.AreEqual(1, m_Cards.Charges(monster), "the monster waits one step");
            Assert.AreEqual(2, m_Cards.Charges(confuse));
            Assert.AreSame(Hand, m_Cards.Slots.GetSlot(confuse));

            CardSession.Complete(Cards.InvokeHook(monster, CardHooks.OnStep));
            Assert.AreSame(m_Cards.Cell("e5"), m_Cards.Slots.GetSlot(monster), "the monster spends its charge instead of moving");
        }

        [Test]
        public void Confuse_NoMonster_BackToHand_NoChargeSpent()
        {
            PlaceHero(m_Lucia);
            var confuse = InHand(m_Confuse);
            m_Cards.Place(m_Kit.Herb, "a1");

            Play(confuse, "a1");

            Assert.AreEqual(3, m_Cards.Charges(confuse));
            Assert.AreSame(Hand, m_Cards.Slots.GetSlot(confuse));
        }

        [Test]
        public void Talisman_OneCharge_SpentToBackpackFaceDown()
        {
            var hero = PlaceHero(m_Alfredo);
            var talisman = InHand(m_Talisman);

            Play(talisman, "c3");

            Assert.AreEqual(0, m_Cards.Charges(talisman));
            Assert.AreSame(m_Cards.Slots.Expedition.Backpack, m_Cards.Slots.GetSlot(talisman));
            Assert.IsFalse(CardSession.IsFaceUp(talisman));
            Assert.That(m_Cards.Cell("c3").Entities, Is.EqualTo(new[] { hero }));
        }

        [Test]
        public void Abilities_FitHeroes_ByOwner()
        {
            var lucia = m_Cards.Slots.Create(m_Lucia, m_Cards.Slots.Inventory);
            var stefan = m_Cards.Slots.Create(m_Stefan, m_Cards.Slots.Inventory);
            var alfredo = m_Cards.Slots.Create(m_Alfredo, m_Cards.Slots.Inventory);

            bool Fits(EntityModel ability, Entity hero)
            {
                return m_Cards.Slots.Create(ability, m_Cards.Slots.Inventory).GetRequired<AbilityCard>().Fits(hero);
            }

            Assert.IsTrue(Fits(m_Vision, lucia) && Fits(m_Confuse, lucia) && !Fits(m_Vision, stefan) && !Fits(m_Confuse, alfredo));
            Assert.IsTrue(Fits(m_Revolver, stefan) && !Fits(m_Revolver, lucia));
            Assert.IsTrue(new[] { lucia, stefan, alfredo }.All(hero => Fits(m_Teleport, hero) && Fits(m_Talisman, hero)), "player abilities");
        }

#endregion
    }
}
