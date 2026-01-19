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
        public float StepDuration;

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
            component.Settings = this;
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
            public CardBrain Settings;

            [JsonIgnore]
            public SlotEntity Slot
            {
                get => _slot;
                set => _slot = value;
            }

            [JsonIgnore]
            public CardType Type => Settings.Type;

            [JsonIgnore]
            public bool CanBeDragged => Settings.CanBeDragged;

            [JsonIgnore]
            public float StepDuration => Settings.StepDuration;

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
                Settings = Owner.Model.GetComponent<CardBrain>();
            }

            public void OnPlacedFirstTime()
            {
                try
                {
                    var context = new Context(
                        Game.BaseContext,
                        new ActiveCard.Data { Card = Owner });
                    Settings.OnPlacedFirstTime.ExecuteAll(context);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            public void OnPlaced()
            {
                try
                {
                    var context = new Context(
                        Game.BaseContext,
                        new ActiveCard.Data { Card = Owner });
                    Settings.OnPlaced.ExecuteAll(context);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            public void OnFlipped()
            {
                try
                {
                    var context = new Context(
                        Game.BaseContext,
                        new ActiveCard.Data { Card = Owner });
                    Settings.OnFlipped.ExecuteAll(context);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            public void OnCovered(CompositeEntity coverCard)
            {
                try
                {
                    var context = new Context(
                        Game.BaseContext,
                        new ActiveCard.Data { Card = Owner },
                        new CoveringCard.Data { Card = coverCard });
                    Settings.OnCovered.ExecuteAll(context);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            public void OnUnCovered(CompositeEntity coverCard)
            {
                try
                {
                    var context = new Context(
                        Game.BaseContext,
                        new ActiveCard.Data { Card = Owner },
                        new CoveringCard.Data { Card = coverCard });
                    Settings.OnUnCovered.ExecuteAll(context);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            public bool OnStep()
            {
                try
                {
                    if (!IsFaceUp)
                        return false;
                    if (Settings.OnStep == null || Settings.OnStep.Count == 0)
                        return false;
                    var newContext = new Context(
                        Game.BaseContext,
                        new ActiveCard.Data { Card = Owner });
                    return Settings.OnStep.ExecuteAll(newContext);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                return false;
            }

            public HashSet<SlotEntity> GetAllowedMoves()
            {
                if (Settings?.AllowedMoves == null)
                    return null;

                var context = new Context(
                    Game.BaseContext,
                    new ActiveCard.Data { Card = Owner });
                var moves = Settings.AllowedMoves?.Evaluate(context);
                return moves;
            }
        }
    }
}