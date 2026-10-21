using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Variables;
using TheGrowth.EditorTools.Scripting;
using TheGrowth.Tests.EditMode.Content;
using TheGrowth.Tests.EditMode.Scripting;

namespace TheGrowth.Tests.EditMode.Variables
{
    /// <summary>
    /// Переменные в языке: <c>$имя</c> и <c>[узел].имя</c> в узлах разных типов, ошибки разбора (не объявлена, прежнее
    /// имя, область, тип), команды <c>set</c> / <c>add</c>, сравнения, логика, арифметика; печать → разбор → печать.
    /// </summary>
    public sealed class VariableScriptTests
    {
        private ContentTestKit m_Kit;
        private VariableCatalog m_Catalog;
        private ScriptParser m_Parser;
        private ScriptPrinter m_Printer;

        [SetUp]
        public void SetUp()
        {
            m_Kit = new ContentTestKit();
            m_Catalog = VariableTestKit.Catalog(m_Kit, VariableTestKit.Standard());
            var resolver = new VariableScriptResolver(m_Catalog);
            m_Parser = new ScriptParser(VariableTestKit.Language, null, resolver);
            m_Printer = new ScriptPrinter(VariableTestKit.Language, null, resolver);
        }

        [TearDown]
        public void TearDown()
        {
            m_Kit.Dispose();
        }

#region Helpers

        private ScriptParseResult Parse(string text)
        {
            return m_Parser.Parse("test", ScriptFileKind.Script, text);
        }

        private T ParseSingle<T>(string line) where T : Command
        {
            var result = Parse("S:\n    " + line + "\n");
            Assert.IsFalse(result.HasErrors, "Unexpected errors:\n" + result);
            Assert.AreEqual(1, result.File.Sections[0].Commands.Count);
            return (T)result.File.Sections[0].Commands[0];
        }

        private T ParseCondition<T>(string condition) where T : Condition
        {
            var branch = ParseSingle<Branch>("if " + condition);
            Assert.IsInstanceOf<T>(branch.Condition);
            return (T)branch.Condition;
        }

        /// <summary>Единственная ошибка строки 2 (первая строка секции), содержащая фрагмент.</summary>
        private void AssertLineError(string line, string fragment)
        {
            var result = Parse("S:\n    " + line + "\n");
            Assert.IsTrue(result.HasErrors, $"Expected errors in '{line}'");
            Assert.IsTrue(result.Errors.Any(error => error.Line == 2 && error.Message.Contains(fragment)),
                $"No error containing '{fragment}' in '{line}':\n{result}");
            Assert.AreEqual(1, result.Errors.Count, result.ToString());
        }

        private void AssertRoundTrip(string text)
        {
            var result = Parse(text);
            Assert.IsFalse(result.HasErrors, "Unexpected errors:\n" + result);
            var printed = m_Printer.Print(result.File);
            Assert.AreEqual(text, printed, "print(parse(text)) differs");

            var again = Parse(printed);
            Assert.IsFalse(again.HasErrors, again.ToString());
            Assert.AreEqual(printed, m_Printer.Print(again.File), "print(parse(print)) differs");
        }

        private static string Lines(params string[] lines)
        {
            return string.Join("\n", lines) + "\n";
        }

#endregion

#region Reading

        [Test]
        public void Read_InEvaluatorsOfEachType_AndAsCondition()
        {
            var check = ParseSingle<CheckCommand>("check $coins");
            var coins = (VariableValue<int>)check.Value;
            Assert.AreSame(m_Catalog.Get("coins"), coins.Declaration);
            Assert.IsNull(coins.Owner);

            var met = ParseCondition<VariableCondition>("$met_lucia");
            Assert.AreSame(m_Catalog.Get("met_lucia"), met.Declaration);

            var name = ParseCondition<Equal>("[$player_name == \"Ann\"]");
            Assert.IsInstanceOf<VariableValue<string>>(name.A);
            Assert.AreEqual("Ann", ((Constant<string>)name.B).Value);

            var flag = ParseCondition<NotEqual>("[$met_lucia != true]");
            Assert.IsInstanceOf<VariableValue<bool>>(flag.A);
            Assert.AreEqual(true, ((Constant<bool>)flag.B).Value);

            var profile = ParseCondition<Not>("[not $ending_seen]");
            Assert.AreEqual(VariableScope.Profile, ((VariableCondition)profile.Condition).Declaration.Scope);
        }

        [Test]
        public void Read_OwnerVariable()
        {
            var check = ParseSingle<CheckCommand>("check [active owner].charges");

            var charges = (VariableValue<int>)check.Value;
            Assert.AreSame(m_Catalog.Get("charges"), charges.Declaration);
            Assert.IsInstanceOf<ActiveOwnerEvaluator>(charges.Owner);

            var compare = ParseCondition<Compare>("[[active owner].charges > 1]");
            Assert.IsInstanceOf<VariableValue<int>>(compare.A);
        }

        [Test]
        public void Unknown_Error()
        {
            AssertLineError("check $nope", "unknown variable '$nope'");
            AssertLineError("check [active owner].nope", "unknown variable '[...].nope'");
        }

        [Test]
        public void WrongType_Error()
        {
            AssertLineError("check $player_name", "variable '$player_name' is string, expected int");
            AssertLineError("if $coins", "variable '$coins' is int, expected condition");
            AssertLineError("if [$player_name > 1]", "variable '$player_name' is string, expected int");
            AssertLineError("if [$coins == \"a\"]", "'\"a\"' is not a valid integer");
            AssertLineError("if [$coins == $met_lucia]", "variable '$met_lucia' is bool, expected int");
        }

        [Test]
        public void PreviousName_ErrorWithNewName()
        {
            AssertLineError("check $money", "variable '$money' is renamed to '$gold'");
            AssertLineError("add 1 to $money", "variable '$money' is renamed to '$gold'");
        }

        [Test]
        public void Scope_CardNeedsOwner_GameAndProfileNeedDollar()
        {
            AssertLineError("check $charges", "'charges' is a card variable: write [card].charges");
            AssertLineError("set $charges to 1", "'charges' is a card variable");
            AssertLineError("check [active owner].coins", "'coins' is a game variable: write $coins");
            AssertLineError("set [active owner].ending_seen to true", "'ending_seen' is a profile variable");
        }

        [Test]
        public void Owner_MustBeAnOwnerExpression()
        {
            AssertLineError("check [some text].charges", "cannot parse IVariableOwner expression [some text]");
            AssertLineError("set [nothing].charges to 1", "cannot parse IVariableOwner expression [nothing]");
        }

#endregion

#region Writing

        [Test]
        public void Set_AnyType_ValueTypeFromVariable()
        {
            var coins = ParseSingle<SetVariable>("set $coins to 5");
            Assert.IsInstanceOf<VariableReference<int>>(coins.Variable);
            Assert.AreEqual(5, ((Constant<int>)coins.Value).Value);

            var met = ParseSingle<SetVariable>("set $met_lucia to true");
            Assert.IsInstanceOf<VariableReference<bool>>(met.Variable);
            Assert.AreEqual(true, ((Constant<bool>)met.Value).Value);

            var name = ParseSingle<SetVariable>("set $player_name to \"Ann\"");
            Assert.AreEqual("Ann", ((Constant<string>)name.Value).Value);

            var copy = ParseSingle<SetVariable>("set $gold to [$coins + -1]");
            Assert.IsInstanceOf<Plus>(copy.Value);

            var owned = ParseSingle<SetVariable>("set [active owner].charges to $coins");
            Assert.IsInstanceOf<ActiveOwnerEvaluator>(owned.Variable.Owner);
            Assert.IsInstanceOf<VariableValue<int>>(owned.Value);
        }

        [Test]
        public void Set_WrongValueType_Error()
        {
            AssertLineError("set $coins to \"a\"", "'\"a\"' is not a valid integer");
            AssertLineError("set $met_lucia to 5", "'5' is not a valid true or false");
            AssertLineError("set $player_name to true", "'true' is not a valid string in quotes");
            AssertLineError("set $coins to $player_name", "variable '$player_name' is string, expected int");
            AssertLineError("set $coins to [$coins > 1]", "cannot parse Int32 expression");
        }

        [Test]
        public void Set_UndeclaredVariable_Error()
        {
            AssertLineError("set $nope to 1", "unknown variable '$nope'");
            AssertLineError("set [active owner].nope to 1", "unknown variable '[...].nope'");
        }

        [Test]
        public void Add_IntVariableOnly()
        {
            var add = ParseSingle<AddToVariable>("add -1 to [active owner].charges");
            Assert.AreEqual(-1, ((Constant<int>)add.Amount).Value);
            Assert.AreSame(m_Catalog.Get("charges"), add.Variable.Declaration);

            AssertLineError("add 1 to $player_name", "variable '$player_name' is string, expected int variable");
            AssertLineError("add \"a\" to $coins", "cannot parse 'add'");
        }

        [Test]
        public void Variable_WithoutDollarOrOwner_IsNotAVariable()
        {
            AssertLineError("set coins to 1", "cannot parse 'set'");
            AssertLineError("set [active owner] to 1", "cannot parse 'set'");
        }

#endregion

#region Round trip

        [TestCase("set $coins to 5")]
        [TestCase("set $coins to -5")]
        [TestCase("set $met_lucia to false")]
        [TestCase("set $player_name to \"Ann \\\"the\\\" first\"")]
        [TestCase("set $ending_seen to $met_lucia")]
        [TestCase("set $gold to [[$coins + 10] - $trust_lucia]")]
        [TestCase("set [active owner].charges to [[active owner].charges - 1]")]
        [TestCase("add 5 to $coins")]
        [TestCase("add -1 to [active owner].charges")]
        [TestCase("add [$coins + 1] to $gold")]
        [TestCase("if $met_lucia")]
        [TestCase("if [not $met_lucia]")]
        [TestCase("if [[active owner].charges > 1]")]
        [TestCase("if [$coins < 3]")]
        [TestCase("if [$coins >= 3]")]
        [TestCase("if [$coins <= 3]")]
        [TestCase("if [$coins == 3]")]
        [TestCase("if [3 != $coins]")]
        [TestCase("if [$met_lucia == true]")]
        [TestCase("if [$player_name != \"Player\"]")]
        [TestCase("if [[$coins > 1] and [not $ending_seen]]")]
        [TestCase("if [[$coins > 1] or [[$trust_lucia == 2] and $met_lucia]]")]
        [TestCase("set $met_lucia to [$coins > 3]")]
        [TestCase("set $ending_seen to [[$coins > 1] and [not $met_lucia]]")]
        [TestCase("set [active owner].charges to [$coins + 1]")]
        [TestCase("if [[$coins > 3] == $met_lucia]")]
        public void RoundTrip_NewCommandsAndNodes(string line)
        {
            AssertRoundTrip(line.StartsWith("if ") ? Lines("S:", "    " + line, "        log \"ok\"") : Lines("S:", "    " + line));
        }

        [Test]
        public void RoundTrip_SpecExample()
        {
            AssertRoundTrip(Lines(
                "OnPlaced:",
                "    if [$trust_lucia > 3]",
                "        add 5 to $coins",
                "        set $met_lucia to true",
                "        log \"We are near target\"",
                "    else",
                "        set [active owner].charges to 0"));
        }

        [Test]
        public void Print_TreeBuiltInCode()
        {
            var coins = m_Catalog.Get("coins");
            var commands = new Command[]
            {
                new SetVariable { Variable = new VariableReference<int>(coins), Value = new Constant<int>(3) },
                new AddToVariable { Amount = new Minus { A = new VariableValue<int>(m_Catalog.Get("gold")), B = new Constant<int>(1) }, Variable = new VariableReference<int>(coins) },
                new Branch { Condition = new Or { A = new VariableCondition(m_Catalog.Get("met_lucia")), B = new Not { Condition = new VariableCondition(m_Catalog.Get("ending_seen")) } } },
                new SetVariable { Variable = VariableReference.Create(m_Catalog.Get("charges"), new ActiveOwnerEvaluator()), Value = new Constant<int>(1) },
            };

            Assert.AreEqual(Lines(
                "set $coins to 3",
                "add [$gold - 1] to $coins",
                "if [$met_lucia or [not $ending_seen]]",
                "set [active owner].charges to 1"), m_Printer.PrintCommands(commands));
        }

#endregion

#region Importer

        [Test]
        public void Importer_PermissiveVariables_CheckSyntaxOnly()
        {
            Assert.IsEmpty(ScriptImportValidator.Validate("Assets/a.script", Lines(
                "S:",
                "    set $anything to 5",
                "    set $other to [$a + $b]",
                "    add 1 to $whatever",
                "    if [$a == $b]",
                "        log \"a\"")));
            Assert.IsNotEmpty(ScriptImportValidator.Validate("Assets/a.script", "S:\n    set $a\n"));
        }

        [Test]
        public void Importer_VariablesByDeclarations_ErrorsWithPathAndColumn()
        {
            var messages = ScriptImportValidator.Validate("Assets/a.script", Lines("S:", "    add 1 to $money", "    set $coins to 1"),
                null, new VariableScriptResolver(m_Catalog));

            Assert.That(messages, Is.EqualTo(new[] { "Assets/a.script(2,14): variable '$money' is renamed to '$gold'" }));
        }

#endregion
    }
}
