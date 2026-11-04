using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using TheGrowth.Architecture.Events;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Variables;
using TheGrowth.Model.Decks;
using TheGrowth.Model.Expeditions;
using TheGrowth.Tests.EditMode.Cards;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Expeditions
{
    /// <summary>
    /// Переменные области «экспедиция» (<see cref="VariableScope.Expedition"/>, <see cref="IExpeditionVariables"/>) на поле 3 × 3
    /// из <see cref="CardTestKit"/> (переменная <c>clues</c>, int, по умолчанию 1; тестовый герой): вне экспедиции чтение —
    /// значение по умолчанию, запись ничего не делает (с предупреждением); в экспедиции — чтение, запись, событие только при
    /// изменении; сброс при старте, в сейве только явно заданные значения, удаление в конце; у карты такой переменной быть не
    /// может.
    /// </summary>
    public sealed class ExpeditionVariableTests
    {
        private CardTestKit m_Kit;
        private CardSession m_Cards;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit();
        }

        [TearDown]
        public void TearDown()
        {
            m_Cards?.Dispose();
            m_Cards = null;
            m_Kit.Dispose();
        }

        private ExpeditionSystem Expedition => m_Cards.Expedition;

        private VariableDeclaration Clues => m_Kit.Global.Get<VariableCatalog>().Get("clues");

        /// <summary>Колода — одна трава «не раскладывать»; сессия без слотов экспедиции.</summary>
        private void Open()
        {
            m_Kit.Deck(new DeckEntry(m_Kit.Herb, 1, true));
            m_Cards = new CardSession(m_Kit, m_Kit.NewSession(CardTestKit.DefaultSeed, false));
        }

        private Entity Start()
        {
            var hero = m_Cards.Slots.Create(m_Kit.Hero, m_Cards.Slots.Inventory);
            CardSession.Complete(Expedition.Start(m_Kit.Location, hero));
            return hero;
        }

        private static bool Done(UniTask<bool> task)
        {
            var awaiter = task.GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted, "the action is not completed synchronously");
            return awaiter.GetResult();
        }

        [Test]
        public void OutsideExpedition_ReadDefault_WriteNothing_Warnings()
        {
            Open();
            var monster = m_Cards.Place(m_Kit.Monster, m_Cards.Slots.Inventory);

            LogAssert.Expect(LogType.Warning, new Regex(@"\[Variables\] test\.Start: expedition variable '\$clues' is written outside of an expedition, nothing is changed"));
            LogAssert.Expect(LogType.Warning, new Regex(@"expedition variable '\$clues' is written outside of an expedition, nothing is changed"));
            LogAssert.Expect(LogType.Warning, new Regex(@"expedition variable '\$clues' is read outside of an expedition, the default value is used"));
            m_Cards.Run(monster, "set $clues to 3", "add 2 to $clues", "set [active card].charges to $clues");

            Assert.AreEqual(1, m_Cards.Charges(monster), "the default value");
            Assert.IsNull(Expedition.Variables);
            Assert.IsNull(((IExpeditionVariables)Expedition).Variables);
        }

        [Test]
        public void InExpedition_ReadWrite_ChangedOnlyOnRealChange()
        {
            Open();
            Start();
            var monster = m_Cards.Place(m_Kit.Monster, "c3");
            var bus = new List<VariableChanged>();
            var store = new List<VariableChanged>();
            m_Kit.Global.Get<EventBus>().Subscribe<VariableChanged>(change =>
            {
                if (change.Scope == VariableScope.Expedition)
                    bus.Add(change);
            });
            Expedition.Variables.Changed += store.Add;

            m_Cards.Run(monster, "set $clues to 3", "add 2 to $clues", "set $clues to 5", "set [active card].charges to $clues");

            Assert.AreEqual(5, m_Cards.Charges(monster));
            Assert.AreEqual(5, Expedition.Variables.Get<int>(Clues));
            Assert.That(bus.Select(change => (change.OldValue, change.NewValue)), Is.EqualTo(new[] { ((object)1, (object)3), (3, 5) }),
                "setting the same value raises no event");
            Assert.That(store.Select(change => change.Name), Is.EqualTo(new[] { "clues", "clues" }));
            Assert.IsNull(bus[0].Owner);
        }

        [Test]
        public void ResetAtStart_SavedOnlyWhenSet_DeletedAtEnd()
        {
            var ender = m_Kit.Model("Ender", "OnCovered:\n    end expedition with victory\n");
            Open();
            var hero = Start();
            Assert.AreEqual(1, Expedition.Variables.Get<int>(Clues));
            Assert.IsFalse(Expedition.Variables.IsSet(Clues));
            Assert.AreEqual(0, ((JObject)m_Cards.Session.Capture().GetSection(ExpeditionSystem.Key).Data["variables"]).Count, "only set values are saved");

            m_Cards.Run(null, "set $clues to 4");
            Assert.AreEqual(4, (int)m_Cards.Session.Capture().GetSection(ExpeditionSystem.Key).Data["variables"]["clues"]);
            m_Cards.Place(ender, "a2");
            Assert.IsTrue(Done(Expedition.Play(hero, m_Cards.Cell("a2"))));

            Assert.IsFalse(Expedition.IsActive);
            Assert.IsNull(Expedition.Variables, "deleted at the end of the expedition");
            Assert.AreEqual(0, ((JObject)m_Cards.Session.Capture().GetSection(ExpeditionSystem.Key).Data["variables"]).Count);

            CardSession.Complete(Expedition.Start(m_Kit.Location, hero));
            Assert.AreEqual(1, Expedition.Variables.Get<int>(Clues), "reset at the start");
            Assert.IsFalse(Expedition.Variables.IsSet(Clues));
        }

        [Test]
        public void SaveAndLoad_ExpeditionVariables()
        {
            Open();
            Start();
            m_Cards.Run(null, "set $clues to 7");

            var loaded = new CardSession(m_Kit, m_Kit.Reload(m_Cards.Session));
            try
            {
                Assert.AreEqual(7, loaded.Expedition.Variables.Get<int>(Clues));
                Assert.IsTrue(loaded.Expedition.Variables.IsSet(Clues));
            }
            finally
            {
                loaded.Dispose();
            }
        }

        [Test]
        public void CardVariable_ExpeditionScope_Error()
        {
            var model = m_Kit.Model("Bad", null);
            model.VariableList.Add(new EntityVariable("clues", "2"));
            Open();

            var schema = new EntityVariableSchema(model, m_Kit.Global.Get<VariableCatalog>());

            Assert.That(schema.Errors, Has.Some.Contains("variable 'clues' is a expedition variable, only card variables can be on an entity"));
            Assert.IsFalse(schema.TryGetDefault(Clues, out _));
        }
    }
}
