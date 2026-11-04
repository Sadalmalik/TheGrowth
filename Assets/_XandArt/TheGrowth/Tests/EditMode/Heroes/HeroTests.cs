using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Content;
using TheGrowth.Core.Entities;
using TheGrowth.EditorTools.Content;
using TheGrowth.EditorTools.Entities;
using TheGrowth.EditorTools.Heroes;
using TheGrowth.Model.Heroes;
using TheGrowth.Tests.EditMode.Cards;
using UnityEditor;

namespace TheGrowth.Tests.EditMode.Heroes
{
    /// <summary>
    /// Персонаж, компоненты карты героя и способности (поведения, «способность подходит герою»), проверки валидатора
    /// (<see cref="HeroChecks"/>: ошибки и предупреждения), примеры контента проекта (персонажи, карты героев, способности с
    /// владельцами и зарядами из прототипа).
    /// </summary>
    public sealed class HeroTests
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

        private List<ContentError> Validate(params ContentAsset[] assets)
        {
            return ContentValidator.Validate(new ContentAsset[] { m_Kit.Tags, m_Kit.Variables }.Concat(assets).ToList(), null)
                .Where(error => error.Kind == ContentErrorKind.InvalidHero)
                .ToList();
        }

#region Components

        [Test]
        public void HeroCard_HasCharacterOfConfig()
        {
            var lucia = m_Kit.Character("Lucia");
            var model = m_Kit.HeroModel("LuciaCard", lucia);
            using var cards = new CardSession(m_Kit);

            var hero = cards.Slots.Create(model, cards.Slots.Inventory);

            Assert.AreSame(lucia, hero.GetRequired<HeroCard>().Character);
            Assert.AreEqual("hero_card", hero.GetRequired<HeroCard>().Key);
            Assert.IsTrue(hero.HasTag(HeroCardSettings.Tag));
            Assert.AreEqual("Lucia", lucia.DisplayName);
        }

        [Test]
        public void Ability_FitsHeroOfOwner_PlayerAbilityFitsAnyHero()
        {
            var lucia = m_Kit.Character("Lucia");
            var stefan = m_Kit.Character("Stefan");
            var luciaCard = m_Kit.HeroModel("LuciaCard", lucia);
            var stefanCard = m_Kit.HeroModel("StefanCard", stefan);
            var vision = m_Kit.AbilityModel("Vision", lucia);
            var talisman = m_Kit.AbilityModel("Talisman", null);
            using var cards = new CardSession(m_Kit);
            var inventory = cards.Slots.Inventory;
            var luciaHero = cards.Slots.Create(luciaCard, inventory);
            var stefanHero = cards.Slots.Create(stefanCard, inventory);
            var plainCard = cards.Slots.Create(m_Kit.Plain, inventory);
            var luciaAbility = cards.Slots.Create(vision, inventory).GetRequired<AbilityCard>();
            var playerAbility = cards.Slots.Create(talisman, inventory).GetRequired<AbilityCard>();

            Assert.AreSame(lucia, luciaAbility.Owner);
            Assert.IsFalse(luciaAbility.IsPlayerAbility);
            Assert.IsTrue(luciaAbility.Fits(luciaHero));
            Assert.IsFalse(luciaAbility.Fits(stefanHero), "an ability of another character");
            Assert.IsFalse(luciaAbility.Fits(plainCard), "a card without a hero component has no character");
            Assert.IsFalse(luciaAbility.Fits(null));

            Assert.IsNull(playerAbility.Owner);
            Assert.IsTrue(playerAbility.IsPlayerAbility);
            Assert.IsTrue(playerAbility.Fits(luciaHero) && playerAbility.Fits(stefanHero) && playerAbility.Fits(plainCard));
            Assert.IsTrue(new AbilityCardSettings(lucia).Fits(lucia) && !new AbilityCardSettings(lucia).Fits(null) && new AbilityCardSettings().Fits(null));
        }

#endregion

#region Validator

        [Test]
        public void Validator_HeroWithCharacterAndTag_NoProblems()
        {
            var model = m_Kit.HeroModel("LuciaCard", m_Kit.Character("Lucia"));

            Assert.IsEmpty(Validate(model));
        }

        [Test]
        public void Validator_HeroWithoutTag_Warning()
        {
            var model = m_Kit.Model("Untagged", null);
            model.ComponentList.Add(new HeroCardSettings(m_Kit.Character("Lucia")));

            var errors = Validate(model);

            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            Assert.IsTrue(errors[0].IsWarning);
            StringAssert.Contains("hero card is not tagged Hero", errors[0].Message);
        }

        [Test]
        public void Validator_TagWithoutHeroComponent_Warning()
        {
            var model = m_Kit.Model("Tagged", null, HeroCardSettings.Tag);

            var errors = Validate(model);

            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            Assert.IsTrue(errors[0].IsWarning);
            StringAssert.Contains("tagged Hero but has no hero card component", errors[0].Message);
            Assert.AreEqual(0, ContentValidator.CountErrors(errors), "warnings don't stop the build");
        }

        [Test]
        public void Validator_HeroWithoutCharacter_Error()
        {
            var model = m_Kit.HeroModel("Nobody", null);

            var errors = Validate(model);

            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            Assert.IsFalse(errors[0].IsWarning);
            StringAssert.Contains("hero card has no character", errors[0].Message);
        }

        [Test]
        public void Validator_HeroOrAbilityNotOnCard_Error()
        {
            var modifier = m_Kit.Content.Asset<EntityModel>("Charm");
            EntityModelEditing.SetKind(modifier, EntityKind.Modifier);
            modifier.ComponentList.Add(new HeroCardSettings(m_Kit.Character("Lucia")));
            modifier.ComponentList.Add(new AbilityCardSettings());
            modifier.TagList.Add(HeroCardSettings.Tag);

            var errors = Validate(modifier).Where(error => !error.IsWarning).Select(error => error.Message).ToList();

            Assert.AreEqual(2, errors.Count, string.Join("\n", errors));
            Assert.That(errors, Has.Exactly(1).Contains("only cards can be heroes"));
            Assert.That(errors, Has.Exactly(1).Contains("only cards can be abilities"));
        }

        [Test]
        public void Validator_AbilityWithoutOwner_IsPlayerAbility_NoProblems()
        {
            Assert.IsEmpty(Validate(m_Kit.AbilityModel("Talisman", null), m_Kit.AbilityModel("Vision", m_Kit.Character("Lucia"))));
        }

#endregion

#region Project content

        [Test]
        public void Project_Characters_HeroCards_Abilities()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ContentRegistry>(ContentRegistryBuilder.RegistryPath);

            var characters = registry.GetAll<CharacterModel>().Select(character => character.Id).ToArray();
            Assert.That(characters, Is.SupersetOf(new[] { "Lucia", "Alfredo", "Stefan" }));
            foreach (var id in new[] { "Lucia", "Alfredo", "Stefan" })
            {
                var hero = registry.Get<EntityModel>(id);
                var settings = hero.Components.OfType<HeroCardSettings>().Single();
                Assert.AreSame(registry.Get<CharacterModel>(id), settings.Character, id);
                Assert.IsTrue(hero.HasTag(HeroCardSettings.Tag), id);
                Assert.AreEqual(id, hero.ScriptId);
            }

            // Владельцы и начальные заряды — из ассетов прототипа (Resources/Cards/Abilities).
            var abilities = new (string Id, string Owner, string Charges)[]
            {
                ("Vision", "Lucia", "4"), ("Revolver", "Stefan", "2"), ("Teleport", null, "2"), ("Confuse", "Lucia", "3"), ("Talisman", null, "1"),
            };

            foreach (var (id, owner, charges) in abilities)
            {
                var ability = registry.Get<EntityModel>(id);
                var settings = ability.Components.OfType<AbilityCardSettings>().Single();
                Assert.AreEqual(owner, settings.Owner != null ? settings.Owner.Id : null, id);
                Assert.IsTrue(ability.Variables.Any(variable => variable.Name == "charges" && variable.Value == charges), id);
                Assert.AreEqual(id, ability.ScriptId);
            }
        }

        [Test]
        public void Project_HeroesAndAbilities_HaveNoHeroProblems()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ContentRegistry>(ContentRegistryBuilder.RegistryPath);

            foreach (var id in new[] { "Lucia", "Alfredo", "Stefan", "Vision", "Revolver", "Teleport", "Confuse", "Talisman" })
                Assert.IsEmpty(HeroChecks.Check(registry.Get<EntityModel>(id)), id);
        }

#endregion
    }
}
