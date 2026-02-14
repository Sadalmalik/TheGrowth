using UnityEngine;

namespace XandArt.TheGrowth.Expedition.Slots
{
    /// <summary>
    /// Возвращает текущий итерируемый слот
    /// </summary>
    public class BoardSlot : Evaluator<SlotEntity>
    {
        [SerializeField]
        private EType _slot;
        
        public override SlotEntity Evaluate(Context context)
        {
            var expeditionManager = context.GetRequired<GlobalData>().container.Get<ExpeditionManager>();

            return _slot switch
            {
                EType.Deck => expeditionManager.Board.DeckSlot,
                EType.Hand => expeditionManager.Board.HandSlot,
                EType.Back => expeditionManager.Board.BackSlot,
                _ => null
            };
        }

        private enum EType
        {
            Deck,
            Hand,
            Back
        }
    }
}