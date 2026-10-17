using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TheGrowth.Core.Scripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.Tests.EditMode.Scripting
{
#region Conditions and evaluators

    public enum CompareOp
    {
        [ScriptToken(">")]
        Greater,

        [ScriptToken("<")]
        Less,

        [ScriptToken(">=")]
        GreaterOrEqual,

        [ScriptToken("==")]
        Equal,
    }

    public enum TestSpeed
    {
        Fast,
        Slow,
    }

    [Script("compare", "[A] [Operator] [B]")]
    public sealed class CompareCondition : Condition
    {
        public Evaluator<int> A;
        public CompareOp Operator;
        public Evaluator<int> B;

        public override bool Check(ScriptContext context)
        {
            var a = A.Evaluate(context);
            var b = B.Evaluate(context);
            return Operator switch
            {
                CompareOp.Greater => a > b,
                CompareOp.Less => a < b,
                CompareOp.GreaterOrEqual => a >= b,
                _ => a == b,
            };
        }
    }

    [Script("not", "not [Condition]")]
    public sealed class NotCondition : Condition
    {
        public Condition Condition;

        public override bool Check(ScriptContext context)
        {
            return !Condition.Check(context);
        }
    }

    [Script("any", "any of [Conditions]")]
    public sealed class AnyCondition : Condition
    {
        public List<Condition> Conditions;

        public override bool Check(ScriptContext context)
        {
            return Conditions.Exists(c => c.Check(context));
        }
    }

    [Script("flag", "flag [Value]")]
    public sealed class FlagCondition : Condition
    {
        public bool Value;

        public override bool Check(ScriptContext context)
        {
            return Value;
        }
    }

    [Script("add", "[A] + [B]")]
    public sealed class AddEvaluator : Evaluator<int>
    {
        public Evaluator<int> A;
        public Evaluator<int> B;

        public override int Evaluate(ScriptContext context)
        {
            return A.Evaluate(context) + B.Evaluate(context);
        }
    }

    [Script("sum", "sum of [Values]")]
    public sealed class SumEvaluator : Evaluator<int>
    {
        public List<int> Values;

        public override int Evaluate(ScriptContext context)
        {
            var sum = 0;
            foreach (var value in Values)
                sum += value;

            return sum;
        }
    }

    /// <summary>Узел переменной: создаёт <see cref="TestVariableResolver"/>.</summary>
    public sealed class TestVariable : Evaluator<int>
    {
        public string Name;

        public override int Evaluate(ScriptContext context)
        {
            return context.GetRequired<Counter>().Value;
        }
    }

#endregion

#region Commands

    [Script("mark", "mark [Name]")]
    public sealed class ScriptMark : InstantCommand
    {
        public string Name;

        protected override void Run(ScriptContext context)
        {
            context.GetRequired<TraceLog>().Entries.Add(Name);
        }
    }

    [Script("give", "give [Count] [Card]")]
    public sealed class GiveCommand : InstantCommand
    {
        public int Count;
        public TestConfig Card;

        protected override void Run(ScriptContext context)
        {
        }
    }

    [Script("wait", "wait [Seconds] seconds")]
    public sealed class WaitSecondsCommand : InstantCommand
    {
        public float Seconds;

        protected override void Run(ScriptContext context)
        {
        }
    }

    [Script("speed", "speed [Speed]")]
    public sealed class SpeedCommand : InstantCommand
    {
        public TestSpeed Speed;

        protected override void Run(ScriptContext context)
        {
        }
    }

    [Script("check", "check [Value]")]
    public sealed class CheckCommand : InstantCommand
    {
        public Evaluator<int> Value;

        protected override void Run(ScriptContext context)
        {
            context.GetRequired<TraceLog>().Entries.Add("check " + Value.Evaluate(context));
        }
    }

    [Script("say", "say [Text...]")]
    public sealed class SayCommand : InstantCommand
    {
        public string Text;

        protected override void Run(ScriptContext context)
        {
        }
    }

    [Script("line", "[Character]: [Text...]", Context = CommandContext.DialogOnly)]
    [Script("line", "[Character].[Emotion]: [Text...]", Context = CommandContext.DialogOnly)]
    public sealed class LineCommand : InstantCommand
    {
        public TestConfig Character;
        public TestConfig Emotion;
        public string Text;

        protected override void Run(ScriptContext context)
        {
        }
    }

    [Script("move", "move [Card] to [Slot]", Context = CommandContext.GameplayOnly)]
    public sealed class MoveCommand : InstantCommand
    {
        public TestConfig Card;
        public int Slot;

        protected override void Run(ScriptContext context)
        {
        }
    }

    [Script("repeat", "repeat {Body} until [Condition]")]
    public sealed class RepeatUntilCommand : Command
    {
        public List<Command> Body = new List<Command>();
        public Condition Condition;

        public override async UniTask Execute(ScriptContext context)
        {
            do
            {
                await context.Runner.RunBlock(Body, context);
            }
            while (!Condition.Check(context));
        }
    }

    [Script("attempt", "attempt {Body} otherwise {Fallback} finally {Cleanup}")]
    public sealed class AttemptCommand : InstantCommand
    {
        public List<Command> Body = new List<Command>();
        public List<Command> Fallback = new List<Command>();
        public List<Command> Cleanup = new List<Command>();

        protected override void Run(ScriptContext context)
        {
        }
    }

#endregion

#region Resolvers

    /// <summary>Конфиги по имени; созданные объекты уничтожаются в <see cref="Dispose"/>.</summary>
    public sealed class TestContentResolver : IScriptContentResolver, IDisposable
    {
        private readonly Dictionary<string, TestConfig> m_Items = new Dictionary<string, TestConfig>();

        public TestConfig Add(string id)
        {
            var item = ScriptableObject.CreateInstance<TestConfig>();
            item.name = id;
            m_Items.Add(id, item);
            return item;
        }

        public TestConfig Get(string id)
        {
            return m_Items[id];
        }

        public bool TryResolve(Type type, string id, out object value)
        {
            if (m_Items.TryGetValue(id, out var item) && type.IsInstanceOfType(item))
            {
                value = item;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetId(object value, out string id)
        {
            if (value is TestConfig item && m_Items.TryGetValue(item.name, out var known) && known == item)
            {
                id = item.name;
                return true;
            }

            id = null;
            return false;
        }

        public void Dispose()
        {
            foreach (var item in m_Items.Values)
                Object.DestroyImmediate(item);

            m_Items.Clear();
        }
    }

    /// <summary>Переменные int: <c>$x</c>, <c>$coins</c>, <c>$trust</c>.</summary>
    public sealed class TestVariableResolver : IScriptVariableResolver
    {
        private readonly HashSet<string> m_Names = new HashSet<string> { "x", "coins", "trust" };

        public bool TryResolve(string name, Type type, out object value, out string error)
        {
            value = null;
            error = null;
            if (!m_Names.Contains(name))
            {
                error = $"unknown variable '${name}'";
                return false;
            }

            if (!type.IsAssignableFrom(typeof(TestVariable)))
            {
                error = $"variable '${name}' is int, expected {type.Name}";
                return false;
            }

            value = new TestVariable { Name = name };
            return true;
        }

        public bool TryGetName(object value, out string name)
        {
            name = (value as TestVariable)?.Name;
            return name != null;
        }
    }

#endregion

    public static class ScriptLanguageTypes
    {
        /// <summary>Встроенные команды Core и тестовые узлы этого файла.</summary>
        public static IEnumerable<Type> All()
        {
            foreach (var type in ScriptRegistry.CollectTypes(new[] { typeof(Command).Assembly }))
                yield return type;

            yield return typeof(CompareCondition);
            yield return typeof(NotCondition);
            yield return typeof(AnyCondition);
            yield return typeof(FlagCondition);
            yield return typeof(AddEvaluator);
            yield return typeof(SumEvaluator);
            yield return typeof(ScriptMark);
            yield return typeof(GiveCommand);
            yield return typeof(WaitSecondsCommand);
            yield return typeof(SpeedCommand);
            yield return typeof(CheckCommand);
            yield return typeof(SayCommand);
            yield return typeof(LineCommand);
            yield return typeof(MoveCommand);
            yield return typeof(RepeatUntilCommand);
            yield return typeof(AttemptCommand);
        }
    }
}
