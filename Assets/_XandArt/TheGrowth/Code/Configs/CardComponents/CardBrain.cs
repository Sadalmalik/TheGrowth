using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [Flags]
    public enum CardType
    {
        None = 0b00000000_00000000_00000000_00000000,
        
        Character = 0b00000001,
        Ability   = 0b00000010,
        Resource  = 0b00000100,
        Monster   = 0b00001000,
        
        All = Character | Ability | Resource | Monster
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
            var component = card.AddComponent<Component>();
            component.Brain = this;
        }

        public class Component : EntityComponent
        {
            [JsonProperty]
            private Ref<EntitySlot> _slot;

            [JsonProperty]
            public Vector3 Position;
            
            [JsonProperty]
            public bool IsFaceUp;

            [JsonIgnore]
            public CardBrain Brain;

            [JsonIgnore]
            public EntitySlot Slot
            {
                get => _slot;
                set => _slot = value;
            }

            public override void OnPostLoad()
            {
                base.OnPostLoad();
                Brain = Owner.Model.GetComponent<CardBrain>();
            }

            public void OnPlaced()
            {
                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner });
                Brain.OnPlaced.ExecuteAll(context);
            }

            public void OnPlacedFirstTime()
            {
                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner });
                Brain.OnPlacedFirstTime.ExecuteAll(context);
            }

            public void OnFlipped()
            {
                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner });
                Brain.OnFlipped.ExecuteAll(context);
            }

            public void OnCovered(Entity coverCard)
            {
                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner },
                    new CoveringCard.Data { Card = coverCard });
                Brain.OnCovered.ExecuteAll(context);
            }

            public void OnUnCovered(Entity coverCard)
            {
                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner },
                    new CoveringCard.Data { Card = coverCard });
                Brain.OnUnCovered.ExecuteAll(context);
            }

            public void OnStep()
            {
                if (!IsFaceUp)
                    return;
                var newContext = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner });
                Brain.OnStep.ExecuteAll(newContext);
            }

            public HashSet<EntitySlot> GetAllowedMoves()
            {
                if (Brain?.AllowedMoves == null)
                    return null;

                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner });
                var moves = Brain.AllowedMoves?.Evaluate(context);
                return moves;
            }
        }
    }
}