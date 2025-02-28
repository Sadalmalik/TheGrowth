using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sadalmalik.TheGrowth
{
    public class CardEntity : MonoBehaviour
    {
        public CardView view;
        public CardConfig config;
        
        public bool IsAnimated { get; private set; }
        public CardSlot Slot { get; set; }


        public void SetConfig(CardConfig Config)
        {
            config = Config;
            view.SetConfig(config);
        }

        private float _dragStart;

        void OnMouseDown()
        {
            if (IsAnimated)
                return;

            _dragStart = Time.time;
        }

        private void OnMouseDrag()
        {
            if (IsAnimated) return;
            if (Camera.main == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, float.PositiveInfinity, 8))
            {
                transform.position = hit.point + Vector3.up * 0.3f;
            }
        }

        private void OnMouseUp()
        {
            var dt = Time.time - _dragStart;

            if (dt < 0.2f)
            {
                FlipCard();
            }

            if (Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, float.PositiveInfinity, 8))
            {
                transform.position = hit.point + Vector3.up * 0.1f;
            }
        }

        public void MoveTo(CardSlot slot, bool instant=false)
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
            else
            {
                // aaaa
            }

            void MoveComplete()
            {
                IsAnimated = false;
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
            }
        }
    }
}