using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Variables;
using TheGrowth.Model;
using TheGrowth.Model.Variables;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Saves;
using TheGrowth.Tests.EditMode.Scripting;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Variables
{
    /// <summary>
    /// Секция <c>variables</c> через <see cref="Session.Create"/>: значения по умолчанию, только явно заданные значения в
    /// сейве, удалённая, переименованная и сменившая тип переменная, новое значение по умолчанию для старого сейва.
    /// </summary>
    public sealed class VariablesSaveTests
    {
        private ContentTestKit m_Kit;
        private LifetimeScope m_Global;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Global = CreateGlobal(VariableTestKit.Standard());
        }

        [TearDown]
        public void TearDown()
        {
            m_Global.Dispose();
            m_Kit.Dispose();
        }

        private LifetimeScope CreateGlobal(VariableDeclaration[] variables)
        {
            var registry = m_Kit.Registry(new ContentAsset[] { VariableTestKit.Declarations(m_Kit, "Variables", variables) });
            return VariableTestKit.Global(registry);
        }

        private static SaveGame SaveWithValues(string json)
        {
            var save = new SaveGame();
            save.AddSection(new SaveSection(VariablesSystem.Key, 1, JObject.Parse("{ \"values\": " + json + " }")));
            return save;
        }

        private static VariableStore Variables(Session session)
        {
            return session.Get<VariablesSystem>().Variables;
        }

        private static object Get(Session session, string name)
        {
            var catalog = session.Get<VariableCatalog>();
            return Variables(session).Get(catalog.Get(name));
        }

        [Test]
        public void NewGame_Defaults_EmptySection()
        {
            var session = Session.Create(m_Global, new SaveGame());

            Assert.AreSame(session.Get<VariablesSystem>(), session.Get<IGameVariables>());
            Assert.AreEqual(0, Get(session, "coins"));
            Assert.AreEqual("Player", Get(session, "player_name"));

            var section = session.Capture().GetSection(VariablesSystem.Key);
            Assert.AreEqual(1, section.Version);
            Assert.AreEqual("{\"values\":{}}", section.Data.ToString(Formatting.None));
            session.Dispose();
        }

        [Test]
        public void Capture_OnlySetValues_LoadRestores()
        {
            var first = Session.Create(m_Global, new SaveGame());
            var catalog = first.Get<VariableCatalog>();
            Variables(first).Set(catalog.Get("coins"), 7);
            Variables(first).Set(catalog.Get("player_name"), "Ann");
            Variables(first).Set(catalog.Get("met_lucia"), false);

            var captured = first.Capture();
            first.Dispose();

            Assert.AreEqual("{\"values\":{\"coins\":7,\"met_lucia\":false,\"player_name\":\"Ann\"}}",
                captured.GetSection(VariablesSystem.Key).Data.ToString(Formatting.None));

            var second = Session.Create(m_Global, SaveTestKit.Unpack(SaveTestKit.Pack(captured)));
            Assert.AreEqual(7, Get(second, "coins"));
            Assert.AreEqual("Ann", Get(second, "player_name"));
            Assert.AreEqual(false, Get(second, "met_lucia"));
            Assert.IsTrue(Variables(second).IsSet(catalog.Get("met_lucia")), "an explicit value equal to the default stays explicit");
            Assert.IsFalse(Variables(second).IsSet(catalog.Get("trust_lucia")));
            second.Dispose();
        }

        [Test]
        public void NewDefault_AppliesToOldSaveWithoutValue()
        {
            var save = SaveWithValues("{ \"coins\": 1 }");
            m_Global.Dispose();
            var variables = VariableTestKit.Standard();
            variables[0] = new VariableDeclaration("trust_lucia", VariableScope.Game, VariableType.Int, "5");
            m_Global = CreateGlobal(variables);

            var session = Session.Create(m_Global, save);

            Assert.AreEqual(5, Get(session, "trust_lucia"));
            Assert.AreEqual(1, Get(session, "coins"));
            session.Dispose();
        }

        [Test]
        public void RemovedVariable_Warning_Ignored()
        {
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape("[Saves] Section 'variables': variable 'removed_var' is not declared, its value is ignored")));

            var session = Session.Create(m_Global, SaveWithValues("{ \"removed_var\": 3, \"coins\": 2 }"));

            Assert.AreEqual(2, Get(session, "coins"));
            Assert.AreEqual("{\"values\":{\"coins\":2}}", session.Capture().GetSection(VariablesSystem.Key).Data.ToString(Formatting.None));
            session.Dispose();
        }

        [Test]
        public void RenamedVariable_ReadUnderNewName_WrittenUnderNewName()
        {
            var session = Session.Create(m_Global, SaveWithValues("{ \"money\": 10 }"));

            Assert.AreEqual(10, Get(session, "gold"));
            Assert.AreEqual("{\"values\":{\"gold\":10}}", session.Capture().GetSection(VariablesSystem.Key).Data.ToString(Formatting.None));
            session.Dispose();
        }

        [Test]
        public void RenamedVariable_BothNamesSaved_NewNameWins()
        {
            LogAssert.Expect(LogType.Warning, new Regex("variable 'money' is renamed to 'gold', which is also saved"));

            var session = Session.Create(m_Global, SaveWithValues("{ \"money\": 10, \"gold\": 4 }"));

            Assert.AreEqual(4, Get(session, "gold"));
            session.Dispose();
        }

        [TestCase("{ \"coins\": \"many\" }", "value 'many' of variable 'coins' is not int")]
        [TestCase("{ \"coins\": 99999999999 }", "of variable 'coins' is not int")]
        [TestCase("{ \"met_lucia\": 1 }", "of variable 'met_lucia' is not bool")]
        [TestCase("{ \"ending_seen\": true }", "variable 'ending_seen' is now a profile variable")]
        public void WrongTypeOrScope_Warning_DefaultUsed(string values, string warning)
        {
            LogAssert.Expect(LogType.Warning, new Regex(Regex.Escape(warning)));

            var session = Session.Create(m_Global, SaveWithValues(values));

            Assert.AreEqual(0, Variables(session).Count);
            session.Dispose();
        }

        [Test]
        public void SessionScripts_ParsedByDeclarations_RunChangesGameAndProfile()
        {
            m_Global.Dispose();
            var profile = new MemoryProfileStorage();
            var registry = m_Kit.Registry(new ContentAsset[] { VariableTestKit.Declarations(m_Kit, "Variables", VariableTestKit.Standard()) }, new[]
            {
                m_Kit.Script("Hub", "Start:\n    if [$trust_lucia == 0]\n        add 5 to $coins\n    set $ending_seen to true\n"),
                m_Kit.Script("Broken", "Start:\n    add 1 to $money\n"),
            });
            m_Global = VariableTestKit.Global(registry, profile);
            var session = Session.Create(m_Global, new SaveGame());
            var library = session.Get<IScriptLibrary>();

            Assert.IsTrue(library.TryGetFile("Hub", out var hub));
            var probe = Probe.Watch(session.Scripts.Run(hub, "Start"));

            Assert.IsTrue(probe.IsCompleted);
            Assert.AreEqual(5, Get(session, "coins"));
            StringAssert.Contains("\"ending_seen\": true", profile.Text);
            Assert.AreEqual("{\"values\":{\"coins\":5}}", session.Capture().GetSection(VariablesSystem.Key).Data.ToString(Formatting.None),
                "profile variables are not in the slot");

            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("variable '$money' is renamed to '$gold'")));
            Assert.IsFalse(library.TryGetFile("Broken", out _));
            session.Dispose();
        }

        [Test]
        public void SessionBuilder_RegistersVariablesSystem_ChangesInEventBus()
        {
            var session = Session.Create(m_Global, new SaveGame());
            var changes = new List<VariableChanged>();
            var catalog = session.Get<VariableCatalog>();

            using (VariableTestKit.Record(m_Global.Get<EventBus>(), changes))
                Variables(session).Set(catalog.Get("trust_lucia"), 2);

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(VariableScope.Game, changes[0].Scope);
            session.Dispose();
        }
    }
}
