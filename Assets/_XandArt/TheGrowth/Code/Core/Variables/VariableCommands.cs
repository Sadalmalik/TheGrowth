using TheGrowth.Core.Scripting;

namespace TheGrowth.Core.Variables
{
    /// <summary>Записать значение в переменную любого типа; тип значения — как у переменной (проверяется при разборе).</summary>
    [Script("set", "set [Variable] to [Value]")]
    public sealed class SetVariable : InstantCommand
    {
        public VariableReference Variable;

        [ScriptTypeOf(nameof(Variable))]
        public IEvaluator Value;

        protected override void Run(ScriptContext context)
        {
            Variable.SetValue(context, Value.Evaluate(context));
        }
    }

    /// <summary>Прибавить число к переменной int; вычитание — отрицательным числом. Переполнение — ошибка исполнения.</summary>
    [Script("add", "add [Amount] to [Variable]")]
    public sealed class AddToVariable : InstantCommand
    {
        public Evaluator<int> Amount;
        public VariableReference<int> Variable;

        protected override void Run(ScriptContext context)
        {
            var amount = Amount.Evaluate(context);
            var store = Variable.GetStore(context);
            store.Set(Variable.Declaration, checked(store.Get<int>(Variable.Declaration) + amount));
        }
    }
}
