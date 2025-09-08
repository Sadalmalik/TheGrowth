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
        public Evaluator<SlotEntity> SpawnSlot;

        public Evaluator<HashSet<SlotEntity>> AllowedMoves;

        [Space]
        public List<Command> OnPlacedFirstTime;
        public List<Command> OnPlaced;
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
            private Ref<SlotEntity> _slot;

            [JsonProperty]
            public Vector3 Position;
            
            [JsonProperty]
            public bool IsFaceUp;

            [JsonIgnore]
            public CardBrain Brain;

            [JsonIgnore]
            public SlotEntity Slot
            {
                get => _slot;
                set => _slot = value;
            }

            public void FlipCard(Action onComplete, bool instant = false)
            {
                IsFaceUp = !IsFaceUp;
                var view = Owner.View as EntityCardView;
                if (view == null)
                {
                    OnFlipped();
                    onComplete?.Invoke();
                    return;
                }
                if (view.IsFaceUp != IsFaceUp)
                    view.Flip(onComplete, instant);
                onComplete?.Invoke();
            }

            public override void OnPostLoad()
            {
                base.OnPostLoad();
                Brain = Owner.Model.GetComponent<CardBrain>();
            }

            public void OnPlacedFirstTime()
            {
                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner });
                Brain.OnPlacedFirstTime.ExecuteAll(context);
            }

            public void OnPlaced()
            {
                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner });
                Brain.OnPlaced.ExecuteAll(context);
            }

            public void OnFlipped()
            {
                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner });
                Brain.OnFlipped.ExecuteAll(context);
            }

            public void OnCovered(CompositeEntity coverCard)
            {
                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner },
                    new CoveringCard.Data { Card = coverCard });
                Brain.OnCovered.ExecuteAll(context);
            }

            public void OnUnCovered(CompositeEntity coverCard)
            {
                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner },
                    new CoveringCard.Data { Card = coverCard });
                Brain.OnUnCovered.ExecuteAll(context);
            }

            public bool OnStep()
            {
                if (!IsFaceUp)
                    return false;
                var newContext = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner });
                return Brain.OnStep.ExecuteAll(newContext);
            }

            public HashSet<SlotEntity> GetAllowedMoves()
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