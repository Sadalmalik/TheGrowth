using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Возвращает ближайший к карте слот
    /// </summary>
    public class NearestSlot : Evaluator<SlotEntity>
    {
        public Evaluator<CompositeEntity> Card = new PlayerCard();
        public Evaluator<HashSet<SlotEntity>> Collection;

        public override SlotEntity Evaluate(Context context)
        {
            var set = Collection.Evaluate(context);
            if (set.Count == 0) return null;

            var cardEntity = Card.Evaluate(context);
            if (cardEntity == null) return null;
            var card = cardEntity.GetComponent<CardBrain.Component>();
            var bestSlot = set.First();
            var bestDist = float.PositiveInfinity;

            foreach (var slot in set)
            {
                var dist = Vector3.Distance(slot.Position, card.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestSlot = slot;
                }
            }

            return bestSlot;
        }
    }
}