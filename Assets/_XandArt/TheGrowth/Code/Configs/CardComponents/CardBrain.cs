using System;
using System.Collections.Generic;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [Flags]
    public enum CardType
    {
        None       = 0b00000000_00000000_00000000_00000000,
        Resource   = 0b00000001,
        Monster    = 0b00000010,
        Player     = 0b00000100,
        Ability    = 0b00001000,
        Character  = 0b00010000,
    }
    
    public class CardBrain : IEntityModelComponent
    {
        public CardType Type;
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