using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Tags;
using TheGrowth.Model.Slots;

namespace TheGrowth.Model.Cards
{
    /// <summary>
    /// Тег у карты (из конфига) или у слота (клетка поля — из графа). Один шаблон на карту и слот: два шаблона
    /// <c>[] has tag []</c> совпали бы скелетом. Пусто — false.
    /// </summary>
    [Script("has_tag", "[Target] has tag [Tag]")]
    public sealed class HasTagCondition : Condition
    {
        [ScriptResult("card or slot", typeof(Entity), typeof(Slot))]
        public IEvaluator Target;

        public Tag Tag;

        public override bool Check(ScriptContext context)
        {
            return Target.Evaluate(context) switch
            {
                Entity entity => entity.HasTag(Tag.Name),
                Slot slot => slot.HasTag(Tag.Name),
                _ => false,
            };
        }
    }

    /// <summary>В слоте нет сущностей. Пусто — false.</summary>
    [Script("is_empty", "[Slot] is empty")]
    public sealed class IsEmptyCondition : Condition
    {
        public Evaluator<Slot> Slot;

        public override bool Check(ScriptContext context)
        {
            return Slot.Evaluate(context) is { IsEmpty: true };
        }
    }

    /// <summary>Карта или слот найдены (значение не пустое).</summary>
    [Script("exists", "[Value] exists")]
    public sealed class ExistsCondition : Condition
    {
        [ScriptResult("card or slot", typeof(Entity), typeof(Slot))]
        public IEvaluator Value;

        public override bool Check(ScriptContext context)
        {
            return Value.Evaluate(context) != null;
        }
    }
}
