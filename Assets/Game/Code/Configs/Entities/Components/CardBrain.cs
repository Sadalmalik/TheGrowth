using System.Collections.Generic;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class CardBrain : ICardComponent
    {
        public bool CanBeDragged;
        public bool EndsTheTurn;
        [Space]
        public Evaluator<EntitySlot> SpawnSlot;
        public Evaluator<HashSet<EntitySlot>> AllowedMoves;
        [Space]
        public List<Command> OnPlaced;
        public List<Command> OnPlacedFirstTime;
        public List<Command> OnFlipped;
        public List<Command> OnStep;
        public List<Command> OnCovered;
        public List<Command> OnUnCovered;

        public void OnEntityCreated(EntityCard card)
        {
            // pass
        }
    }
}