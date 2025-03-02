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

        public void MoveTo(EntitySlot slot, bool instant = false)
        {
            if (Slot != null)
                Slot.Cards.Remove(this);
            Slot = slot;

            if (view != null)
            {
                IsAnimated = true;
                slot.Cards.Add(this);
                view.MoveTo(slot, MoveComplete, instant: instant);
            }

            void MoveComplete()
            {
                IsAnimated = false;

                if (AllowEvents)
                {
                    OnPlaced();
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
            context.Add(new ActiveCardData { Card = this });
            config.OnPlaced.ExecuteAll(context);
        }

        public void OnFlipped()
        {
            if (config.OnFlipped == null)
                return;
            var context = new Context(new ActiveCardData { Card = this });
            config.OnFlipped.ExecuteAll(context);
        }

        public void OnStep()
        {
            if (!IsFaceUp)
                return;
            if (config.OnStep == null)
                return;
            var context = new Context(new ActiveCardData { Card = this });
            config.OnStep.ExecuteAll(context);
        }

        public HashSet<EntitySlot> GetAllowedMoves()
        {
            if (config.AllowedMoves == null)
                return null;

            var context = new Context(new ActiveCardData { Card = this });
            var moves = config.AllowedMoves?.Evaluate(context);
            return moves;
        }

#endregion
    }
}