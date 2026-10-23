using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Core.Content;
using TheGrowth.Core.Tags;
using TheGrowth.EditorTools.Content;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Graphs;
using TheGrowth.Tests.EditMode.Locations;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Tags
{
    /// <summary>Реестр тегов: сбор из конфигов, поиск, формат имени, дубли, валидатор контента.</summary>
    public sealed class TagCatalogTests
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
        public void Catalog_CollectsConfigs_FindsByName()
        {
            var field = m_Kit.Asset<TagDeclarations>("FieldTags");
            field.SetTags(new[] { new TagDeclaration("PlayerSpawn", "Hero start"), new TagDeclaration("Exit") });
            var cards = GraphTestKit.Tags(m_Kit, "CardTags", "monster", "Лес_2");

            var catalog = new TagCatalog(new[] { field, cards });

            Assert.IsEmpty(catalog.Errors, string.Join("\n", catalog.Errors));
            Assert.That(catalog.Tags.Select(tag => tag.Name), Is.EqualTo(new[] { "PlayerSpawn", "Exit", "monster", "Лес_2" }));
            Assert.IsTrue(catalog.TryGet("PlayerSpawn", out var spawn));
            Assert.AreEqual("Hero start", spawn.Description);
            Assert.IsTrue(catalog.Contains("monster"));
            Assert.IsFalse(catalog.Contains("Monster"), "names are case-sensitive");
            Assert.IsFalse(catalog.Contains(null));
        }

        [TestCase("")]
        [TestCase("1st")]
        [TestCase("player spawn")]
        [TestCase("player-spawn")]
        public void InvalidName_Error_NotInCatalog(string name)
        {
            var tags = GraphTestKit.Tags(m_Kit, "Tags", name, "Exit");

            var catalog = new TagCatalog(new[] { tags });

            Assert.AreEqual(1, catalog.Errors.Count);
            Assert.AreEqual(name, catalog.Errors[0].Tag);
            StringAssert.Contains(name == "" ? "name is empty" : "not an identifier", catalog.Errors[0].Message);
            Assert.That(catalog.Tags.Select(tag => tag.Name), Is.EqualTo(new[] { "Exit" }));
        }

        [Test]
        public void Duplicate_AcrossConfigs_Error_FirstKept()
        {
            var first = m_Kit.Asset<TagDeclarations>("First");
            first.SetTags(new[] { new TagDeclaration("Exit", "first") });
            var second = m_Kit.Asset<TagDeclarations>("Second");
            second.SetTags(new[] { new TagDeclaration("Exit", "second"), new TagDeclaration("Exit", "third") });

            var catalog = new TagCatalog(new[] { first, second });

            Assert.AreEqual(2, catalog.Errors.Count);
            Assert.IsTrue(catalog.Errors.All(error => error.Source == second && error.Message.Contains("declared twice (also in 'First')")));
            Assert.IsTrue(catalog.TryGet("Exit", out var exit));
            Assert.AreEqual("first", exit.Description);
        }

        [Test]
        public void FromContent_Init_LogsErrors()
        {
            var registry = m_Kit.Registry(new ContentAsset[]
            {
                LocationTestKit.Location(m_Kit, "hub", "HubScene"),
                GraphTestKit.Tags(m_Kit, "Tags", "Exit", "Exit"),
            });

            var catalog = TagCatalog.FromContent(registry);

            Assert.AreEqual(1, catalog.Tags.Count);
            LogAssert.Expect(LogType.Error, new Regex(@"\[Tags\] Tags: tag 'Exit': is declared twice"));
            catalog.Init();
        }

        [Test]
        public void Validator_ReportsTagErrors()
        {
            var tags = GraphTestKit.Tags(m_Kit, "Tags", "Exit", "Exit", "bad name");

            var errors = ContentValidator.Validate(new ContentAsset[] { tags }, null);

            Assert.AreEqual(2, errors.Count, string.Join("\n", errors));
            Assert.IsTrue(errors.All(error => error.Kind == ContentErrorKind.InvalidTag && error.Context == tags));
            Assert.IsTrue(errors.Any(error => error.Message == "tag 'Exit': is declared twice (also in 'Tags')"));
            Assert.IsTrue(errors.Any(error => error.Message.StartsWith("tag 'bad name': name is not an identifier")));
        }
    }
}
