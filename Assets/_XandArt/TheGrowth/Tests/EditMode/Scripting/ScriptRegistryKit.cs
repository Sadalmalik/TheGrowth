using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Tests.EditMode.Scripting
{
    // Заведомо неверные шаблоны для тестов реестра. Каждый тест строит реестр только из нужных типов;
    // сборка тестов в общий реестр (импортёр, игра) не попадает — ScriptRegistry.IsScriptAssembly.

#region Names and skeletons

    [Script("dup", "dup one")]
    public sealed class DuplicateNameA : Command
    {
        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("dup", "dup two")]
    public sealed class DuplicateNameB : Command
    {
        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("same1", "same [A]")]
    public sealed class SameSkeletonA : Command
    {
        public int A;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("same2", "same [B]")]
    public sealed class SameSkeletonB : Command
    {
        public string B;

        public override UniTask Execute(ScriptContext context) => default;
    }

    /// <summary>Эвалуатор bool со скелетом кондишена <see cref="SameSkeletonCondition"/>: кондишен подставляется вместо bool — конфликт.</summary>
    [Script("same4", "same [A]")]
    public sealed class SameSkeletonBoolEvaluator : Evaluator<bool>
    {
        public int A;

        public override bool Evaluate(ScriptContext context) => false;
    }

    /// <summary>Тот же скелет, но другой вид результата (кондишен) — допустимо.</summary>
    [Script("same3", "same [A]")]
    public sealed class SameSkeletonCondition : Condition
    {
        public int A;

        public override bool Check(ScriptContext context) => true;
    }

    [Script("mixed1", "mixed one")]
    [Script("mixed2", "mixed two")]
    public sealed class MixedNames : Command
    {
        public override UniTask Execute(ScriptContext context) => default;
    }

#endregion

#region Fields

    [Script("missing", "missing [Nope]")]
    public sealed class MissingField : Command
    {
        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("badblock", "badblock {Value}")]
    public sealed class BlockOnInt : Command
    {
        public int Value;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("blockparam", "blockparam [Body]")]
    public sealed class BlockInBrackets : Command
    {
        public List<Command> Body;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("restmid", "restmid [Text...] end")]
    public sealed class RestNotLast : Command
    {
        public string Text;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("restint", "restint [Count...]")]
    public sealed class RestNotString : Command
    {
        public int Count;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("unsupported", "unsupported [Value]")]
    public sealed class UnsupportedField : Command
    {
        public object Value;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("twice", "twice [A] and [A]")]
    public sealed class FieldTwice : Command
    {
        public int A;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("condblock", "condblock {Body}")]
    public sealed class ConditionWithBlock : Condition
    {
        public List<Command> Body;

        public override bool Check(ScriptContext context) => true;
    }

    public enum DuplicateTokenEnum
    {
        [ScriptToken(">")]
        A,

        [ScriptToken(">")]
        B,
    }

    [Script("badenum", "badenum [Value]")]
    public sealed class BadEnumField : Command
    {
        public DuplicateTokenEnum Value;

        public override UniTask Execute(ScriptContext context) => default;
    }

#endregion

#region Template syntax and classes

    [Script("unclosed", "unclosed [A")]
    public sealed class UnclosedBracket : Command
    {
        public int A;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("blockfirst", "{Body} after")]
    public sealed class BlockFirst : Command
    {
        public List<Command> Body;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("twin", "twin {A}{B}")]
    public sealed class AdjacentBlocks : Command
    {
        public List<Command> A;
        public List<Command> B;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("paramcont", "paramcont {A} [X] {B}")]
    public sealed class ContinuationStartsWithParam : Command
    {
        public List<Command> A;
        public int X;
        public List<Command> B;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("plain", "plain")]
    public sealed class NotANode
    {
    }

    [Script("abstract", "abstract")]
    public abstract class AbstractCommand : Command
    {
    }

#endregion

#region Continuations and ambiguity

    [Script("begin", "begin {Body} finally {Tail}")]
    public sealed class BeginFinally : Command
    {
        public List<Command> Body;
        public List<Command> Tail;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("finally", "finally [X]")]
    public sealed class FinallyCommand : Command
    {
        public int X;

        public override UniTask Execute(ScriptContext context) => default;
    }

    [Script("pick1", "pick [Values]")]
    public sealed class PickList : InstantCommand
    {
        public List<int> Values;

        protected override void Run(ScriptContext context)
        {
        }
    }

    [Script("pick2", "pick [A], [B]")]
    public sealed class PickPair : InstantCommand
    {
        public int A;
        public int B;

        protected override void Run(ScriptContext context)
        {
        }
    }

#endregion
}
