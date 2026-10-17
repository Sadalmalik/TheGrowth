using System;
using System.Linq;
using NUnit.Framework;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Scripting
{
    public sealed class ScriptRegistryTests
    {
        private static ScriptRegistryException BuildFails(params Type[] types)
        {
            return Assert.Throws<ScriptRegistryException>(() => ScriptRegistry.Build(types));
        }

        private static void AssertError(ScriptRegistryException exception, string fragment)
        {
            Assert.IsTrue(exception.Errors.Any(e => e.Contains(fragment)),
                $"No error containing '{fragment}':\n{string.Join("\n", exception.Errors)}");
        }

        private static ScriptTemplate Single(ScriptRegistry registry, Type type)
        {
            Assert.IsTrue(registry.TryGetTemplates(type, out var templates));
            Assert.AreEqual(1, templates.Count);
            return templates[0];
        }

#region Template compiler

        [Test]
        public void Compile_Branch_HeaderAndOptionalElse()
        {
            var template = Single(ScriptRegistry.Build(new[] { typeof(Branch) }), typeof(Branch));

            Assert.AreEqual("if", template.Name);
            Assert.AreEqual(ScriptNodeKind.Command, template.NodeKind);
            Assert.AreEqual("if [] {} else {}", template.Skeleton);
            Assert.AreEqual(2, template.Lines.Count);

            var header = template.Lines[0];
            Assert.AreEqual(2, header.Elements.Count);
            Assert.AreEqual("if", header.Elements[0].Literal.Text);
            Assert.AreEqual("Condition", header.Elements[1].Param.Name);
            Assert.AreEqual(ScriptValueKind.Condition, header.Elements[1].Param.Value.Kind);
            Assert.AreEqual("OnTrue", header.Block.Name);
            Assert.IsFalse(header.IsOptional);

            var otherwise = template.Lines[1];
            Assert.AreEqual("else", otherwise.Elements[0].Literal.Text);
            Assert.AreEqual("OnFalse", otherwise.Block.Name);
            Assert.IsTrue(otherwise.IsOptional);
        }

        [Test]
        public void Compile_TrailingLine()
        {
            var template = Single(ScriptRegistry.Build(new[] { typeof(RepeatUntilCommand), typeof(FlagCondition) }), typeof(RepeatUntilCommand));

            Assert.AreEqual("repeat {} until []", template.Skeleton);
            Assert.AreEqual(2, template.Lines.Count);
            Assert.AreEqual("Body", template.Lines[0].Block.Name);
            Assert.IsNull(template.Lines[1].Block);
            Assert.IsFalse(template.Lines[1].IsOptional);
            Assert.AreEqual("Condition", template.Lines[1].Params[0].Name);
        }

        [Test]
        public void Compile_RestParamAndPunctuation()
        {
            var registry = ScriptRegistry.Build(new[] { typeof(LineCommand) });
            Assert.IsTrue(registry.TryGetTemplates(typeof(LineCommand), out var templates));

            // Самый подробный шаблон — первым, независимо от порядка атрибутов.
            Assert.AreEqual(2, templates.Count);
            Assert.AreEqual("[] . [] : [...]", templates[0].Skeleton);
            Assert.AreEqual("[] : [...]", templates[1].Skeleton);
            Assert.IsTrue(templates[0].Header.Params[2].IsRest);
            Assert.AreEqual(CommandContext.DialogOnly, templates[0].Context);
            Assert.AreEqual(ScriptValueKind.Content, templates[0].Header.Params[0].Value.Kind);
        }

        [Test]
        public void Compile_ValueKinds()
        {
            var registry = ScriptRegistry.Build(new[] { typeof(CheckCommand), typeof(SumEvaluator), typeof(CompareCondition), typeof(Goto) });

            var check = Single(registry, typeof(CheckCommand)).Header.Params[0].Value;
            Assert.AreEqual(ScriptValueKind.Evaluator, check.Kind);
            Assert.AreEqual(typeof(int), check.ResultType);
            Assert.AreEqual(ScriptValueKind.Integer, check.Constant.Kind);
            Assert.AreEqual(typeof(Constant<int>), check.ConstantType);
            Assert.That(check.Candidates.Select(t => t.Type), Is.EquivalentTo(new[] { typeof(SumEvaluator) }));

            var sum = Single(registry, typeof(SumEvaluator)).Header.Params[0];
            Assert.IsTrue(sum.IsList);
            Assert.AreEqual(ScriptValueKind.Integer, sum.Value.Kind);

            var op = Single(registry, typeof(CompareCondition)).Header.Params[1].Value;
            Assert.AreEqual(ScriptValueKind.Enum, op.Kind);
            Assert.AreEqual(">", op.Enum.Members.First(m => m.Name == "Greater").Token);

            Assert.AreEqual(ScriptValueKind.Section, Single(registry, typeof(Goto)).Header.Params[0].Value.Kind);
        }

        [TestCase(typeof(UnclosedBracket), "unclosed '['")]
        [TestCase(typeof(BlockFirst), "no header text before the first block")]
        [TestCase(typeof(AdjacentBlocks), "text is required between blocks")]
        [TestCase(typeof(ContinuationStartsWithParam), "must start with a literal word")]
        [TestCase(typeof(MissingField), "field 'Nope' not found")]
        [TestCase(typeof(BlockOnInt), "block field must be List<Command>")]
        [TestCase(typeof(BlockInBrackets), "List<Command> is a block")]
        [TestCase(typeof(RestNotLast), "must be the last element of its line")]
        [TestCase(typeof(RestNotString), "must be a string field")]
        [TestCase(typeof(UnsupportedField), "unsupported parameter type System.Object")]
        [TestCase(typeof(FieldTwice), "is used twice")]
        [TestCase(typeof(ConditionWithBlock), "blocks are allowed only in command templates")]
        [TestCase(typeof(BadEnumField), "'>' is used by A and B")]
        [TestCase(typeof(NotANode), "must derive from Command, Condition or Evaluator<T>")]
        [TestCase(typeof(AbstractCommand), "abstract class")]
        [TestCase(typeof(MixedNames), "must have the same name")]
        public void Compile_InvalidTemplate_Fails(Type type, string fragment)
        {
            AssertError(BuildFails(type), fragment);
        }

#endregion

#region Registry validation

        [Test]
        public void Validate_DuplicateNames()
        {
            AssertError(BuildFails(typeof(DuplicateNameA), typeof(DuplicateNameB)), "script name 'dup' is used by");
        }

        [Test]
        public void Validate_DuplicateSkeletons()
        {
            AssertError(BuildFails(typeof(SameSkeletonA), typeof(SameSkeletonB)), "the same skeleton 'same []'");
        }

        [Test]
        public void Validate_SameSkeletonOfOtherResultKind_IsAllowed()
        {
            Assert.DoesNotThrow(() => ScriptRegistry.Build(new[] { typeof(SameSkeletonA), typeof(SameSkeletonCondition) }));
        }

        [Test]
        public void Validate_ContinuationWordStartsCommand()
        {
            AssertError(BuildFails(typeof(BeginFinally), typeof(FinallyCommand)), "continuation word 'finally' of 'begin'");
        }

        [Test]
        public void Validate_CollectsAllProblems()
        {
            var exception = BuildFails(typeof(MissingField), typeof(BlockOnInt), typeof(DuplicateNameA), typeof(DuplicateNameB));

            Assert.AreEqual(3, exception.Errors.Count, string.Join("\n", exception.Errors));
            StringAssert.Contains("3 problem(s)", exception.Message);
        }

        [Test]
        public void Validate_BuiltInsAndTestNodes_AreValid()
        {
            var registry = ScriptRegistry.Build(ScriptLanguageTypes.All());

            foreach (var name in new[] { "if", "goto", "call", "return", "parallel", "background", "log" })
                Assert.IsTrue(registry.TryGetTemplates(name, out _), name);
        }

        [Test]
        public void LoadedAssemblies_ExcludeTestAssemblies()
        {
            Assert.IsTrue(ScriptRegistry.IsScriptAssembly(typeof(Command).Assembly));
            Assert.IsFalse(ScriptRegistry.IsScriptAssembly(typeof(ScriptRegistryTests).Assembly));
            Assert.IsFalse(ScriptRegistry.IsScriptAssembly(typeof(object).Assembly));

            var registry = ScriptRegistry.FromLoadedAssemblies();
            Assert.IsTrue(registry.TryGetTemplates(typeof(Branch), out _));
            Assert.IsFalse(registry.TryGetTemplates(typeof(ScriptMark), out _));
        }

        [Test]
        public void Match_Ambiguous_IsParseError()
        {
            var registry = ScriptRegistry.Build(new[] { typeof(PickList), typeof(PickPair) });
            var parser = new ScriptParser(registry);

            var result = parser.Parse("test", ScriptFileKind.Script, "S:\n    pick 1, 2\n    pick 1, 2, 3\n    pick 1\n");

            Assert.AreEqual(1, result.Errors.Count, result.ToString());
            Assert.AreEqual(2, result.Errors[0].Line);
            StringAssert.Contains("ambiguous", result.Errors[0].Message);
            StringAssert.Contains("pick1", result.Errors[0].Message);
            StringAssert.Contains("pick2", result.Errors[0].Message);
            Assert.AreEqual(2, result.File.Sections[0].Commands.Count);
        }

#endregion
    }
}
