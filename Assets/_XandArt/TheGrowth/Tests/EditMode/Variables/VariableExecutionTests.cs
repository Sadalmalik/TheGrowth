using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Profile;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Variables;
using TheGrowth.Model.Variables;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Scripting;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Variables
{
    /// <summary>
    /// Исполнение разобранных скриптов с переменными: <c>set</c>, <c>add</c>, сравнения, логика, арифметика, переменные
    /// владельца, события. Область: шина, объявления, профиль в памяти, переменные игры, исполнитель.
    /// </summary>
    public sealed class VariableExecutionTests
    {
        private ContentTestKit m_Kit;
        private LifetimeScope m_Scope;
        private VariableCatalog m_Catalog;
        private EventBus m_Events;
        private MemoryProfileStorage m_ProfileStorage;
        private VariablesSystem m_Game;
        private ProfileService m_Profile;
        private TestScriptLibrary m_Library;
        private ScriptRunner m_Runner;
        private ScriptParser m_Parser;
        private TraceLog m_Trace;
        private TestVariableOwner m_Owner;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Catalog = VariableTestKit.Catalog(m_Kit, VariableTestKit.Standard());

            m_Scope = new LifetimeScope("VariableTests");
            var container = m_Scope.Container;
            m_Events = container.Add(new EventBus());
            container.Add(m_Catalog);
            m_ProfileStorage = container.Add(new MemoryProfileStorage());
            m_Profile = container.Add(new ProfileService());
            m_Game = container.Add(new VariablesSystem());
            m_Library = container.Add(new TestScriptLibrary());
            m_Runner = container.Add(new ScriptRunner());
            container.Init();

            m_Parser = new ScriptParser(VariableTestKit.Language, null, new VariableScriptResolver(m_Catalog));
            m_Trace = new TraceLog();
            m_Owner = new TestVariableOwner("wolf", m_Events);
        }

        [TearDown]
        public void TearDown()
        {
            m_Scope.Dispose();
            m_Kit.Dispose();
        }

#region Helpers

        private void Run(params string[] lines)
        {
            var result = m_Parser.Parse("test", ScriptFileKind.Script, "Start:\n" + string.Join("\n", lines) + "\n");
            Assert.IsFalse(result.HasErrors, result.ToString());
            m_Library.Add(result.File);

            var probe = Probe.Watch(m_Runner.Run(result.File, "Start", m_Runner.RootContext.With(m_Trace, m_Owner)));
            Assert.IsTrue(probe.IsCompleted);
            Assert.IsNull(probe.Exception);
        }

        private object Game(string name)
        {
            return m_Game.Variables.Get(m_Catalog.Get(name));
        }

        private void SetGame(string name, object value)
        {
            m_Game.Variables.Set(m_Catalog.Get(name), value);
        }

        /// <summary>Строка <c>if [условие]</c> с отметкой в журнале «yes» или «no».</summary>
        private bool Check(string condition)
        {
            m_Trace.Entries.Clear();
            Run($"    if {condition}", "        mark \"yes\"", "    else", "        mark \"no\"");
            return m_Trace.Entries.Count == 1 && m_Trace.Entries[0] == "yes";
        }

#endregion

        [Test]
        public void Set_AllTypesAndScopes()
        {
            Run("    set $coins to 5",
                "    set $met_lucia to true",
                "    set $player_name to \"Ann\"",
                "    set $ending_seen to $met_lucia",
                "    set [active owner].charges to [$coins - 1]");

            Assert.AreEqual(5, Game("coins"));
            Assert.AreEqual(true, Game("met_lucia"));
            Assert.AreEqual("Ann", Game("player_name"));
            Assert.AreEqual(true, m_Profile.Variables.Get(m_Catalog.Get("ending_seen")));
            Assert.AreEqual(4, m_Owner.Variables.Get(m_Catalog.Get("charges")));
            Assert.AreEqual(1, m_ProfileStorage.WriteCount, "the profile is written on change");
        }

        [Test]
        public void Set_ConditionAsBoolValue_EvaluatedAtExecution()
        {
            Run("    set $coins to 5",
                "    set $met_lucia to [$coins > 3]",
                "    set $ending_seen to [[$coins > 10] or [not $met_lucia]]");

            Assert.AreEqual(true, Game("met_lucia"));
            Assert.AreEqual(false, m_Profile.Variables.Get(m_Catalog.Get("ending_seen")));

            Run("    set $coins to 1",
                "    set $met_lucia to [$coins > 3]");

            Assert.AreEqual(false, Game("met_lucia"));
        }

        [Test]
        public void Add_AddsAndSubtracts_FromDefault()
        {
            Run("    add 5 to $coins", "    add -2 to $coins", "    add [$coins + 1] to $gold", "    add -1 to [active owner].charges");

            Assert.AreEqual(3, Game("coins"));
            Assert.AreEqual(4, Game("gold"));
            Assert.AreEqual(1, m_Owner.Variables.Get(m_Catalog.Get("charges")), "the default 2 minus 1");
        }

        [TestCase(4, 5, new[] { "near" })]
        [TestCase(3, 0, new string[0])]
        public void SpecExample_IfTrustAddCoins(int trust, int coins, string[] trace)
        {
            SetGame("trust_lucia", trust);

            Run("    if [$trust_lucia > 3]", "        add 5 to $coins", "        mark \"near\"");

            Assert.AreEqual(coins, Game("coins"));
            Assert.That(m_Trace.Entries, Is.EqualTo(trace));
        }

        [TestCase("[$coins > 2]", true)]
        [TestCase("[$coins > 3]", false)]
        [TestCase("[$coins < 4]", true)]
        [TestCase("[$coins < 3]", false)]
        [TestCase("[$coins >= 3]", true)]
        [TestCase("[$coins >= 4]", false)]
        [TestCase("[$coins <= 3]", true)]
        [TestCase("[$coins <= 2]", false)]
        [TestCase("[$coins == 3]", true)]
        [TestCase("[$coins == 4]", false)]
        [TestCase("[$coins != 4]", true)]
        [TestCase("[3 != $coins]", false)]
        [TestCase("[$met_lucia == true]", true)]
        [TestCase("[$met_lucia != true]", false)]
        [TestCase("[$player_name == \"Ann\"]", true)]
        [TestCase("[$player_name == \"ann\"]", false)]
        [TestCase("[$player_name != \"Player\"]", true)]
        [TestCase("[[active owner].charges == 2]", true)]
        public void Comparisons(string condition, bool expected)
        {
            SetGame("coins", 3);
            SetGame("met_lucia", true);
            SetGame("player_name", "Ann");

            Assert.AreEqual(expected, Check(condition));
        }

        [TestCase("$met_lucia", true)]
        [TestCase("$ending_seen", false)]
        [TestCase("[not $ending_seen]", true)]
        [TestCase("[$met_lucia and $ending_seen]", false)]
        [TestCase("[$met_lucia and [not $ending_seen]]", true)]
        [TestCase("[$ending_seen or $met_lucia]", true)]
        [TestCase("[$ending_seen or [$coins > 5]]", false)]
        [TestCase("[not [$ending_seen or [not $met_lucia]]]", true)]
        public void Logic(string condition, bool expected)
        {
            SetGame("met_lucia", true);

            Assert.AreEqual(expected, Check(condition));
        }

        [Test]
        public void Arithmetic_PlusMinus_NestedWithoutPrecedence()
        {
            SetGame("coins", 10);

            Run("    set $gold to [[$coins + 5] - [$coins - 3]]", "    set $trust_lucia to [-2 - -7]");

            Assert.AreEqual(8, Game("gold"));
            Assert.AreEqual(5, Game("trust_lucia"));
        }

        [Test]
        public void Changes_AreEventBusEvents()
        {
            var changes = new List<VariableChanged>();
            using (VariableTestKit.Record(m_Events, changes))
            {
                Run("    set $coins to 1", "    set $coins to 1", "    add 2 to $coins", "    set $ending_seen to true", "    add 1 to [active owner].charges");
            }

            Assert.That(changes.ConvertAll(change => change.ToString()), Is.EqualTo(new[]
            {
                "game coins: 0 -> 1",
                "game coins: 1 -> 3",
                "profile ending_seen: False -> True",
                "card charges: 2 -> 3",
            }));
            Assert.AreSame(m_Owner, changes[3].Owner);
            Assert.IsNull(changes[0].Owner);
        }

        [Test]
        public void Overflow_IsScriptError_ValueUnchanged()
        {
            SetGame("coins", int.MaxValue);

            LogAssert.Expect(LogType.Error, new Regex("Script error in 'add' at test:2: .*overflow", RegexOptions.IgnoreCase));
            var result = m_Parser.Parse("test", ScriptFileKind.Script, "Start:\n    add 1 to $coins\n");
            var probe = Probe.Watch(m_Runner.Run(result.File, "Start", m_Runner.RootContext.With(m_Trace)));

            Assert.IsTrue(probe.IsCompleted);
            Assert.AreEqual(int.MaxValue, Game("coins"));
        }

        [Test]
        public void OwnerVariable_WithoutOwnerInContext_IsScriptError()
        {
            LogAssert.Expect(LogType.Error, new Regex("Script error in 'set' at test:2: .*TestVariableOwner"));
            var result = m_Parser.Parse("test", ScriptFileKind.Script, "Start:\n    set [active owner].charges to 1\n");
            var probe = Probe.Watch(m_Runner.Run(result.File, "Start", m_Runner.RootContext.With(m_Trace)));

            Assert.IsTrue(probe.IsCompleted);
        }
    }
}
