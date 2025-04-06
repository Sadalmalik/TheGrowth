using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sadalmalik.TheGrowth
{
    public class EntityCard : MonoBehaviour
    {
        public CardView view;
        [FormerlySerializedAs("config")]
        public CardModel model;
        public CardBrain brain;

        public bool IsAnimated { get; private set; }
        public EntitySlot Slot { get; set; }
        public bool AllowEvents { get; set; } = true;

        public bool CanBeDragged => brain?.CanBeDragged ?? false;
        public bool EndsTheTurn => brain?.EndsTheTurn ?? false;

        public bool IsFaceUp => view.IsFaceUp;

        public void SetConfig(CardModel model)
        {
            this.model = model;
            brain = this.model.GetComponent<CardBrain>();
        }

#region Commands

        public void MoveTo(EntitySlot newSlot, bool instant = false, bool allowAfter = false, Action<EntityCard> callback=null)
        {
            if (Slot != null)
            {
                Slot.Cards.Remove(this);
            }

            var oldSlot = Slot;
            Slot = newSlot;
            Slot.Cards.Add(this);

            if (view != null)
            {
                IsAnimated = true;
                view.MoveTo(newSlot, MoveComplete, instant: instant);
            }

            void MoveComplete()
            {
                IsAnimated = false;

                if (AllowEvents)
                {
                    if (oldSlot != null)
                    {
                        var oldUnder = oldSlot.Cards.Count - 2;
                        if (oldUnder >= 0)
                        {
                            oldSlot.Cards[oldUnder].OnUnCovered(this);
                        }
                    }

                    var newUnder = Slot.Cards.Count - 2;
                    if (newUnder >= 0)
                    {
                        Slot.Cards[newUnder].OnCovered(this);
                    }

                    OnPlaced();
                }

                if (allowAfter)
                {
                    AllowEvents = true;
                }
                
                callback?.Invoke(this);
            }
        }

        public void FlipCard()
        {
            if (view != null)
            {
                IsAnimated = true;
                view.Flip(FlipFace);
            }

            void FlipFace()
            {
                IsAnimated = false;

                if (AllowEvents)
                {
                    OnFlipped();
                }
            }
        }

#endregion


#region Behaviour

        public void OnPlaced()
        {
            if (brain?.OnPlaced == null)
                return;
            var context = new Context();
            context.Add(new ActiveCard.Data { Card = this });
            brain.OnPlaced.ExecuteAll(context);
        }
        
        public void OnPlacedFirstTime()
        {
            if (brain?.OnPlacedFirstTime == null)
                return;
            var context = new Context();
            context.Add(new ActiveCard.Data { Card = this });
            brain.OnPlacedFirstTime.ExecuteAll(context);
        }

        public void OnFlipped()
        {
            if (brain?.OnFlipped == null)
                return;
            var context = new Context(new ActiveCard.Data { Card = this });
            brain.OnFlipped.ExecuteAll(context);
        }

        public void OnCovered(EntityCard coverCard)
        {
            if (brain?.OnCovered == null)
                return;
            var context = new Context(
                new ActiveCard.Data { Card = this },
                new CoveringCard.Data { Card = coverCard });
            brain.OnCovered.ExecuteAll(context);
        }

        public void OnUnCovered(EntityCard coverCard)
        {
            if (brain?.OnUnCovered == null)
                return;
            var context = new Context(
                new ActiveCard.Data { Card = this },
                new CoveringCard.Data { Card = coverCard });
            brain.OnUnCovered.ExecuteAll(context);
        }

        public void OnStep(Context context)
        {
            if (!IsFaceUp)
                return;
            if (brain?.OnStep == null)
                return;
            var newContext = new Context(context, new ActiveCard.Data { Card = this });
            brain.OnStep.ExecuteAll(newContext);
        }

        public HashSet<EntitySlot> GetAllowedMoves()
        {
            if (brain?.AllowedMoves == null)
                return null;

            var context = new Context(new ActiveCard.Data { Card = this });
            var moves = brain.AllowedMoves?.Evaluate(context);
            return moves;
        }

#endregion
    }
}