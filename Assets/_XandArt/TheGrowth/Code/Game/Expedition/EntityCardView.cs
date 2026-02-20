using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using UnityEngine.UI;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public partial class EntityCardView : SerializedMonoBehaviour, IEntityView
    {
        public GameObject Object => gameObject;

        public Entity Data { get; set; }

        [FormerlySerializedAs("model")]
        public Transform innerTransform;

        public GameObject faceObject;
        public Canvas faceCanvas;
        public Canvas coverCanvas;
        public Image decor;
        public Image portrait;
        public TMP_Text charges;
        public Collider cardCollider;

        private Sequence _moveTween;
        private Sequence _flipTween;
        private bool _isFaceUp;

        public bool IsFaceUp => _isFaceUp;

        public event Action OnAnimationComplete;

        private Charges.Component m_Charges;

        public void Bind(CompositeEntity card)
        {
            var visual = card.Model.GetComponent<CardVisual>();
            if (visual != null)
            {
                decor.sprite = visual.Decor;
                portrait.sprite = visual.Portrait;
                SetFaceVisible(false);
                _isFaceUp = false;
            }

            m_Charges = card.GetComponent<Charges.Component>();
        }

        private void Update()
        {
            var newText = m_Charges is { ShowOnCard: true }
                ? m_Charges.Charges.ToString()
                : "";
            if (!charges.text.Equals(newText))
                charges.SetText(newText);
        }

        public void SetCanvasSortingOrder(int order)
        {
            faceCanvas.sortingOrder = order;
            coverCanvas.sortingOrder = order;
        }
        
        public void SetFaceVisible(bool visible)
        {
            faceObject.SetActive(visible);
        }

        public void SlideInSlot(SlotEntity slot, Action onComplete, bool instant = false, int index = -1)
        {
            var brain = (Data as CompositeEntity)?.GetComponent<CardBrain.Component>();
            Assert.AreEqual(brain.Slot, slot);

            var endPosition = slot.GetNewPosition(index);
            var endRotation = slot.GetNewRotation();

            var duration = CardsViewConfig.Instance.shiftDuration;
            _moveTween?.Kill();
            _moveTween = DOTween.Sequence()
                .Append(transform.DOMove(endPosition, duration))
                .Insert(0, transform.DORotate(endRotation, duration))
                .AppendCallback(OnMoveEnd);

            void OnMoveEnd()
            {
                _moveTween = null;
                transform.position = endPosition;
                transform.rotation = Quaternion.Euler(endRotation);
                transform.SetParent(slot.SlotView.Object.transform);
                onComplete?.Invoke();
                OnAnimationComplete?.Invoke();
            }
        }

        public void MoveTo(SlotEntity slot, Action onComplete, bool instant = false, int index = -1)
        {
            var endPosition = slot.GetNewPosition(index);
            var endRotation = slot.GetNewRotation();

            if (instant)
            {
                OnMoveEnd();
                return;
            }

            var duration = CardsViewConfig.Instance.jumpDuration;
            _moveTween?.Kill();
            _moveTween = DOTween.Sequence()
                .Append(transform.DOJump(endPosition, 3, 1, duration))
                .Insert(0, transform.DORotate(endRotation, duration))
                .AppendCallback(OnMoveEnd);

            void OnMoveEnd()
            {
                _moveTween = null;
                transform.position = endPosition;
                transform.rotation = Quaternion.Euler(endRotation);
                transform.SetParent(slot.SlotView.Object.transform);
                onComplete?.Invoke();
                OnAnimationComplete?.Invoke();
            }
        }

        public void Flip(Action onComplete, bool instant = false)
        {
            var angle = innerTransform.localRotation.eulerAngles.z;
            angle = (angle + 180) % 360;

            bool wasFaceUp = _isFaceUp;

            if (instant)
            {
                innerTransform.rotation = Quaternion.Euler(0, 0, angle);

                _isFaceUp = !_isFaceUp;
                SetFaceVisible(_isFaceUp);

                onComplete?.Invoke();
                OnAnimationComplete?.Invoke();
                return;
            }

            if (!_isFaceUp)
                SetFaceVisible(true);

            var duration = CardsViewConfig.Instance.flipDuration;
            _flipTween?.Kill();
            _flipTween = DOTween.Sequence()
                .Append(innerTransform.DOLocalMove(Vector3.up, duration * 0.25f))
                .Append(innerTransform.DOLocalRotate(new Vector3(0, 0, angle), duration * 0.5f))
                .Append(innerTransform.DOLocalMove(Vector3.zero, duration * 0.25f))
                .AppendCallback(() =>
                {
                    _flipTween = null;

                    if (wasFaceUp)
                        SetFaceVisible(false);

                    _isFaceUp = !_isFaceUp;
                    onComplete?.Invoke();
                    OnAnimationComplete?.Invoke();
                });
        }
    }
}