using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TheGrowth.Core.Scripting;
using TheGrowth.Model.Cards;
using TheGrowth.Model.Expeditions;
using TheGrowth.Tests.EditMode.Cards;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheGrowth.Tests.EditMode.Expeditions
{
    /// <summary>
    /// Узлы хода экспедиции без старта экспедиции, на поле 3 × 3 из <see cref="CardTestKit"/>: печать → разбор → печать, контекст
    /// команд, вне экспедиции — предупреждение и ничего (эвалуаторы — 0), <c>move ... without hooks</c> — без хуков размещения.
    /// </summary>
    public sealed class ExpeditionNodeTests
    {
        private CardTestKit m_Kit;
        private CardSession m_Cards;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit();
            m_Cards = new CardSession(m_Kit);
        }

        [TearDown]
        public void TearDown()
        {
            m_Cards.Dispose();
            m_Kit.Dispose();
        }

        private static string Lines(params string[] lines)
        {
            return string.Join("\n", lines) + "\n";
        }

        private ScriptFile ParseOk(string text)
        {
            var result = m_Kit.Parser().Parse("test", ScriptFileKind.Script, text);
            Assert.IsFalse(result.HasErrors, "Unexpected errors:\n" + result);
            return result.File;
        }

#region Language

        [Test]
        public void RoundTrip_ExpeditionNodes()
        {
            var text = Lines(
                "Start:",
                "    end turn",
                "    end expedition with victory",
                "    end expedition with defeat",
                "    set turns left to 25",
                "    set turns left to [[turns left] + 1]",
                "    add 3 to turns left",
                "    add -1 to turns left",
                "    add [turn number] to turns left",
                "    if [[turns left] <= 0]",
                "        end expedition with defeat",
                "    else",
                "        if [[turn number] > 1]",
                "            end turn",
                "    set [active card].charges to [turns left]",
                "    set $clues to [turn number]",
                "    add 1 to $clues",
                "    move [active card] to [hand] without hooks",
                "    move [active card] to [slot of [hero]]");
            var printer = m_Kit.Printer();

            var printed = printer.Print(ParseOk(text));

            Assert.AreEqual(text, printed);
            Assert.AreEqual(printed, printer.Print(ParseOk(printed)));
        }

        [Test]
        public void Parse_NodeTypes_ResultByNameOrToken()
        {
            var file = ParseOk(Lines(
                "Start:",
                "    end expedition with Victory",
                "    end expedition with defeat",
                "    set turns left to 5",
                "    add 2 to turns left",
                "    move [active card] to [hand] without hooks",
                "    move [active card] to [hand]"));
            var commands = file.Sections[0].Commands;

            Assert.AreEqual(ExpeditionResult.Victory, ((EndExpeditionCommand)commands[0]).Result);
            Assert.AreEqual(ExpeditionResult.Defeat, ((EndExpeditionCommand)commands[1]).Result);
            Assert.IsInstanceOf<SetTurnsLeftCommand>(commands[2]);
            Assert.IsInstanceOf<AddTurnsLeftCommand>(commands[3]);
            Assert.IsInstanceOf<MoveCardWithoutHooks>(commands[4]);
            Assert.IsInstanceOf<MoveCard>(commands[5]);
            Assert.AreEqual(Lines("Start:", "    end expedition with victory"), m_Kit.Printer().Print(ParseOk(Lines("Start:", "    end expedition with Victory"))),
                "the printer writes the token");
        }

        [Test]
        public void Dialog_MoveWithoutHooksNotAllowed_TurnNodesAllowed()
        {
            var result = m_Kit.Parser().Parse("test", ScriptFileKind.Dialog, Lines(
                "Start:",
                "    move [active card] to [hand] without hooks",
                "    end turn",
                "    end expedition with defeat",
                "    add 1 to turns left"));

            Assert.That(result.Errors.Select(error => error.Line), Is.EqualTo(new[] { 2 }), result.ToString());
            StringAssert.Contains("'move_without_hooks' is not allowed in dialog files", result.Errors[0].Message);
        }

        [Test]
        public void ExpeditionVariable_WithOwner_ParseError()
        {
            var result = m_Kit.Parser().Parse("test", ScriptFileKind.Script, Lines("Start:", "    set [active card].clues to 2"));

            Assert.IsTrue(result.HasErrors);
            StringAssert.Contains("'clues' is a expedition variable: write $clues", result.Errors[0].Message);
        }

#endregion

#region Outside of an expedition

        [Test]
        public void OutsideExpedition_Warnings_NothingChanged_EvaluatorsZero()
        {
            var monster = m_Cards.Place(m_Kit.Monster, "a1");

            LogAssert.Expect(LogType.Warning, new Regex(@"\[Expedition\] test:2: end turn: there is no active expedition, the command does nothing"));
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Expedition\] test:3: end expedition with victory: there is no active expedition, the command does nothing"));
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Expedition\] test:4: set turns left: there is no active expedition, the command does nothing"));
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Expedition\] test:5: add to turns left: there is no active expedition, the command does nothing"));
            LogAssert.Expect(LogType.Warning, new Regex(@"turns left: there is no active expedition, 0 is used"));
            LogAssert.Expect(LogType.Warning, new Regex(@"turn number: there is no active expedition, 0 is used"));
            m_Cards.Run(monster,
                "end turn",
                "end expedition with victory",
                "set turns left to 5",
                "add 1 to turns left",
                "set [active card].charges to [turns left]",
                "add [turn number] to [active card].charges");

            var expedition = m_Cards.Expedition;
            Assert.IsFalse(expedition.IsActive);
            Assert.AreEqual(0, expedition.TurnsLeft);
            Assert.AreEqual(0, expedition.TurnNumber);
            Assert.AreEqual(0, m_Cards.Charges(monster));
            Assert.AreSame(m_Cards.Cell("a1"), m_Cards.Slots.GetSlot(monster));
        }

#endregion

#region Move without hooks

        [Test]
        public void MoveWithoutHooks_NoPlacementHooks_MoveWithHooks_Contrast()
        {
            var gamma = m_Cards.Place(m_Kit.Gamma, "a1");
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");
            var beta = m_Cards.Place(m_Kit.Beta, "c3");

            Assert.IsEmpty(m_Cards.RunTrace(alpha, "move [active card] to [random slot of [slots tagged Exit]] without hooks"),
                "no OnUnCovered of Gamma, no OnCovered of Beta, no OnPlaced of Alpha");
            Assert.That(m_Cards.Cell("c3").Entities, Is.EqualTo(new[] { beta, alpha }));

            Assert.That(m_Cards.RunTrace(gamma, "move [active card] to [random slot of [slots tagged Exit]]"),
                Is.EqualTo(new[] { "Alpha: OnCovered <- Gamma", "Gamma: OnPlaced" }));
            Assert.That(m_Cards.Cell("c3").Entities, Is.EqualTo(new[] { beta, alpha, gamma }));
        }

        [Test]
        public void MoveWithoutHooks_FullSlot_Warning_EmptyValues_Nothing()
        {
            var alpha = m_Cards.Place(m_Kit.Alpha, "a1");
            for (var i = 0; i < 3; i++)
                m_Cards.Place(m_Kit.Plain, m_Cards.Slots.Expedition.Hand);

            LogAssert.Expect(LogType.Warning, new Regex(@"test:2: move .*Alpha.*slot 'exp/hand' is full \(3\), the command does nothing"));
            m_Cards.Run(alpha, "move [active card] to [hand] without hooks");
            Assert.AreSame(m_Cards.Cell("a1"), m_Cards.Slots.GetSlot(alpha));

            m_Cards.Run(null, "move [active card] to [backpack] without hooks", "move [hero] to [slot of [hero]] without hooks");
            Assert.AreSame(m_Cards.Cell("a1"), m_Cards.Slots.GetSlot(alpha));
        }

#endregion
    }
}
