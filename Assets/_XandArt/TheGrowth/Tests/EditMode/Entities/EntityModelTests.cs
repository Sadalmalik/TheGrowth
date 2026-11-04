using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Content;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Tags;
using TheGrowth.Core.Variables;
using TheGrowth.EditorTools.Content;
using TheGrowth.EditorTools.Entities;
using TheGrowth.Model.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.Tests.EditMode.Entities
{
    /// <summary>
    /// Конфиг сущности в редакторе: пресеты видов, обязательные компоненты (дописывание, запрет удаления), компоненты по типу,
    /// проверки валидатора (компоненты, теги, переменные, <c>[ModelKind]</c>), типы компонентов.
    /// </summary>
    public sealed class EntityModelTests
    {
        private EntityTestKit m_Kit;
        private readonly List<Object> m_Created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            m_Kit = new EntityTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in m_Created)
                Object.DestroyImmediate(item);

            m_Created.Clear();
            m_Kit.Dispose();
        }

        private EntityModel Preset(EntityKind kind)
        {
            var model = EntityModelEditing.CreatePreset(kind);
            m_Created.Add(model);
            return model;
        }

        private List<ContentError> Validate(params ContentAsset[] assets)
        {
            return ContentValidator.Validate(new ContentAsset[] { m_Kit.Tags, m_Kit.Variables }.Concat(assets).ToList(), null);
        }

        private static List<string> Messages(IEnumerable<ContentError> errors, ContentErrorKind kind)
        {
            return errors.Where(error => error.Kind == kind).Select(error => error.Message).ToList();
        }

#region Presets and required components

        [Test]
        public void Presets_CardHasBrain_ModifierAndMechanicHaveNoRequiredComponents()
        {
            var card = Preset(EntityKind.Card);
            var modifier = Preset(EntityKind.Modifier);
            var mechanic = Preset(EntityKind.Mechanic);

            Assert.AreEqual(EntityKind.Card, card.Kind);
            Assert.That(card.Components.Select(settings => settings.GetType()), Is.EqualTo(new[] { typeof(CardBrainSettings) }));
            Assert.AreEqual("card_brain", card.Components[0].Key);
            Assert.AreEqual(EntityKind.Modifier, modifier.Kind);
            Assert.IsEmpty(modifier.Components);
            Assert.AreEqual(EntityKind.Mechanic, mechanic.Kind);
            Assert.IsEmpty(mechanic.Components);
            Assert.That(RequiredComponents.Get(EntityKind.Card), Is.EqualTo(new[] { typeof(CardBrainSettings) }));
        }

        [Test]
        public void RequiredComponent_CannotBeRemoved_OtherCan()
        {
            var card = Preset(EntityKind.Card);
            EntityModelEditing.Add(card, typeof(MarkerSettings));

            Assert.IsFalse(EntityModelEditing.CanRemove(card, 0, out var reason));
            StringAssert.Contains("required for a card", reason);
            Assert.IsFalse(EntityModelEditing.Remove(card, 0));
            Assert.IsTrue(EntityModelEditing.CanRemove(card, 1, out _));
            Assert.IsTrue(EntityModelEditing.Remove(card, 1));
            Assert.That(card.Components.Select(settings => settings.GetType()), Is.EqualTo(new[] { typeof(CardBrainSettings) }));
        }

        [Test]
        public void AddMissingComponents_AddsOnceAtStart_ValidatorReportsMissingBefore()
        {
            var card = m_Kit.Content.Asset<EntityModel>("Bare");
            card.SetKind(EntityKind.Card);
            card.ComponentList.Add(new MarkerSettings());

            var errors = Messages(Validate(card), ContentErrorKind.InvalidEntity);
            Assert.That(errors, Has.Exactly(1).Contains("required component Card Brain of a card is missing"));

            Assert.That(EntityModelEditing.AddMissingComponents(card), Is.EqualTo(new[] { typeof(CardBrainSettings) }));
            Assert.IsEmpty(EntityModelEditing.AddMissingComponents(card), "nothing is missing any more");
            Assert.That(card.Components.Select(settings => settings.GetType()), Is.EqualTo(new[] { typeof(CardBrainSettings), typeof(MarkerSettings) }));
            Assert.IsEmpty(Messages(Validate(card), ContentErrorKind.InvalidEntity));
        }

        [Test]
        public void SetKind_AddsRequiredOfNewKind_KeepsOthers()
        {
            var model = Preset(EntityKind.Modifier);
            Assert.That(EntityModelEditing.SetKind(model, EntityKind.Card), Is.EqualTo(new[] { typeof(CardBrainSettings) }));
            Assert.IsFalse(EntityModelEditing.CanRemove(model, 0, out _));

            Assert.IsEmpty(EntityModelEditing.SetKind(model, EntityKind.Mechanic));
            Assert.AreEqual(1, model.Components.Count, "the brain stays");
            Assert.IsTrue(EntityModelEditing.CanRemove(model, 0, out _), "not required for a mechanic");
        }

        [Test]
        public void Add_OneComponentPerKey_Move()
        {
            var card = Preset(EntityKind.Card);
            EntityModelEditing.Add(card, typeof(CounterSettings));

            Assert.IsFalse(EntityModelEditing.CanAdd(card, typeof(CounterSettings), out var reason));
            StringAssert.Contains("'test_counter' is already added", reason);
            Assert.IsFalse(EntityModelEditing.CanAdd(card, typeof(DuplicateCounterSettings), out _), "same key, other type");
            Assert.IsFalse(EntityModelEditing.CanAdd(card, typeof(KeylessSettings), out reason));
            StringAssert.Contains("no [ComponentKey]", reason);
            Assert.Throws<InvalidOperationException>(() => EntityModelEditing.Add(card, typeof(CounterSettings)));

            Assert.IsTrue(EntityModelEditing.Move(card, 1, 0));
            Assert.That(card.Components.Select(settings => settings.GetType()), Is.EqualTo(new[] { typeof(CounterSettings), typeof(CardBrainSettings) }));
            Assert.IsFalse(EntityModelEditing.Move(card, 0, 5));
        }

#endregion

#region Validator

        [Test]
        public void Validator_ComponentErrors()
        {
            var card = m_Kit.Model("Broken", EntityKind.Card);
            card.ComponentList.Add(null);
            card.ComponentList.Add(new KeylessSettings());
            card.ComponentList.Add(new CounterSettings());
            card.ComponentList.Add(new DuplicateCounterSettings());

            var errors = Messages(Validate(card), ContentErrorKind.InvalidEntity);

            Assert.That(errors, Has.Exactly(1).Contains("component 1 is empty or its type is missing"));
            Assert.That(errors, Has.Exactly(1).Contains("component KeylessSettings has no [ComponentKey]"));
            Assert.That(errors, Has.Exactly(1).Contains("component 'test_counter' is added twice"));
        }

        [Test]
        public void Validator_TagsAndVariables()
        {
            var card = m_Kit.Model("Tagged", EntityKind.Card, "Hero", "UnknownTag", "Hero");
            card.VariableList.Add(new EntityVariable("charges", "many"));
            card.VariableList.Add(new EntityVariable("coins", "1"));
            card.VariableList.Add(new EntityVariable("no_such_variable"));
            card.VariableList.Add(new EntityVariable("mood", "angry"));
            card.VariableList.Add(new EntityVariable("mood"));

            var errors = Messages(Validate(card), ContentErrorKind.InvalidEntity);

            Assert.That(errors, Is.EquivalentTo(new[]
            {
                "tag 'UnknownTag' is not declared",
                "tag 'Hero' is set twice",
                "variable 'charges': value 'many' is not a valid int (integer)",
                "variable 'coins' is a game variable, only card variables can be on an entity",
                "variable 'no_such_variable' is not declared",
                "variable 'mood' is listed twice",
            }));
        }

        [Test]
        public void Validator_ModelKind_FieldsListsNestedAndComponents()
        {
            var links = m_Kit.Content.Asset<TestEntityLinks>("Links");
            links.Card = m_Kit.Monster;
            links.Modifiers.Add(m_Kit.Poison);
            links.Modifiers.Add(m_Kit.Hero);
            links.Nested.Mechanic = m_Kit.Hero;
            links.Placed = m_Kit.Wall;
            links.Any = m_Kit.Poison;
            var linking = m_Kit.Model("Linking", EntityKind.Card);
            ((LinkSettings)EntityModelEditing.Add(linking, typeof(LinkSettings))).Modifier = m_Kit.Monster;

            var errors = Messages(Validate(m_Kit.Hero, m_Kit.Monster, m_Kit.Poison, m_Kit.Wall, links, linking), ContentErrorKind.WrongModelKind);

            Assert.That(errors, Is.EquivalentTo(new[]
            {
                "Modifiers[1]: 'Hero' is a card, expected modifier",
                "Nested.Mechanic: 'Hero' is a card, expected mechanic",
                "m_Components[1].Modifier: 'Monster' is a card, expected modifier",
            }));

            links.Placed = m_Kit.Poison;
            Assert.That(Messages(Validate(links), ContentErrorKind.WrongModelKind), Has.Exactly(1).EqualTo("m_Placed: 'Poison' is a modifier, expected card or mechanic"));
        }

        [Test]
        public void ComponentTypes_KeysAndDuplicates()
        {
            var errors = EntityModelEditing.CheckComponentTypes(new[] { typeof(CounterSettings), typeof(MarkerSettings), typeof(KeylessSettings), typeof(DuplicateCounterSettings) });

            Assert.That(errors, Has.Count.EqualTo(2));
            Assert.That(errors, Has.Exactly(1).Contains("KeylessSettings has no [ComponentKey]"));
            Assert.That(errors, Has.Exactly(1).Contains("component key 'test_counter' is used by"));
        }

        [Test]
        public void ComponentTypes_OfGame_AreValid_TestTypesExcluded()
        {
            var types = EntityModelEditing.FindComponentTypes();

            Assert.That(types, Has.Member(typeof(CardBrainSettings)));
            Assert.That(types, Has.No.Member(typeof(CounterSettings)));
            Assert.IsEmpty(EntityModelEditing.CheckComponentTypes(types));
        }

        [Test]
        public void ModelKindDrawer_OffersOnlyModelsOfKind()
        {
            var modifiers = ModelKindDrawer.FindModels(new ModelKindAttribute(EntityKind.Modifier));
            var cards = ModelKindDrawer.FindModels(new ModelKindAttribute(EntityKind.Card, EntityKind.Mechanic));

            Assert.That(modifiers, Is.Not.Empty, "the project has sample modifiers");
            Assert.That(modifiers.Select(model => model.Kind), Is.All.EqualTo(EntityKind.Modifier));
            Assert.That(cards.Select(model => model.Kind), Is.All.Not.EqualTo(EntityKind.Modifier));
            Assert.That(cards.Select(model => model.Kind), Has.Some.EqualTo(EntityKind.Mechanic));
        }

        [Test]
        public void ProjectSamples_AreValid()
        {
            var registry = UnityEditor.AssetDatabase.LoadAssetAtPath<ContentRegistry>(ContentRegistryBuilder.RegistryPath);
            var models = registry.GetAll<EntityModel>();
            var tags = TagCatalog.FromContent(registry);
            var variables = VariableCatalog.FromContent(registry);

            Assert.That(models.Select(model => model.Id), Is.SupersetOf(new[] { "Lucia", "Monster", "Herb", "Exit", "Poison", "Wall" }));
            foreach (var model in models)
                Assert.IsEmpty(EntityModelChecks.Check(model, tags, variables), model.ToString());

            Assert.IsTrue(registry.Get<EntityModel>("Monster").Variables.Any(variable => variable.Name == "charges" && variable.Value == "3"));
        }

#endregion
    }
}
