using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheGrowth.EditorTools.Content;
using TheGrowth.EditorTools.Decks;
using TheGrowth.EditorTools.Entities;
using TheGrowth.EditorTools.Flow;
using TheGrowth.Model.Decks;
using TheGrowth.Model.Locations;
using TheGrowth.Tests.EditMode.Cards;
using UnityEditor;

namespace TheGrowth.Tests.EditMode.Decks
{
    /// <summary>
    /// Валидатор контента для колод и раскладки на контенте <see cref="CardTestKit"/> (поле 3 × 3: a1 — <c>PlayerSpawn</c>, c3 —
    /// <c>Exit</c>; выход — <c>SpawnSlot -> [slots tagged Exit]</c>): строки колоды, вид конфига, клетка героя, предупреждения
    /// <c>SpawnSlot</c> (раскладка на пустом поле без сцены) и локации экспедиции без колоды.
    /// </summary>
    public sealed class DeckValidationTests
    {
        private CardTestKit m_Kit;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        private List<ContentError> Validate()
        {
            var errors = ContentValidator.Validate(m_Kit.Registry.Assets, m_Kit.Registry.Scripts);
            var other = errors.Where(error => error.Kind is not (ContentErrorKind.InvalidDeck or ContentErrorKind.InvalidDeal or ContentErrorKind.WrongModelKind)).ToList();
            Assert.IsEmpty(other, "the test content has other errors:\n" + string.Join("\n", other));
            return errors;
        }

        private ContentError[] Deal(List<ContentError> errors)
        {
            return errors.Where(error => error.Kind == ContentErrorKind.InvalidDeal).ToArray();
        }

#region Deck rows

        [Test]
        public void Rows_NoCardOrCountBelowOne_Errors()
        {
            m_Kit.Deck(new DeckEntry(null), new DeckEntry(m_Kit.Plain, 0), new DeckEntry(m_Kit.Plain, 2));

            var errors = Validate().Where(error => error.Kind == ContentErrorKind.InvalidDeck).ToArray();

            Assert.AreEqual(2, errors.Length, string.Join("\n", errors));
            StringAssert.Contains("row 0: no card config", errors[0].Message);
            StringAssert.Contains("row 1 ('Plain'): count is 0", errors[1].Message);
            Assert.IsFalse(errors[0].IsWarning);
        }

        [Test]
        public void Row_NotCard_WrongModelKind()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Poison));

            var errors = Validate().Where(error => error.Kind == ContentErrorKind.WrongModelKind).ToArray();

            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            StringAssert.Contains("m_Entries[0].m_Model", errors[0].Message);
        }

#endregion

#region Field

        [Test]
        public void DeckWithoutPlayerSpawn_Error()
        {
            m_Kit.Graph.FindSlot("a1").TagList.Clear();
            m_Kit.Deck(new DeckEntry(m_Kit.Plain));

            var errors = Deal(Validate());

            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            StringAssert.Contains("has no slot tagged PlayerSpawn", errors[0].Message);
            Assert.IsFalse(errors[0].IsWarning);
        }

        [Test]
        public void NoDeck_NoPlayerSpawn_NoError()
        {
            m_Kit.Graph.FindSlot("a1").TagList.Clear();

            Assert.IsEmpty(Deal(Validate()));
        }

        [Test]
        public void ExpeditionLocationWithoutDeck_Warning()
        {
            SetType(LocationType.Expedition);

            var warnings = Deal(Validate());

            Assert.AreEqual(1, warnings.Length, string.Join("\n", warnings));
            Assert.IsTrue(warnings[0].IsWarning);
            StringAssert.Contains("the expedition location has a field but no deck: only the hero is dealt", warnings[0].Message);
        }

        [Test]
        public void ExpeditionLocationWithDeck_NoWarning()
        {
            SetType(LocationType.Expedition);
            m_Kit.Deck(new DeckEntry(m_Kit.Plain));

            Assert.IsEmpty(Validate());
        }

        private void SetType(LocationType type)
        {
            var serialized = new SerializedObject(m_Kit.Location);
            serialized.FindProperty("m_Type").enumValueIndex = (int)type;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

#endregion

#region SpawnSlot

        [Test]
        public void SpawnSlot_EnoughSlots_NoWarning()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Exit), new DeckEntry(m_Kit.Monster, 30));

            Assert.IsEmpty(Validate());
        }

        [Test]
        public void SpawnSlot_FewerSlotsThanCards_Warning()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Exit, 3));

            var errors = Validate();
            var warnings = Deal(errors);

            Assert.AreEqual(1, warnings.Length, string.Join("\n", warnings));
            Assert.IsTrue(warnings[0].IsWarning);
            StringAssert.Contains("SpawnSlot of 'Exit' has free field slots for 1 of 3 card(s) of deck 'Deck': 2 stay in the deck", warnings[0].Message);
            Assert.AreEqual(0, ContentValidator.CountErrors(errors));
        }

        [Test]
        public void SpawnSlot_NoSlots_Warning()
        {
            m_Kit.Graph.FindSlot("c3").TagList.Clear();
            m_Kit.Deck(new DeckEntry(m_Kit.Exit));

            var warnings = Deal(Validate());

            Assert.AreEqual(1, warnings.Length, string.Join("\n", warnings));
            Assert.IsTrue(warnings[0].IsWarning);
            StringAssert.Contains("SpawnSlot of 'Exit' has no free field slot on the empty field", warnings[0].Message);
        }

        [Test]
        public void SpawnSlot_KeepInDeckRows_NotChecked()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Exit), new DeckEntry(m_Kit.Exit, 3, true));

            Assert.IsEmpty(Validate());
        }

        [Test]
        public void SpawnSlot_HeroCellIsTaken()
        {
            var ghost = m_Kit.Model("Ghost", "SpawnSlot -> [slots tagged PlayerSpawn]\n");
            m_Kit.Deck(new DeckEntry(ghost));

            var warnings = Deal(Validate());

            Assert.AreEqual(1, warnings.Length, string.Join("\n", warnings));
            StringAssert.Contains("SpawnSlot of 'Ghost' has no free field slot", warnings[0].Message);
        }

#endregion

#region Project

        [Test]
        public void Project_TestLocation_HasDeck_RowsValid()
        {
            // Колода тестовой локации — пример контента; граф автор правит руками, на его содержимое тест не опирается.
            var location = AssetDatabase.LoadAssetAtPath<LocationModel>(FlowSceneSetup.TestLocationPath);

            Assert.IsNotNull(location.Deck);
            Assert.IsNotEmpty(location.Deck.Entries);
            Assert.IsEmpty(DeckChecks.CheckDeck(location.Deck));
            Assert.IsEmpty(ModelKindChecks.Check(location.Deck));
        }

#endregion
    }
}
