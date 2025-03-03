using System.Collections.Generic;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class EntityCard : MonoBehaviour
    {
        public CardView view;
        public CardConfig config;

        public bool IsAnimated { get; private set; }
        public EntitySlot Slot { get; set; }
        public bool AllowEvents { get; set; } = true;

        public bool IsFaceUp => view.IsFaceUp;

        public void SetConfig(CardConfig Config)
        {
            config = Config;
            view.SetConfig(config);
        }

#region Commands

        public void MoveTo(EntitySlot newSlot, bool instant = false, bool allowAfter = false)
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
            if (config.OnPlaced == null)
                return;
            var context = new Context();
            context.Add(new ActiveCard.Data { Card = this });
            config.OnPlaced.ExecuteAll(context);
        }

        public void OnFlipped()
        {
            if (config.OnFlipped == null)
                return;
            var context = new Context(new ActiveCard.Data { Card = this });
            config.OnFlipped.ExecuteAll(context);
        }

        public void OnCovered(EntityCard coverCard)
        {
            if (config.OnPlaced == null)
                return;
            var context = new Context(
                new ActiveCard.Data { Card = this },
                new CoveringCard.Data { Card = coverCard });
            config.OnPlaced.ExecuteAll(context);
        }

        public void OnUnCovered(EntityCard coverCard)
        {
            if (config.OnFlipped == null)
                return;
            var context = new Context(
                new ActiveCard.Data { Card = this },
                new CoveringCard.Data { Card = coverCard });
            config.OnFlipped.ExecuteAll(context);
        }

        public void OnStep()
        {
            if (!IsFaceUp)
                return;
            if (config.OnStep == null)
                return;
            var context = new Context(new ActiveCard.Data { Card = this });
            config.OnStep.ExecuteAll(context);
        }

        public HashSet<EntitySlot> GetAllowedMoves()
        {
            if (config.AllowedMoves == null)
                return null;

            var context = new Context(new ActiveCard.Data { Card = this });
            var moves = config.AllowedMoves?.Evaluate(context);
            return moves;
        }

#endregion
    }
}