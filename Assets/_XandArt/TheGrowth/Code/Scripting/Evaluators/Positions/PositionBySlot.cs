using UnityEngine;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает координаты слота
    /// </summary>
    public class PositionBySlot : Evaluator<Vector2Int>
    {
        public Evaluator<SlotEntity> Slot;

        public override Vector2Int Evaluate(Context context)
        {
            var slot = Slot.Evaluate(context);
            return slot.Index;
        }
    }
}