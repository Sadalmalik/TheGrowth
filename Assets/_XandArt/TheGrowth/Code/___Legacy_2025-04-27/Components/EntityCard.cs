using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [Obsolete]
    public class EntityCard : MonoBehaviour
    {
        public EntityCardView view;
        [FormerlySerializedAs("config")]
        public EntityModel model;
        public CardBrain ModelBrain;

        public bool IsAnimated { get; private set; }
        public SlotEntity Slot { get; set; }
        public bool AllowEvents { get; set; } = true;

        public bool CanBeDragged => ModelBrain?.CanBeDragged ?? false;
        public bool EndsTheTurn => ModelBrain?.EndsTheTurn ?? false;

        public bool IsFaceUp => view.IsFaceUp;

        public void SetConfig(EntityModel model)
        {
            this.model = model;
            ModelBrain = this.model.GetComponent<CardBrain>();
        }

#region Commands

        public void MoveTo(SlotEntity newSlot, bool instant = false, bool allowAfter = false, Action<EntityCard> callback=null)
        {
            if (Slot != null)
            {
                //Slot.Remove(this);
            }

            var oldSlot = Slot;
            Slot = newSlot;
            //Slot.Add(this);

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
                    // if (oldSlot != null)
                    // {
                    //     var oldUnder = oldSlot.Cards.Count - 2;
                    //     if (oldUnder >= 0)
                    //     {
                    //         oldSlot.Cards[oldUnder].OnUnCovered(this);
                    //     }
                    // }
                    //
                    // var newUnder = Slot.Cards.Count - 2;
                    // if (newUnder >= 0)
                    // {
                    //     Slot.Cards[newUnder].OnCovered(this);
                    // }

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

        public void OnPlaced() { }
        public void OnPlacedFirstTime() { }
        public void OnFlipped() { }
        public void OnCovered(EntityCard coverCard) { }
        public void OnUnCovered(EntityCard coverCard) { }
        public void OnStep(Context context) { }

        public HashSet<SlotEntity> GetAllowedMoves()
        {
            return null;
        }

#endregion
    }
}