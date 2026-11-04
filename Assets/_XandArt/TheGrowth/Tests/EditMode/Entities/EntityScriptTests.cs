using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;
using TheGrowth.Model;
using TheGrowth.Model.Slots;
using TheGrowth.Tests.EditMode.Scripting;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Entities
{
    /// <summary>
    /// Сущности в скриптах: <c>[ModelKind]</c> у параметров-конфигов (ошибка разбора для конфига не того вида), переменные
    /// карты у сущности (<c>[active entity].имя</c>): значение по умолчанию, запись переменной, которой нет у конфига, — ошибка
    /// исполнения с местом в скрипте.
    /// </summary>
    public sealed class EntityScriptTests
    {
        private EntityTestKit m_Kit;
        private Session m_Session;
        private ScriptParser m_Parser;
        private TraceLog m_Trace;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new EntityTestKit();
            m_Session = m_Kit.NewSession();
            m_Parser = m_Kit.Parser();
            m_Trace = new TraceLog();
        }

        [TearDown]
        public void TearDown()
        {
            m_Session.Dispose();
            m_Kit.Dispose();
        }

        private ScriptParseResult Parse(params string[] lines)
        {
            return m_Parser.Parse("test", ScriptFileKind.Script, "Start:\n" + string.Join("\n", lines) + "\n");
        }

        private Probe Run(Entity entity, params string[] lines)
        {
            var result = Parse(lines);
            Assert.IsFalse(result.HasErrors, result.ToString());
            var runner = m_Session.Scripts;
            return Probe.Watch(runner.Run(result.File, "Start", runner.RootContext.With(new ActiveEntity(entity), m_Trace)));
        }

        private Entity Card(EntityModel model)
        {
            var slots = m_Session.Get<SlotSystem>();
            return slots.Create(model, slots.Inventory);
        }

#region ModelKind

        [Test]
        public void ModelKind_Parameter_AcceptsKind()
        {
            var result = Parse("    modify with Poison", "    place Monster, Wall");

            Assert.IsFalse(result.HasErrors, result.ToString());
            var commands = result.File.Sections[0].Commands;
            Assert.AreSame(m_Kit.Poison, ((ModifierWithCommand)commands[0]).Model);
            Assert.That(((PlaceCommand)commands[1]).Models, Is.EqualTo(new[] { m_Kit.Monster, m_Kit.Wall }));
        }

        [Test]
        public void ModelKind_Parameter_OtherKind_IsParseError()
        {
            var result = Parse("    modify with Monster", "    place Hero, Poison");

            Assert.That(result.Errors.Select(error => (error.Line, error.Message)), Is.EqualTo(new[]
            {
                (2, "[Model]: 'Monster' is a card, expected modifier"),
                (3, "[Models]: 'Poison' is a modifier, expected card or mechanic"),
            }));
        }

        [Test]
        public void ModelKind_UnknownId_IsParseError()
        {
            var result = Parse("    modify with Nothing");

            Assert.That(result.Errors.Select(error => error.Message), Is.EqualTo(new[] { "unknown EntityModel 'Nothing'" }));
        }

#endregion

#region Variables

        [Test]
        public void Variables_ReadDefaults_WriteOnModel()
        {
            var monster = Card(m_Kit.Monster);
            var hero = Card(m_Kit.Hero);

            var probe = Run(monster, "    add 2 to [active entity].charges", "    set [active entity].mood to \"angry\"");
            Assert.IsTrue(probe.IsCompleted);
            Assert.AreEqual(5, monster.Variables.Get(m_Kit.Declaration("charges")), "3 by the model + 2");
            Assert.AreEqual("angry", monster.Variables.Get(m_Kit.Declaration("mood")));

            Run(hero, "    if [[active entity].charges == 2]", "        mark \"declared default\"");
            Assert.That(m_Trace.Entries, Is.EqualTo(new[] { "declared default" }), "not on the model: read as the declared default");
        }

        [Test]
        public void Variables_WriteNotOnModel_IsScriptErrorWithLocation()
        {
            var hero = Card(m_Kit.Hero);

            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape("Script error in 'set' at test:3: Variable 'charges' is not a variable of EntityModel 'Hero'")));
            var probe = Run(hero, "    mark \"before\"", "    set [active entity].charges to 1", "    mark \"after\"");

            Assert.IsTrue(probe.IsCompleted);
            Assert.That(m_Trace.Entries, Is.EqualTo(new[] { "before" }), "the run is aborted");
            Assert.AreEqual(2, hero.Variables.Get(m_Kit.Declaration("charges")), "unchanged");
        }

#endregion
    }
}
