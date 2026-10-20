using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;
using TheGrowth.EditorTools.Content;

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
