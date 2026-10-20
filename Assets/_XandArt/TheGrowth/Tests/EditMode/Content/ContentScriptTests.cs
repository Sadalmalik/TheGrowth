using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Content
{
    /// <summary>ID конфигов в скриптах через реестр контента: разбор, печать, круговой тест.</summary>
    public sealed class ContentScriptTests
    {
        private ContentTestKit m_Kit;
        private TestCardModel m_Monster;
        private TestHeroModel m_Hero;
        private TestLocationModel m_Forest;
        private ContentRegistry m_Registry;
        private ScriptParser m_Parser;
        private ScriptPrinter m_Printer;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Monster = m_Kit.Asset<TestCardModel>("monster_1");
            m_Hero = m_Kit.Asset<TestHeroModel>("lucia");
            m_Forest = m_Kit.Asset<TestLocationModel>("forest");
            m_Registry = m_Kit.Registry(new ContentAsset[] { m_Monster, m_Hero, m_Forest });

            var resolver = new ContentScriptResolver(m_Registry);
            m_Parser = new ScriptParser(ContentTestKit.Language, resolver);
            m_Printer = new ScriptPrinter(ContentTestKit.Language, resolver);
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        [Test]
        public void Parse_ConfigIds_ResolveToRegistryAssets()
        {
            var file = ParseOk("S:\n    spawn monster_1\n    spawn lucia\n    summon lucia\n    travel to forest\n");
            var commands = file.Sections[0].Commands;

            Assert.AreSame(m_Monster, ((SpawnCardCommand)commands[0]).Card);
            Assert.AreSame(m_Hero, ((SpawnCardCommand)commands[1]).Card, "derived type fits base-type parameter");
            Assert.AreSame(m_Hero, ((SummonHeroCommand)commands[2]).Hero);
            Assert.AreSame(m_Forest, ((TravelCommand)commands[3]).Place);
        }

        [Test]
        public void Parse_UnknownOrWrongTypeId_ErrorWithLocation()
        {
            var result = m_Parser.Parse("test", ScriptFileKind.Script, "S:\n    spawn nope\n    summon monster_1\n    travel to monster_1\n");

            Assert.That(result.Errors.Select(error => error.ToString()), Is.EqualTo(new[]
            {
                "test:2:11: unknown TestCardModel 'nope'",
                "test:3:12: unknown TestHeroModel 'monster_1'",
                "test:4:15: unknown TestLocationModel 'monster_1'",
            }));
        }

        [Test]
        public void RoundTrip_ConfigIds()
        {
            const string text = "S:\n    spawn monster_1\n    summon lucia\n    travel to forest\n";

            var printed = m_Printer.Print(ParseOk(text));

            Assert.AreEqual(text, printed);
            Assert.AreEqual(printed, m_Printer.Print(ParseOk(printed)));
        }

        [Test]
        public void Print_AssetNotInRegistry_PrintsMissingValue()
        {
            var foreign = m_Kit.Asset<TestCardModel>("monster_1");
            var command = new SpawnCardCommand { Card = foreign };

            Assert.AreEqual("spawn " + ScriptSyntax.MissingValue + "\n", m_Printer.PrintCommand(command));
        }

        private ScriptFile ParseOk(string text)
        {
            var result = m_Parser.Parse("test", ScriptFileKind.Script, text);
            Assert.IsFalse(result.HasErrors, "Unexpected errors:\n" + result);
            return result.File;
        }
    }
}
