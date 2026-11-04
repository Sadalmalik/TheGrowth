using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Content;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;
using TheGrowth.EditorTools.Content;
using TheGrowth.EditorTools.Entities;

namespace TheGrowth.Tests.EditMode.Content
{
    public sealed class ContentValidatorTests
    {
        private ContentTestKit m_Kit;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        [Test]
        public void ValidContent_NoErrors()
        {
            var errors = ContentValidator.Validate(
                new ContentAsset[] { m_Kit.Asset<TestCardModel>("monster_1"), m_Kit.Asset<TestLocationModel>("monster_1"), m_Kit.Asset<TestCardModel>("Лес_2") },
                new[] { m_Kit.Script("Hub", "Start:\n"), m_Kit.Script("Lucia_intro", "Start:\n", ScriptFileKind.Dialog) });

            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void Properties_OnlyInScriptFilesOfEntities()
        {
            var hero = m_Kit.Asset<EntityModel>("Hero");
            EntityModelEditing.SetKind(hero, EntityKind.Card);
            hero.SetScript("Hero");
            var shared = m_Kit.Script("Shared", "Start:\n    log \"a\"\nSpawnSlot -> [free slots]\n");

            var errors = ContentValidator.Validate(new ContentAsset[] { hero }, new[]
            {
                m_Kit.Script("Hero", "AllowedMoves -> [free slots]\n\nOnPlaced:\n    log \"a\"\n"),
                shared,
                m_Kit.Script("Talk", "SpawnSlot -> [free slots]\n", ScriptFileKind.Dialog),
            });

            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            Assert.AreEqual(ContentErrorKind.InvalidScript, errors[0].Kind);
            Assert.AreSame(shared.Text, errors[0].Context);
            StringAssert.Contains("line 3: property 'SpawnSlot' is allowed only in a script file of an entity", errors[0].Message);
        }

        [Test]
        public void EntityScript_NotInRegistry_Error()
        {
            var hero = m_Kit.Asset<EntityModel>("Hero");
            EntityModelEditing.SetKind(hero, EntityKind.Card);
            hero.SetScript("Hero");

            var errors = ContentValidator.Validate(new ContentAsset[] { hero }, null);

            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            Assert.AreEqual(ContentErrorKind.InvalidScript, errors[0].Kind);
            StringAssert.Contains("script file 'Hero' of the entity is not in the registry", errors[0].Message);
        }

        [Test]
        public void EmptyId_Error()
        {
            var errors = ContentValidator.Validate(new ContentAsset[] { m_Kit.Asset<TestCardModel>("", "Nameless") }, null);

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(ContentErrorKind.EmptyId, errors[0].Kind);
            Assert.AreEqual("<memory:Nameless>", errors[0].Path);
        }

        [TestCase("1monster")]
        [TestCase("monster 1")]
        [TestCase("monster-1")]
        [TestCase("cards.monster")]
        public void InvalidId_Error(string id)
        {
            var errors = ContentValidator.Validate(new ContentAsset[] { m_Kit.Asset<TestCardModel>(id, "Bad") }, null);

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(ContentErrorKind.InvalidId, errors[0].Kind);
            StringAssert.Contains($"'{id}'", errors[0].Message);
        }

        [Test]
        public void DuplicateId_InSameScope_ErrorForEachAsset()
        {
            var card = m_Kit.Asset<TestCardModel>("lucia", "CardLucia");
            var hero = m_Kit.Asset<TestHeroModel>("lucia", "HeroLucia");
            var location = m_Kit.Asset<TestLocationModel>("lucia", "LocationLucia");

            var errors = ContentValidator.Validate(new ContentAsset[] { card, hero, location }, null);

            Assert.AreEqual(2, errors.Count, string.Join("\n", errors));
            Assert.IsTrue(errors.All(error => error.Kind == ContentErrorKind.DuplicateId));
            Assert.That(errors.Select(error => error.Path), Is.EquivalentTo(new[] { "<memory:CardLucia>", "<memory:HeroLucia>" }));
            StringAssert.Contains("duplicate TestCardModel id 'lucia'", errors[0].Message);
            StringAssert.Contains("<memory:HeroLucia>", errors[0].Message);
        }

        [Test]
        public void MissingAsset_Error()
        {
            var errors = ContentValidator.Validate(new ContentAsset[] { null }, null);

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(ContentErrorKind.MissingAsset, errors[0].Kind);
        }

        [Test]
        public void ScriptIds_InvalidAndDuplicate_Errors()
        {
            var errors = ContentValidator.Validate(null, new[]
            {
                m_Kit.Script("Hub", "Start:\n"),
                m_Kit.Script("Hub", "Start:\n", ScriptFileKind.Dialog),
                m_Kit.Script("hub intro", "Start:\n"),
            });

            Assert.AreEqual(3, errors.Count, string.Join("\n", errors));
            Assert.AreEqual(1, errors.Count(error => error.Kind == ContentErrorKind.InvalidScriptId));
            Assert.AreEqual(2, errors.Count(error => error.Kind == ContentErrorKind.DuplicateScriptId));
        }

        [TestCase("Monster 1", "Monster_1")]
        [TestCase("  forest-path ", "forest_path")]
        [TestCase("1st", "_1st")]
        [TestCase("Лес", "Лес")]
        [TestCase("", "_")]
        public void MakeId_FromName_IsValidId(string name, string expected)
        {
            var id = ContentAsset.MakeId(name);

            Assert.AreEqual(expected, id);
            Assert.IsTrue(ContentAsset.IsValidId(id));
        }

        [Test]
        public void IdScope_IsDirectContentAssetSubclass()
        {
            Assert.AreEqual(typeof(TestCardModel), ContentAsset.GetIdScope(typeof(TestHeroModel)));
            Assert.AreEqual(typeof(TestCardModel), ContentAsset.GetIdScope(typeof(TestCardModel)));
            Assert.AreEqual(typeof(TestLocationModel), ContentAsset.GetIdScope(typeof(TestLocationModel)));
        }
    }
}
