using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Scripting
{
    public sealed class ScriptParserTests : ScriptLanguageTestBase
    {
        private static void AssertError(ScriptError[] errors, int line, string fragment)
        {
            Assert.IsTrue(errors.Any(e => e.Line == line && e.Message.Contains(fragment)),
                $"No error at line {line} containing '{fragment}':\n{string.Join("\n", errors)}");
        }

#region Sections

        [Test]
        public void Sections_InOrder_BlankLinesIgnored()
        {
            var file = ParseOk(Lines("", "Start:", "    mark \"a\"", "", "", "    mark \"b\"", "End:", "", "Empty:"));

            Assert.AreEqual("test", file.Id);
            Assert.That(file.Sections.Select(s => s.Name), Is.EqualTo(new[] { "Start", "End", "Empty" }));
            Assert.AreEqual(2, file.Sections[0].Commands.Count);
            Assert.AreEqual(0, file.Sections[1].Commands.Count);
        }

        [Test]
        public void Sections_CrLfAndBom()
        {
            var file = ParseOk("\uFEFFStart:\r\n    mark \"a\"\r\n");

            Assert.AreEqual(1, file.Sections[0].Commands.Count);
        }

        [Test]
        public void Sections_Duplicate_IsError()
        {
            var errors = ParseErrors(Lines("A:", "    mark \"a\"", "A:", "    mark \"b\""));

            AssertError(errors, 3, "section 'A' is already defined at line 1");
        }

        [Test]
        public void ZeroIndentation_TextIsError()
        {
            var errors = ParseErrors(Lines("mark \"a\"", "S:", "mark \"b\"", "Lucia: hello", "// comment", "Bad name:"));

            AssertError(errors, 1, "text at zero indentation");
            AssertError(errors, 3, "text at zero indentation");
            AssertError(errors, 4, "section header must have nothing after ':'");
            AssertError(errors, 5, "comment at zero indentation");
            AssertError(errors, 6, "invalid section name");
        }

        [Test]
        public void CommandOutsideSection_IsReportedOnce()
        {
            var errors = ParseErrors(Lines("    mark \"a\"", "    mark \"b\"", "S:", "    mark \"c\""));

            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            AssertError(errors, 1, "command outside of a section");
        }

#endregion

#region Indentation

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(3)]
        public void Indentation_AnyConsistentWidth(int width)
        {
            var i1 = new string(' ', width);
            var i2 = new string(' ', width * 2);
            var file = ParseOk(Lines("S:", i1 + "if [flag true]", i2 + "mark \"a\"", i2 + "mark \"b\"", i1 + "mark \"c\""));

            var branch = (Branch)file.Sections[0].Commands[0];
            Assert.AreEqual(2, branch.OnTrue.Count);
            Assert.AreEqual(2, file.Sections[0].Commands.Count);
        }

        [Test]
        public void Indentation_MixedWidthsPerBlock()
        {
            // Блок глубже родителя и одинаков внутри себя; ширина у разных блоков может отличаться.
            var file = ParseOk(Lines(
                "S:",
                "  parallel",
                "        mark \"a\"",
                "        if [flag true]",
                "         mark \"b\"",
                "  mark \"c\""));

            var parallel = (Parallel)file.Sections[0].Commands[0];
            Assert.AreEqual(2, parallel.Commands.Count);
            Assert.AreEqual(1, ((Branch)parallel.Commands[1]).OnTrue.Count);
        }

        [Test]
        public void Indentation_DedentToUnknownLevel_IsError()
        {
            var errors = ParseErrors(Lines("S:", "    if [flag true]", "        mark \"a\"", "      mark \"b\"", "    mark \"c\""));

            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            AssertError(errors, 4, "indentation does not match any outer block");
        }

        [Test]
        public void Indentation_LessThanSectionBody_IsError()
        {
            var errors = ParseErrors(Lines("S:", "        mark \"a\"", "    mark \"b\""));

            AssertError(errors, 3, "indentation is less than");
        }

        [Test]
        public void Indentation_Tab_IsError()
        {
            var errors = ParseErrors("S:\n\tmark \"a\"\n    \tmark \"b\"\n    mark \"c\"\n");

            Assert.AreEqual(2, errors.Length, string.Join("\n", errors));
            Assert.AreEqual(2, errors[0].Line);
            Assert.AreEqual(1, errors[0].Column);
            Assert.AreEqual(3, errors[1].Line);
            Assert.AreEqual(5, errors[1].Column);
            StringAssert.Contains("tab", errors[0].Message);
        }

        [Test]
        public void Indentation_UnderCommandWithoutBlock_IsError()
        {
            var errors = ParseErrors(Lines("S:", "    mark \"a\"", "        mark \"b\""));

            AssertError(errors, 3, "unexpected indentation");
        }

#endregion

#region Blocks and continuations

        [Test]
        public void Blocks_ParallelAndBackground()
        {
            var file = ParseOk(Lines("S:", "    parallel", "        mark \"a\"", "        mark \"b\"", "    background", "        mark \"c\""));

            Assert.AreEqual(2, ((Parallel)file.Sections[0].Commands[0]).Commands.Count);
            Assert.AreEqual(1, ((Background)file.Sections[0].Commands[1]).Commands.Count);
        }

        [Test]
        public void If_WithElse()
        {
            var file = ParseOk(Lines("S:", "    if [flag true]", "        mark \"a\"", "    else", "        mark \"b\"", "        mark \"c\"", "    mark \"d\""));

            var commands = file.Sections[0].Commands;
            var branch = (Branch)commands[0];
            Assert.AreEqual(2, commands.Count);
            Assert.IsTrue(((FlagCondition)branch.Condition).Value);
            Assert.AreEqual("a", ((ScriptMark)branch.OnTrue[0]).Name);
            Assert.AreEqual(2, branch.OnFalse.Count);
        }

        [Test]
        public void If_WithoutElse_HasEmptyElseBlock()
        {
            var branch = (Branch)ParseOk(Lines("S:", "    if [flag false]", "        mark \"a\"", "    mark \"b\"")).Sections[0].Commands[0];

            Assert.AreEqual(1, branch.OnTrue.Count);
            Assert.IsNotNull(branch.OnFalse);
            Assert.AreEqual(0, branch.OnFalse.Count);
        }

        [Test]
        public void ElseIf_IsNestedIf()
        {
            var branch = (Branch)ParseOk(Lines(
                "S:",
                "    if [flag false]",
                "        mark \"a\"",
                "    else",
                "        if [flag true]",
                "            mark \"b\"",
                "        else",
                "            mark \"c\"")).Sections[0].Commands[0];

            var inner = (Branch)branch.OnFalse[0];
            Assert.AreEqual(1, inner.OnTrue.Count);
            Assert.AreEqual(1, inner.OnFalse.Count);
        }

        [Test]
        public void Else_WithoutIf_IsError()
        {
            var errors = ParseErrors(Lines("S:", "    mark \"a\"", "    else", "        mark \"b\""));

            AssertError(errors, 3, "'else' continues 'if' and cannot start a command");
        }

        [Test]
        public void Continuation_WithInvalidText_IsError()
        {
            var errors = ParseErrors(Lines("S:", "    if [flag true]", "        mark \"a\"", "    else now", "        mark \"b\""));

            AssertError(errors, 4, "cannot parse 'if': unexpected 'now'");
        }

        [Test]
        public void TrailingLine_RepeatUntil()
        {
            var file = ParseOk(Lines("S:", "    repeat", "        mark \"a\"", "    until [flag true]", "    mark \"b\""));

            var repeat = (RepeatUntilCommand)file.Sections[0].Commands[0];
            Assert.AreEqual(2, file.Sections[0].Commands.Count);
            Assert.AreEqual(1, repeat.Body.Count);
            Assert.IsInstanceOf<FlagCondition>(repeat.Condition);
        }

        [Test]
        public void TrailingLine_Missing_IsError()
        {
            var errors = ParseErrors(Lines("S:", "    repeat", "        mark \"a\"", "    mark \"b\"", "T:", "    repeat", "        mark \"c\""));

            AssertError(errors, 4, "expected 'until [Condition]' (continuation of 'repeat' at line 2)");
            AssertError(errors, 6, "'repeat': missing line 'until [Condition]' after the block");
        }

        [Test]
        public void OptionalContinuation_SkippedInTheMiddle()
        {
            var attempt = (AttemptCommand)ParseOk(Lines("S:", "    attempt", "        mark \"a\"", "    finally", "        mark \"c\"")).Sections[0].Commands[0];

            Assert.AreEqual(1, attempt.Body.Count);
            Assert.AreEqual(0, attempt.Fallback.Count);
            Assert.AreEqual(1, attempt.Cleanup.Count);
        }

        [Test]
        public void Block_Empty()
        {
            var branch = (Branch)ParseOk(Lines("S:", "    if [flag true]", "    else", "        mark \"b\"")).Sections[0].Commands[0];

            Assert.AreEqual(0, branch.OnTrue.Count);
            Assert.AreEqual(1, branch.OnFalse.Count);
        }

#endregion

#region Parameters

        [Test]
        public void Literals_StringWithEscapes()
        {
            Assert.AreEqual("a \"b\" \\ c\nd", ParseSingle<ScriptMark>("mark \"a \\\"b\\\" \\\\ c\\nd\"").Name);
        }

        [Test]
        public void Literals_Numbers()
        {
            Assert.AreEqual(0.5f, ParseSingle<WaitSecondsCommand>("wait 0.5 seconds").Seconds);
            Assert.AreEqual(-2f, ParseSingle<WaitSecondsCommand>("wait -2 seconds").Seconds);
            Assert.AreEqual(-3, ParseSingle<GiveCommand>("give -3 apple").Count);
        }

        [Test]
        public void Literals_IntegerExpected_IsError()
        {
            var errors = ParseErrors(Lines("S:", "    give 2.5 apple"));

            AssertError(errors, 2, "'2.5' is not a valid integer");
        }

        [Test]
        public void Literals_ConfigId()
        {
            var give = ParseSingle<GiveCommand>("give 2 apple");

            Assert.AreEqual(2, give.Count);
            Assert.AreSame(Content.Get("apple"), give.Card);
        }

        [Test]
        public void Literals_UnknownConfigId_IsError()
        {
            var errors = ParseErrors(Lines("S:", "    give 2 pear"));

            AssertError(errors, 2, "unknown TestConfig 'pear'");
            Assert.AreEqual(12, errors[0].Column);
        }

        [Test]
        public void Literals_SectionRefs()
        {
            Assert.AreEqual("Start", ParseSingle<Goto>("goto Start").Target.ToString());
            Assert.AreEqual("other.Start", ParseSingle<Goto>("goto other.Start").Target.ToString());
            Assert.AreEqual("other", ParseSingle<Call>("call other.Sub").Target.File);
        }

        [Test]
        public void Literals_SectionRefWithSpaces_IsError()
        {
            ParseErrors(Lines("S:", "    goto other . Start"));
        }

        [Test]
        public void Literals_EnumByName()
        {
            Assert.AreEqual(TestSpeed.Slow, ParseSingle<SpeedCommand>("speed Slow").Speed);
        }

        [TestCase(">", CompareOp.Greater)]
        [TestCase(">=", CompareOp.GreaterOrEqual)]
        [TestCase("==", CompareOp.Equal)]
        [TestCase("Less", CompareOp.Less)]
        public void Literals_EnumByToken(string op, CompareOp expected)
        {
            var branch = ParseSingle<Branch>($"if [$x {op} 3]");

            Assert.AreEqual(expected, ((CompareCondition)branch.Condition).Operator);
        }

        [Test]
        public void Constants_WrapBareLiterals()
        {
            var check = ParseSingle<CheckCommand>("check 5");

            Assert.IsInstanceOf<Constant<int>>(check.Value);
            Assert.AreEqual(5, ((Constant<int>)check.Value).Value);
        }

        [Test]
        public void Variables_ResolvedByResolver()
        {
            var check = ParseSingle<CheckCommand>("check $coins");

            Assert.AreEqual("coins", ((TestVariable)check.Value).Name);
        }

        [Test]
        public void Variables_UnknownOrWrongType_IsError()
        {
            var errors = ParseErrors(Lines("S:", "    check $nope", "    if $x"));

            AssertError(errors, 2, "unknown variable '$nope'");
            AssertError(errors, 3, "variable '$x' is int, expected Condition");
        }

        [Test]
        public void Brackets_Nesting()
        {
            var branch = ParseSingle<Branch>("if [[[$x + 1] + -2] >= [sum of 1, 2, 3]]");

            var compare = (CompareCondition)branch.Condition;
            var outer = (AddEvaluator)compare.A;
            var inner = (AddEvaluator)outer.A;
            Assert.AreEqual("x", ((TestVariable)inner.A).Name);
            Assert.AreEqual(1, ((Constant<int>)inner.B).Value);
            Assert.AreEqual(-2, ((Constant<int>)outer.B).Value);
            Assert.That(((SumEvaluator)compare.B).Values, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Lists_OfNodes()
        {
            var branch = ParseSingle<Branch>("if [any of [flag false], [not [flag false]], [$x > 1]]");

            var any = (AnyCondition)branch.Condition;
            Assert.AreEqual(3, any.Conditions.Count);
            Assert.IsInstanceOf<NotCondition>(any.Conditions[1]);
            Assert.IsInstanceOf<CompareCondition>(any.Conditions[2]);
        }

        [Test]
        public void Brackets_Errors()
        {
            var errors = ParseErrors(Lines("S:", "    if []", "    if [flag true", "    if [flag true]]", "    if [flag maybe]", "    log [\"text\"]"));

            AssertError(errors, 2, "empty brackets");
            AssertError(errors, 3, "unclosed '['");
            AssertError(errors, 4, "unmatched ']'");
            AssertError(errors, 5, "cannot parse condition [flag maybe]: unexpected 'maybe', expected true or false");
            AssertError(errors, 6, "cannot parse 'log': unexpected '[', expected string in quotes");
        }

        [Test]
        public void RestOfLine_TakesRawText()
        {
            var say = ParseSingle<SayCommand>("say Wha... \"HOW? [unbalanced, #x");

            Assert.AreEqual("Wha... \"HOW? [unbalanced, #x", say.Text);
        }

        [Test]
        public void RestOfLine_StopsAtLineId()
        {
            var say = ParseSingle<SayCommand>("say Hello there   #k3f9a2");

            Assert.AreEqual("Hello there", say.Text);
            Assert.AreEqual("k3f9a2", say.LineId);
        }

        [Test]
        public void LineId_OnContinuationLine_IsError()
        {
            var errors = ParseErrors(Lines("S:", "    if [flag true] #abcdef", "        mark \"a\"", "    else #ghijkl", "        mark \"b\""));

            Assert.AreEqual(1, errors.Length, string.Join("\n", errors));
            AssertError(errors, 4, "line id is allowed only on the first line");
            Assert.AreEqual(10, errors[0].Column);
        }

        [Test]
        public void MultipleTemplates_PerClass()
        {
            var file = ParseOk(Lines("Start:", "    Lucia: Hi, there!", "    Lucia.happy: You came!"), ScriptFileKind.Dialog);

            var plain = (LineCommand)file.Sections[0].Commands[0];
            var happy = (LineCommand)file.Sections[0].Commands[1];
            Assert.AreSame(Content.Get("Lucia"), plain.Character);
            Assert.IsNull(plain.Emotion);
            Assert.AreEqual("Hi, there!", plain.Text);
            Assert.AreSame(Content.Get("happy"), happy.Emotion);
            Assert.AreEqual("You came!", happy.Text);
        }

#endregion

#region Comments, locations, errors

        [Test]
        public void Comments_AreCommands()
        {
            var file = ParseOk(Lines("S:", "    // first", "    if [flag true]", "        //nested", "        mark \"a\""));

            var commands = file.Sections[0].Commands;
            Assert.AreEqual(" first", ((Comment)commands[0]).Text);
            Assert.AreEqual("nested", ((Comment)((Branch)commands[1]).OnTrue[0]).Text);
        }

        [Test]
        public void Locations_OnEveryCommand()
        {
            var file = ParseOk(Lines("S:", "    mark \"a\"", "", "    if [flag true]", "        mark \"b\"", "    else", "        mark \"c\""), fileId: "cards/lucia");

            var commands = file.Sections[0].Commands;
            var branch = (Branch)commands[1];
            Assert.AreEqual("cards/lucia:2", commands[0].Location.ToString());
            Assert.AreEqual(4, branch.Location.Line);
            Assert.AreEqual(5, branch.OnTrue[0].Location.Line);
            Assert.AreEqual(7, branch.OnFalse[0].Location.Line);
        }

        [Test]
        public void Errors_UnknownCommandAndUnterminatedString()
        {
            var errors = ParseErrors(Lines("S:", "    jump Start", "    log \"abc"));

            AssertError(errors, 2, "unknown command 'jump'");
            AssertError(errors, 3, "unterminated string");
            Assert.AreEqual(5, errors[0].Column);
            Assert.AreEqual(9, errors[1].Column);
        }

        [Test]
        public void Errors_FailedCommand_SkipsItsContinuations()
        {
            var errors = ParseErrors(Lines("S:", "    if [flag true", "        mark \"a\"", "    else", "        log 5", "    mark \"b\""));

            Assert.AreEqual(2, errors.Length, string.Join("\n", errors));
            AssertError(errors, 2, "unclosed '['");
            AssertError(errors, 5, "cannot parse 'log'");
        }

        [Test]
        public void Errors_CollectedFromWholeFile_InLineOrder()
        {
            var result = Parse(Lines(
                "Start:",
                "    mark \"a\"",
                "    jump Start",
                "    if [flag true]",
                "        give 1 pear",
                "        mark \"b\"",
                "    else",
                "        log 5",
                "End:",
                "\tmark \"c\"",
                "    goto",
                "    mark \"d\""));

            Assert.That(result.Errors.Select(e => e.Line), Is.EqualTo(new[] { 3, 5, 8, 10, 11 }), result.ToString());
            Assert.That(result.Errors.Select(e => e.FileId).Distinct(), Is.EqualTo(new[] { "test" }));

            // Остальное разобрано; команда с ошибкой в значении остаётся в дереве без этого значения.
            var start = result.File.Sections[0].Commands;
            Assert.AreEqual(2, start.Count);
            Assert.AreEqual(2, ((Branch)start[1]).OnTrue.Count);
            Assert.IsNull(((GiveCommand)((Branch)start[1]).OnTrue[0]).Card);
            Assert.AreEqual(1, result.File.Sections[1].Commands.Count);
        }

#endregion

#region File kinds

        [Test]
        public void Context_DialogOnlyCommandInScript_IsError()
        {
            var errors = ParseErrors(Lines("S:", "    Lucia: hi"), ScriptFileKind.Script);

            AssertError(errors, 2, "'line' is allowed only in dialog files (.dialog)");
        }

        [Test]
        public void Context_GameplayOnlyCommandInDialog_IsError()
        {
            var errors = ParseErrors(Lines("S:", "    move apple to 3"), ScriptFileKind.Dialog);

            AssertError(errors, 2, "'move' is not allowed in dialog files (.dialog)");
        }

        [Test]
        public void Context_AllowedCommands_Parse()
        {
            ParseOk(Lines("S:", "    move apple to 3", "    mark \"a\""), ScriptFileKind.Script);
            ParseOk(Lines("S:", "    Lucia: hi", "    mark \"a\""), ScriptFileKind.Dialog);
        }

        [Test]
        public void ParseFile_KindAndIdFromPath()
        {
            var result = Parser.ParseFile("Assets/Content/Dialogs/Intro.dialog", Lines("Start:", "    Lucia: hi"));

            Assert.IsFalse(result.HasErrors, result.ToString());
            Assert.AreEqual(ScriptFileKind.Dialog, result.Kind);
            Assert.AreEqual("Intro", result.File.Id);
        }

#endregion
    }
}
