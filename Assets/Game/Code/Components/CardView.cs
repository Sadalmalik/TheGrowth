using System;
using DG.Tweening;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class CardView : MonoBehaviour
    {
        public Transform model;
        public MeshRenderer face;
        public MeshRenderer cover;

        private CardConfig _config;
        private Sequence _tween;
        private bool _isFaceUp;

        public bool IsFaceUp => _isFaceUp;

        public event Action OnAnimationComplete;

        public void SetConfig(CardConfig config)
        {
            _config = config;

            face.GetComponent<MeshFilter>().mesh = config.Face.Model;
            face.material = config.Face.Material;

            cover.GetComponent<MeshFilter>().mesh = config.Cover.Model;
            cover.material = config.Cover.Material;
        }

        public void SetFaceVisible(bool visible)
        {
            var sprite = visible ? _config.Face : _config.Cover;
            face.GetComponent<MeshFilter>().mesh = sprite.Model;
            face.material = sprite.Material;
        }

        public void MoveTo(CardSlot slot, Action onComplete, bool instant = false)
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