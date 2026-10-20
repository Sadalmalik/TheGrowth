using System.Collections.Generic;
using NUnit.Framework;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Scripting
{
    public sealed class ScriptPrinterTests : ScriptLanguageTestBase
    {
#region Canonical format

        [Test]
        public void Print_CanonicalFormat()
        {
            var messy = "\n\nStart:  \n  if   [ flag  true ]  \n\n\n     log   \"a\"   \n  else\n     log \"b\"\n\n\n\nEnd:\n  goto   Start\n\n";

            var printed = Printer.Print(ParseOk(messy));

            Assert.AreEqual(Lines(
                "Start:",
                "    if [flag true]",
                "        log \"a\"",
                "    else",
                "        log \"b\"",
                "",
                "End:",
                "    goto Start"), printed);
        }

        [Test]
        public void Print_EmptyFileAndEmptySection()
        {
            Assert.AreEqual(string.Empty, Printer.Print(new ScriptFile("test", null)));
            Assert.AreEqual(Lines("A:", "", "B:", "    return"), Printer.Print(ParseOk(Lines("A:", "B:", "    return"))));
        }

        [Test]
        public void Print_TreeBuiltInCode()
        {
            var file = new ScriptFile("test", new[]
            {
                new ScriptSection("Start", new Command[]
                {
                    new Branch
                    {
                        Condition = new CompareCondition { A = new TestVariable { Name = "x" }, Operator = CompareOp.Greater, B = new Constant<int>(3) },
                        OnTrue = new List<Command> { new DebugLog { Message = "near" }, new Goto { Target = new SectionRef("other", "End") } },
                    },
                    new Comment { Text = " done" },
                    new SayCommand { Text = "Hello", LineId = "k3f9a2" },
                }),
            });

            Assert.AreEqual(Lines(
                "Start:",
                "    if [$x > 3]",
                "        log \"near\"",
                "        goto other.End",
                "    // done",
                "    say Hello #k3f9a2"), Printer.Print(file));
        }

        [Test]
        public void Print_UnsetParameters_AsMissingMarker()
        {
            var commands = new Command[] { new Branch(), new Goto(), new DebugLog(), new GiveCommand { Count = 1 } };

            Assert.AreEqual(Lines("if []", "goto []", "log \"\"", "give 1 []"), Printer.PrintCommands(commands));
        }

        [Test]
        public void Print_ChoosesMostDetailedTemplateWithAllParameters()
        {
            var lucia = Content.Get("Lucia");
            var plain = new LineCommand { Character = lucia, Text = "Hi" };
            var happy = new LineCommand { Character = lucia, Emotion = Content.Get("happy"), Text = "Hi" };

            Assert.AreEqual("Lucia: Hi\n", Printer.PrintCommand(plain));
            Assert.AreEqual("Lucia.happy: Hi\n", Printer.PrintCommand(happy));
        }

        [Test]
        public void Print_OptionalContinuation_OmittedWhenEmpty()
        {
            var attempt = new AttemptCommand { Cleanup = new List<Command> { new Return() } };

            Assert.AreEqual(Lines("attempt", "finally", "    return"), Printer.PrintCommand(attempt));
        }

        [Test]
        public void Print_Literals()
        {
            Assert.AreEqual("mark \"a \\\"b\\\" \\\\ c\\nd\"\n", Printer.PrintCommand(new ScriptMark { Name = "a \"b\" \\ c\nd" }));
            Assert.AreEqual("wait 0.25 seconds\n", Printer.PrintCommand(new WaitSecondsCommand { Seconds = 0.25f }));
            Assert.AreEqual("wait 0.00001 seconds\n", Printer.PrintCommand(new WaitSecondsCommand { Seconds = 0.00001f }));
            Assert.AreEqual("give -3 apple\n", Printer.PrintCommand(new GiveCommand { Count = -3, Card = Content.Get("apple") }));
            Assert.AreEqual("speed Slow\n", Printer.PrintCommand(new SpeedCommand { Speed = TestSpeed.Slow }));
            Assert.AreEqual("check -7\n", Printer.PrintCommand(new CheckCommand { Value = new Constant<int>(-7) }));
            Assert.AreEqual("[sum of 1, 2]", Printer.PrintNode(new SumEvaluator { Values = new List<int> { 1, 2 } }));
            Assert.AreEqual("$coins", Printer.PrintNode(new TestVariable { Name = "coins" }));
        }

#endregion

#region Round trip

        [TestCase("S:\n    if [flag true]\n        log \"yes\"\n    else\n        log \"no\"\n")]
        [TestCase("S:\n    if [flag true]\n        log \"yes\"\n")]
        [TestCase("S:\n    if [flag true]\n    else\n        log \"no\"\n")]
        [TestCase("S:\n    goto Start\n")]
        [TestCase("S:\n    goto other.Start\n")]
        [TestCase("S:\n    call Sub\n")]
        [TestCase("S:\n    call other.Sub\n")]
        [TestCase("S:\n    return\n")]
        [TestCase("S:\n    parallel\n        log \"a\"\n        log \"b\"\n")]
        [TestCase("S:\n    background\n        log \"a\"\n")]
        [TestCase("S:\n    log \"text with \\\"quotes\\\"\"\n")]
        [TestCase("S:\n    log \"\"\n")]
        public void RoundTrip_BuiltInCommands(string text)
        {
            AssertRoundTrip(text);
        }

        [Test]
        public void RoundTrip_BuiltInCommandTrees()
        {
            var commands = new Command[]
            {
                new Branch { Condition = new FlagCondition { Value = true }, OnTrue = new List<Command> { new Return() } },
                new Branch { Condition = new FlagCondition(), OnTrue = new List<Command>(), OnFalse = new List<Command> { new Return() } },
                new Goto { Target = SectionRef.Parse("Start") },
                new Call { Target = SectionRef.Parse("other.Sub") },
                new Return(),
                new Parallel { Commands = new List<Command> { new DebugLog { Message = "a" }, new DebugLog { Message = "b" } } },
                new Background { Commands = new List<Command> { new DebugLog { Message = "c" } } },
                new DebugLog { Message = "multi\nline" },
            };

            foreach (var command in commands)
            {
                var text = Printer.Print(new ScriptFile("test", new[] { new ScriptSection("S", new[] { command }) }));
                AssertRoundTrip(text);
            }
        }

        [Test]
        public void RoundTrip_MixedScript()
        {
            AssertRoundTrip(Lines(
                "OnPlaced:",
                "    // Reaction to placing",
                "    if [any of [[$x + 1] > 3], [not [flag false]]]",
                "        give 2 apple",
                "        move apple to -1",
                "        wait 0.5 seconds",
                "    else",
                "        if [$x == [sum of 1, 2, 3]]",
                "            say Wha... HOW? #k3f9a2",
                "        else",
                "            check [[$coins + -2] + 7]",
                "    repeat",
                "        parallel",
                "            log \"a\"",
                "            //",
                "            speed Fast",
                "        background",
                "            call Tail",
                "    until [$x >= 10]",
                "    attempt",
                "        log \"try\"",
                "    otherwise",
                "        goto other.Start",
                "    finally",
                "        return",
                "",
                "Tail:",
                "    check $trust",
                "",
                "Empty:"));
        }

        [Test]
        public void RoundTrip_MixedDialog()
        {
            AssertRoundTrip(Lines(
                "Start:",
                "    Lucia.happy: You came! #a1b2c3",
                "    Lucia: Hi... \"quoted\" [brackets], 100% #d4e5f6",
                "    if [$trust > 3]",
                "        call AboutAmulet",
                "    goto Start",
                "",
                "AboutAmulet:",
                "    Lucia: It was my mother's."), ScriptFileKind.Dialog);
        }

#endregion
    }
}
