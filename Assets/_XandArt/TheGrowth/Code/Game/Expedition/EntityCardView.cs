using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public partial class EntityCardView : SerializedMonoBehaviour, IEntityView
    {
        public GameObject Object => gameObject;

        Entity IEntityView.Data { get; set; }

        [FormerlySerializedAs("model")]
        public Transform innerTransform;
        
        public GameObject faceObject;
        public Image decor;
        public Image portrait;

        private Sequence _tween;
        private bool _isFaceUp;

        public bool IsFaceUp => _isFaceUp;

        public event Action OnAnimationComplete;

        public void SetVisual(CardVisual visual)
        {
            decor.sprite = visual.Decor;
            portrait.sprite = visual.Portrait;
        }
        
        public void SetFaceVisible(bool visible)
        {
            faceObject.SetActive(visible);
        }

        public void MoveTo(EntitySlot slot, Action onComplete, bool instant = false, int index=-1)
        {
            var endPosition = slot.GetNewPosition(index);
            var endRotation = slot.GetNewRotation();

            if (instant)
            {
                transform.position = endPosition;
                transform.rotation = Quaternion.Euler(endRotation);
                onComplete?.Invoke();
                OnAnimationComplete?.Invoke();
                return;
            }

            var duration = CardsViewConfig.Instance.jumpDuration;
            _tween?.Kill();
            _tween = DOTween.Sequence()
                .Append(transform.DOJump(endPosition, 3, 1, duration))
                .Insert(0, transform.DORotate(endRotation, duration))
                .AppendCallback(() =>
                {
                    _tween = null;
                    transform.SetParent(slot.SlotView.Object.transform);
                    onComplete?.Invoke();
                    OnAnimationComplete?.Invoke();
                });
        }

        public void Flip(Action onComplete, bool instant = false)
        {
            var angle = innerTransform.localRotation.eulerAngles.z;
            angle = (angle + 180) % 360;
            
            if (instant)
            {
                innerTransform.rotation = Quaternion.Euler(0, 0, angle);
                onComplete?.Invoke();
                OnAnimationComplete?.Invoke();
                return;
            }

            bool wasFaceUp = _isFaceUp;

            if (!_isFaceUp)
                SetFaceVisible(true);

            var duration = CardsViewConfig.Instance.flipDuration;
            _tween?.Kill();
            _tween = DOTween.Sequence()
                .Append(innerTransform.DOLocalMove(Vector3.up, duration * 0.25f))
                .Append(innerTransform.DOLocalRotate(new Vector3(0, 0, angle), duration * 0.5f))
                .Append(innerTransform.DOLocalMove(Vector3.zero, duration * 0.25f))
                .AppendCallback(() =>
                {
                    _tween = null;

                    if (wasFaceUp)
                        SetFaceVisible(false);

                    _isFaceUp = !_isFaceUp;
                    onComplete?.Invoke();
                    OnAnimationComplete?.Invoke();
                });
        }
    }
}