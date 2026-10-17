using NUnit.Framework;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Scripting
{
    /// <summary>Разобранное дерево исполняется <see cref="ScriptRunner"/>.</summary>
    public sealed class ScriptLanguageExecutionTests : ScriptTestBase
    {
        private static ScriptRegistry m_Registry;

        private static ScriptRegistry Registry => m_Registry ??= ScriptRegistry.Build(ScriptLanguageTypes.All());

        private ScriptFile AddParsed(string id, string text)
        {
            var result = new ScriptParser(Registry, null, new TestVariableResolver()).Parse(id, ScriptFileKind.Script, text);
            Assert.IsFalse(result.HasErrors, result.ToString());
            return Library.Add(result.File);
        }

        [TestCase(5, new[] { "start", "other sub", "other tail", "r", "big", "end", "sub" })]
        [TestCase(1, new[] { "start", "other sub", "other tail", "r", "small", "after if" })]
        public void ParsedScript_GotoAndCallAcrossSectionsAndFiles(int x, string[] expected)
        {
            AddParsed("other", string.Join("\n",
                "Sub:",
                "    mark \"other sub\"",
                "    goto Tail",
                "",
                "Tail:",
                "    mark \"other tail\""));
            var file = AddParsed("test", string.Join("\n",
                "Start:",
                "    mark \"start\"",
                "    call other.Sub",
                "    repeat",
                "        // comment is a no-op",
                "        mark \"r\"",
                "    until [flag true]",
                "    if [$x > 3]",
                "        mark \"big\"",
                "        goto End",
                "    else",
                "        mark \"small\"",
                "    mark \"after if\"",
                "",
                "End:",
                "    mark \"end\"",
                "    call Sub",
                "    return",
                "    mark \"after return\"",
                "",
                "Sub:",
                "    mark \"sub\""));

            var probe = Probe.Watch(Runner.Run(file, "Start", Context.With(new Counter { Value = x })));

            Assert.IsTrue(probe.IsCompleted);
            Assert.IsNull(probe.Exception);
            Assert.That(Trace.Entries, Is.EqualTo(expected));
            Assert.AreEqual(0, Runner.ActiveCount);
        }
    }
}
