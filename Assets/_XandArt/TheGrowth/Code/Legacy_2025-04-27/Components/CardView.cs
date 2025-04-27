using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XandArt.TheGrowth
{
    public partial class CardView : MonoBehaviour
    {
        public Transform model;
        public GameObject faceObject;
        public Image face;
        public Image cover;
        public TMP_Text title;
        public TMP_Text leftText;
        public TMP_Text rightText;

        private CardModel m_Model;
        private Sequence _tween;
        private bool _isFaceUp;

        public bool IsFaceUp => _isFaceUp;

        public event Action OnAnimationComplete;

        public void SetFaceVisible(bool visible)
        {
            faceObject.SetActive(visible);
        }

        public void MoveTo(EntitySlot slot, Action onComplete, bool instant = false)
        {
            var endPosition = slot.GetNewPosition();
            var endRotation = slot.GetNewRotation();

            if (instant)
            {
                transform.position = endPosition;
                transform.rotation = Quaternion.Euler(endRotation);
                onComplete?.Invoke();
                OnAnimationComplete?.Invoke();
                return;
            }

            var duration = RootConfig.Instance.jumpDuration;
            _tween = DOTween.Sequence()
                .Append(transform.DOJump(endPosition, 3, 1, duration))
                .Insert(0, transform.DORotate(endRotation, duration))
                .AppendCallback(() =>
                {
                    _tween = null;
                    onComplete?.Invoke();
                    OnAnimationComplete?.Invoke();
                });
        }

        public void Flip(Action onComplete, bool instant = false)
        {
            var angle = model.localRotation.eulerAngles.z;
            if (instant)
            {
                model.rotation = Quaternion.Euler(0, 0, angle);
                onComplete?.Invoke();
                OnAnimationComplete?.Invoke();
            }

            bool wasFaceUp = _isFaceUp;

            if (!_isFaceUp)
                SetFaceVisible(true);

            var duration = RootConfig.Instance.flipDuration;
            angle = (angle + 180) % 360;
            _tween = DOTween.Sequence()
                .Append(model.DOLocalMove(Vector3.up, duration * 0.25f))
                .Append(model.DOLocalRotate(new Vector3(0, 0, angle), duration * 0.5f))
                .Append(model.DOLocalMove(Vector3.zero, duration * 0.25f))
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