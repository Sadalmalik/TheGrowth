using System.Collections.Generic;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class EntityModelBrain : IEntityModelComponent
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

        public void OnEntityCreated(CompositeEntity card)
        {
            // pass
        }
    }
}