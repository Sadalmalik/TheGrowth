using TheGrowth.Core.Entities;
using TheGrowth.Core.Scripting;
using TheGrowth.Model.Slots;

namespace TheGrowth.Model.Cards
{
    /// <summary>
    /// Расширение языка скриптов карт: карта и слот — типы значений (<c>[A] == [B]</c>, <c>exists</c>, <c>has tag</c>),
    /// одиночная карта и слот там, где ждут набор, — набор из одного, свойства файла карты (<see cref="CardProperties"/>).
    /// </summary>
    public sealed class CardLanguage : ScriptExtension
    {
        public override void Declare(ScriptDeclarations declarations)
        {
            declarations.AddValueType<Entity>();
            declarations.AddValueType<Slot>();
            declarations.AddConversion<Entity, CardSet>(CardSet.Of);
            declarations.AddConversion<Slot, SlotSet>(SlotSet.Of);
            declarations.AddProperty<SlotSet>(CardProperties.AllowedMoves);
            declarations.AddProperty<SlotSet>(CardProperties.SpawnSlot);
        }
    }
}
