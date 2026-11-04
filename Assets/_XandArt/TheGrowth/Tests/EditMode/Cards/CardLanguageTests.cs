using System;
using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;
using TheGrowth.Model.Cards;
using TheGrowth.Model.Slots;

namespace TheGrowth.Tests.EditMode.Cards
{
    /// <summary>
    /// Язык скриптов карт без исполнения: каждый шаблон — печать → разбор → печать, свойства файла (разбор, печать,
    /// ошибки с номером строки), преобразование одиночного значения в набор, литерал-тег, ошибки разбора, проверки реестра
    /// для расширений языка.
    /// </summary>
    public sealed class CardLanguageTests
    {
        private CardTestKit m_Kit;
        private ScriptParser m_Parser;
        private ScriptPrinter m_Printer;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new CardTestKit();
            m_Parser = m_Kit.Parser();
            m_Printer = m_Kit.Printer();
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

        private ScriptParseResult Parse(string text, ScriptFileKind kind = ScriptFileKind.Script)
        {
            return m_Parser.Parse("test", kind, text);
        }

        private ScriptFile ParseOk(string text, ScriptFileKind kind = ScriptFileKind.Script)
        {
            var result = Parse(text, kind);
            Assert.IsFalse(result.HasErrors, "Unexpected errors:\n" + result);
            return result.File;
        }

        /// <summary>Текст канонический: разбор → печать даёт тот же текст, повторно — тоже.</summary>
        private void AssertRoundTrip(string text)
        {
            var printed = m_Printer.Print(ParseOk(text));
            Assert.AreEqual(text, printed, "print(parse(text)) differs");
            Assert.AreEqual(printed, m_Printer.Print(ParseOk(printed)), "print(parse(print)) differs");
        }

        private static string Lines(params string[] lines)
        {
            return string.Join("\n", lines) + "\n";
        }

        private (int Line, string Message)[] Errors(string text, ScriptFileKind kind = ScriptFileKind.Script)
        {
            var result = Parse(text, kind);
            Assert.IsTrue(result.HasErrors, "Expected errors, got none");
            return result.Errors.Select(error => (error.Line, error.Message)).ToArray();
        }

#region Round trip

        [Test]
        public void RoundTrip_CardAndSlotEvaluators()
        {
            AssertRoundTrip(Lines(
                "Start:",
                "    trace [active card] a",
                "    trace [covering card] b",
                "    trace [iterating card] c",
                "    trace [hero] d",
                "    trace [card under [active card]] e",
                "    trace [top card of [slot of [hero]]] f",
                "    trace slot [deck]",
                "    trace slot [hand]",
                "    trace slot [discard]",
                "    trace slot [backpack]",
                "    trace slot [inventory]",
                "    trace slot [nearest of [free slots] to [hero]]"));
        }

        [Test]
        public void RoundTrip_Sets()
        {
            AssertRoundTrip(Lines(
                "Start:",
                "    trace cards [cards in [field slots]]",
                "    trace cards [cards tagged Monster]",
                "    trace cards [[cards in [free slots]] except [cards tagged Hero]]",
                "    trace cards [[cards tagged Exit] or [cards tagged Monster]]",
                "    trace cards [[cards in [field slots]] and [cards tagged Resource]]",
                "    trace slots [field slots]",
                "    trace slots [free slots]",
                "    trace slots [slots tagged Exit]",
                "    trace slots [slots around [active card] within 2]",
                "    trace slots [slots of [cards tagged Monster]]",
                "    trace slots [[field slots] except [slots tagged PlayerSpawn]]",
                "    trace slots [[free slots] or [slots tagged Exit]]",
                "    trace slots [[field slots] and [slots around [hero] within 1]]"));
        }

        [Test]
        public void RoundTrip_ConditionsAndNumbers()
        {
            AssertRoundTrip(Lines(
                "Start:",
                "    if [[active card] has tag Hero]",
                "        trace [active card] a",
                "    if [[slot of [active card]] has tag Exit]",
                "        trace [active card] b",
                "    if [[slot of [hero]] is empty]",
                "        trace [active card] c",
                "    if [[covering card] exists]",
                "        trace [active card] d",
                "    if [[nearest of [free slots] to [hero]] exists]",
                "        trace [active card] e",
                "    if [[active card] == [hero]]",
                "        trace [active card] f",
                "    if [[slot of [active card]] != [slot of [hero]]]",
                "        trace [active card] g",
                "    if [[count of [cards tagged Monster]] > 1]",
                "        trace [active card] h",
                "    if [[count of slots in [free slots]] >= [count of [cards in [field slots]]]]",
                "        trace [active card] i"));
        }

        [Test]
        public void RoundTrip_Commands()
        {
            AssertRoundTrip(Lines(
                "Start:",
                "    move [active card] to [nearest of [free slots] to [hero]]",
                "    flip [covering card]",
                "    reveal [iterating card]",
                "    hide [top card of [deck]]",
                "    destroy [card under [hero]]",
                "    create Herb in [backpack]",
                "    for each card in [cards tagged Monster]",
                "        reveal [iterating card]",
                "        move [iterating card] to [slot of [hero]]"));
        }

        [Test]
        public void RoundTrip_RandomAndChance()
        {
            AssertRoundTrip(Lines(
                "SpawnSlot -> [random slot of [[free slots] except [slots tagged PlayerSpawn]]]",
                "",
                "Start:",
                "    trace [random card of [cards tagged Monster]] a",
                "    trace [random card of [active card]] b",
                "    trace slot [random slot of [free slots]]",
                "    move [active card] to [random slot of [slots around [active card] within 1]]",
                "    if [chance 50%]",
                "        trace [active card] c",
                "    if [chance [active card].charges%]",
                "        trace [active card] d",
                "    if [chance [[active card].charges + 10]%]",
                "        trace [active card] e",
                "    if [[random number from 1 to 6] > 3]",
                "        trace [active card] f",
                "    set [active card].charges to [random number from [count of [cards tagged Monster]] to [active card].charges]",
                "    set [active card].charges to [random number from -3 to 0]"));
        }

        [Test]
        public void Chance_SpaceBeforePercent_ParsedAndPrintedCanonically()
        {
            var file = ParseOk(Lines("Start:", "    if [chance 25 %]", "        trace [active card] a"));

            Assert.AreEqual(Lines("Start:", "    if [chance 25%]", "        trace [active card] a"), m_Printer.Print(file));
        }

        [Test]
        public void RoundTrip_SingleValueWhereSetExpected_PrintsSourceNode()
        {
            var text = Lines(
                "AllowedMoves -> [slot of [active card]]",
                "",
                "Start:",
                "    for each card in [active card]",
                "        trace [iterating card] it",
                "    trace slots [[slots around [hero] within 1] except [slot of [active card]]]");
            AssertRoundTrip(text);

            var file = ParseOk(text);
            file.TryGetProperty(CardProperties.AllowedMoves, out var property);
            Assert.IsInstanceOf<ConversionValue<Slot, SlotSet>>(property.Value);
            var loop = (ForEachCard)file.Sections[0].Commands[0];
            Assert.IsInstanceOf<ConversionValue<Entity, CardSet>>(loop.Cards);
        }

#endregion

#region Properties

        [Test]
        public void Properties_ParsedAndPrintedFirst()
        {
            var text = Lines(
                "AllowedMoves -> [slots around [active card] within 1]",
                "SpawnSlot -> [slots tagged Exit]",
                "",
                "OnPlaced:",
                "    reveal [active card]");
            AssertRoundTrip(text);

            var file = ParseOk(text);
            Assert.That(file.Properties.Select(property => property.Name), Is.EqualTo(new[] { "AllowedMoves", "SpawnSlot" }));
            Assert.IsTrue(file.TryGetProperty("SpawnSlot", out var spawn));
            Assert.AreEqual(2, spawn.Location.Line);
            Assert.IsInstanceOf<Evaluator<SlotSet>>(spawn.Value);
            Assert.IsFalse(file.TryGetProperty("Speed", out _));
        }

        [Test]
        public void Properties_OnlyProperties_PrintedWithoutSections()
        {
            AssertRoundTrip(Lines("SpawnSlot -> [slots tagged Exit]"));
        }

        [Test]
        public void Properties_AfterSection_ParsedAndPrintedFirst()
        {
            var file = ParseOk(Lines(
                "OnPlaced:",
                "    reveal [active card]",
                "SpawnSlot -> [free slots]"));

            Assert.AreEqual(Lines("SpawnSlot -> [free slots]", "", "OnPlaced:", "    reveal [active card]"), m_Printer.Print(file));
        }

        [TestCase("Speed -> [free slots]", "unknown property 'Speed': expected AllowedMoves, SpawnSlot")]
        [TestCase("AllowedMoves -> [active card]", "cannot parse SlotSet expression [active card]")]
        [TestCase("AllowedMoves -> 5", "cannot parse property 'AllowedMoves': unexpected '5', expected [SlotSet expression] or $variable")]
        [TestCase("AllowedMoves ->", "property 'AllowedMoves' has no value: expected [SlotSet expression] or $variable")]
        [TestCase("AllowedMoves -> [free slots] [free slots]", "cannot parse property 'AllowedMoves': unexpected '[', expected end of line")]
        [TestCase("SpawnSlot -> $charges", "'charges' is a card variable")]
        public void Properties_Error_WithLineNumber(string line, string message)
        {
            var errors = Errors(Lines("OnPlaced:", "    reveal [active card]", line));

            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            Assert.AreEqual(3, errors[0].Line);
            StringAssert.Contains(message, errors[0].Message);
        }

        [Test]
        public void Properties_Repeated_ErrorAndFirstKept()
        {
            var result = Parse(Lines("SpawnSlot -> [slots tagged Exit]", "SpawnSlot -> [free slots]"));

            Assert.That(result.Errors.Select(error => (error.Line, error.Message)), Is.EqualTo(new[] { (2, "property 'SpawnSlot' is already defined at line 1") }));
            Assert.AreEqual(1, result.File.Properties.Count);
            Assert.AreEqual(1, result.File.Properties[0].Location.Line);
        }

        [Test]
        public void Properties_InDialog_Error()
        {
            var errors = Errors(Lines("SpawnSlot -> [free slots]", "", "Start:", "    trace [hero] a"), ScriptFileKind.Dialog);

            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual(1, errors[0].Line);
            StringAssert.Contains("properties are allowed only in script files", errors[0].Message);
        }

        [Test]
        public void Properties_CommandAfterProperty_IsOutsideOfSection()
        {
            var errors = Errors(Lines("SpawnSlot -> [free slots]", "    reveal [active card]"));

            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual(2, errors[0].Line);
            StringAssert.Contains("outside of a section", errors[0].Message);
        }

        [Test]
        public void Registry_DeclaresCardProperties()
        {
            var registry = CardTestKit.Language;

            Assert.That(registry.Properties.Select(property => property.Name), Is.EqualTo(new[] { "AllowedMoves", "SpawnSlot" }));
            Assert.IsTrue(registry.TryGetProperty("AllowedMoves", out var moves));
            Assert.AreEqual(typeof(SlotSet), moves.Type);
            Assert.IsTrue(registry.TryGetConversion(typeof(Slot), typeof(SlotSet), out _));
            Assert.IsTrue(registry.TryGetConversion(typeof(Entity), typeof(CardSet), out _));
            Assert.IsFalse(registry.TryGetConversion(typeof(SlotSet), typeof(Slot), out _));
        }

        [Test]
        public void Registry_FromLoadedAssemblies_ContainsCardLanguage()
        {
            var registry = ScriptRegistry.FromLoadedAssemblies();

            Assert.IsTrue(registry.TryGetTemplates(typeof(MoveCard), out _));
            Assert.IsTrue(registry.TryGetProperty(CardProperties.SpawnSlot, out _));
            Assert.IsFalse(registry.TryGetTemplates(typeof(TraceCommand), out _), "test commands are not in the game language");
        }

#endregion

#region Parse errors

        [Test]
        public void Tag_UnknownTag_ParseError()
        {
            var errors = Errors(Lines("Start:", "    trace cards [cards tagged Dragon]", "    if [[hero] has tag Exit]", "        trace [hero] a"));

            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            Assert.AreEqual((2, "unknown Tag 'Dragon'"), errors[0]);
        }

        [Test]
        public void Create_NotCardModel_ParseError()
        {
            var errors = Errors(Lines("Start:", "    create Poison in [hand]"));

            Assert.AreEqual(1, errors.Length);
            StringAssert.Contains("'Poison' is a modifier, expected card", errors[0].Message);
        }

        [Test]
        public void ValueConditions_NotCardOrSlot_ParseError()
        {
            var errors = Errors(Lines(
                "Start:",
                "    if [5 has tag Hero]",
                "        trace [hero] a",
                "    if [[count of slots in [free slots]] exists]",
                "        trace [hero] b"));

            Assert.That(errors.Select(error => error.Line), Is.EqualTo(new[] { 2, 4 }), string.Join("\n", errors));
            StringAssert.Contains("expected card or slot, not Int32", errors[0].Message);
            StringAssert.Contains("expected card or slot, not Int32", errors[1].Message);
        }

        [Test]
        public void Equality_TypeOfRightSideFromLeft_ParseError()
        {
            var errors = Errors(Lines("Start:", "    if [[active card] == [slot of [hero]]]", "        trace [hero] a"));

            Assert.AreEqual(1, errors.Length);
            StringAssert.Contains("cannot parse Entity expression [slot of [hero]]", errors[0].Message);
        }

        [Test]
        public void CardCommands_NotInDialogs_CreateAllowed()
        {
            var errors = Errors(Lines(
                "Start:",
                "    move [active card] to [hand]",
                "    flip [active card]",
                "    create Herb in [inventory]",
                "    destroy [active card]"), ScriptFileKind.Dialog);

            Assert.That(errors.Select(error => error.Line), Is.EqualTo(new[] { 2, 3 }), string.Join("\n", errors));
            StringAssert.Contains("'move' is not allowed in dialog files", errors[0].Message);
        }

        [Test]
        public void CountOfSlotSet_IsCountOfSlotsIn()
        {
            var errors = Errors(Lines("Start:", "    if [[count of [free slots]] > 0]", "        trace [hero] a"));

            Assert.AreEqual(1, errors.Length);
            StringAssert.Contains("cannot parse CardSet expression [free slots]", errors[0].Message);
        }

#endregion

#region Content

        [TestCase("Entities/Hero.script")]
        [TestCase("Entities/Monster.script")]
        [TestCase("Entities/Herb.script")]
        [TestCase("Entities/Exit.script")]
        [TestCase("Scripts/Monsters.script")]
        public void ContentScripts_ParseWithoutErrors_InCanonicalFormat(string path)
        {
            AssertRoundTrip(CardTestKit.ReadContent(path));
        }

#endregion
    }
}
