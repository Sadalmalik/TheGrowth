using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sadalmalik.TheGrowth
{
    public class CardEntity : MonoBehaviour
    {
        public CardConfig config;
        
        public float coef;
        public Vector2 noiseScale;
        
        public Transform model;
        public SpriteRenderer face;
        public SpriteRenderer cover;

        public float flipDuration;
        
        private Sequence _tween;

        public void Awake()
        {
            SetConfig(config);
        }

        public void SetConfig(CardConfig Config)
        {
            config = Config;
            face.sprite = config.Face;
            cover.sprite = config.Cover;
        }

        public void Update()
        {
            var pos = transform.position;

            var rot = Mathf.Lerp(-coef, coef, Mathf.PerlinNoise(
                noiseScale.x * pos.y,
                noiseScale.y *pos.z));
            transform.rotation = Quaternion.Euler(0, rot, 0);
        }

        private float _dragStart;
        
        void OnMouseDown()
        {
            if (_tween != null)
            {
                return;
            }

            _dragStart = Time.time;
        }

        private void OnMouseDrag()
        {
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

        public void FlipCard()
        {
            var angle = model.localRotation.eulerAngles.z;
            angle = (angle + 180) % 360;
            _tween = DOTween.Sequence()
                .Append(model.DOLocalMove(Vector3.up, flipDuration * 0.25f))
                .Append(model.DOLocalRotate(new Vector3(0, 0, angle), flipDuration * 0.5f))
                .Append(model.DOLocalMove(Vector3.zero, flipDuration * 0.25f))
                .AppendCallback(() => _tween = null);
        }
    }
}