using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Scripting
{
    /// <summary>
    /// Реестр (встроенные команды + тестовые узлы), резолверы, разбор и печать.
    /// </summary>
    public abstract class ScriptLanguageTestBase
    {
        private static ScriptRegistry m_Registry;

        protected static ScriptRegistry Registry => m_Registry ??= ScriptRegistry.Build(ScriptLanguageTypes.All());

        protected TestContentResolver Content { get; private set; }

        protected TestVariableResolver Variables { get; private set; }

        protected ScriptParser Parser { get; private set; }

        protected ScriptPrinter Printer { get; private set; }

        [SetUp]
        public void SetUpLanguage()
        {
            Content = new TestContentResolver();
            Content.Add("apple");
            Content.Add("Lucia");
            Content.Add("happy");
            Variables = new TestVariableResolver();
            Parser = new ScriptParser(Registry, Content, Variables);
            Printer = new ScriptPrinter(Registry, Content, Variables);
        }

        [TearDown]
        public void TearDownLanguage()
        {
            Content.Dispose();
        }

        protected ScriptParseResult Parse(string text, ScriptFileKind kind = ScriptFileKind.Script, string fileId = "test")
        {
            return Parser.Parse(fileId, kind, text);
        }

        /// <summary>Разбор без ошибок.</summary>
        protected ScriptFile ParseOk(string text, ScriptFileKind kind = ScriptFileKind.Script, string fileId = "test")
        {
            var result = Parse(text, kind, fileId);
            Assert.IsFalse(result.HasErrors, "Unexpected errors:\n" + result);
            return result.File;
        }

        /// <summary>Единственная команда единственной секции.</summary>
        protected T ParseSingle<T>(string line, ScriptFileKind kind = ScriptFileKind.Script) where T : Command
        {
            var file = ParseOk("S:\n    " + line + "\n", kind);
            Assert.AreEqual(1, file.Sections[0].Commands.Count);
            return (T)file.Sections[0].Commands[0];
        }

        protected ScriptError[] ParseErrors(string text, ScriptFileKind kind = ScriptFileKind.Script)
        {
            var result = Parse(text, kind);
            Assert.IsTrue(result.HasErrors, "Expected errors, got none");
            return result.Errors.ToArray();
        }

        /// <summary>Текст канонический: разбор → печать даёт тот же текст, повторно — тоже.</summary>
        protected void AssertRoundTrip(string text, ScriptFileKind kind = ScriptFileKind.Script)
        {
            var printed = Printer.Print(ParseOk(text, kind));
            Assert.AreEqual(text, printed, "print(parse(text)) differs");
            Assert.AreEqual(printed, Printer.Print(ParseOk(printed, kind)), "print(parse(print)) differs");
        }

        protected static string Lines(params string[] lines)
        {
            return string.Join("\n", lines) + "\n";
        }
    }
}
