using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class CameraController : SingletonMonoBehaviour<CameraController>
    {
        public Transform Root;

        [Header("Zooming")]
        public Transform ZoomAxis;

        public float ZoomMin = -35;
        public float ZoomMax = -90;
        public float ZoomFactor = 0.05f;
        public Vector2 MouseControllPadding = new Vector2(50, 50);

        [Header("Movement")]
        public float MaxVelocity = 5f;

        public float MaxAcceleration = 5f;

        [Range(0f, 1f)]
        public float Damping = 0.2f;

        private Vector3 m_LastDirection;
        private Vector3 m_Velocity;
        private CameraBorders m_Borders;

        public void MoveTo(Vector3 position, Action onComplete = null)
        {
            var tween = Root.DOMove(position, CardsViewConfig.Instance.cameraMoveDuration)
                .SetEase(Ease.InOutCubic);
            if (onComplete != null)
                tween.OnComplete(() => onComplete());
        }

        public void Zoom(bool zoomOut)
        {
            var z = Mathf.Lerp(
                ZoomAxis.localPosition.z,
                zoomOut ? ZoomMax : ZoomMin,
                ZoomFactor);
            ZoomAxis.localPosition = new Vector3(0, 0, z);
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            var acceleration = MaxAcceleration * deltaTime;

            m_LastDirection = GetDirection();

            m_Velocity = m_Velocity * (1 - Damping) + m_LastDirection * acceleration;
            var velocity = m_Velocity.magnitude;
            if (MaxVelocity < velocity)
                m_Velocity *= MaxVelocity / velocity;
            Root.position += m_Velocity * deltaTime;

            if (m_Borders == null)
                m_Borders = FindObjectsByType<CameraBorders>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (m_Borders != null)
            {
                Root.position = m_Borders.ClampPosition(Root.position);
            }
        }

        private Vector3 GetDirection()
        {
            var direction = Vector3.zero;

            var pos = Input.mousePosition;

            if (MouseControllPadding.x == 0)
                MouseControllPadding.x = 1;
            if (MouseControllPadding.y == 0)
                MouseControllPadding.y = 1;
            
            var xMin = MouseControllPadding.x;
            var xMax = Screen.width - MouseControllPadding.x;
            var yMin = MouseControllPadding.y;
            var yMax = Screen.height - MouseControllPadding.y;

            if (xMin < pos.x && pos.x < xMax &&
                yMin < pos.y && pos.y < yMax)
            {
                direction = new Vector3(
                    Input.GetAxis("Horizontal"),
                    0,
                    Input.GetAxis("Vertical")
                );
            }
            else
            {
                if (pos.x <= xMin) direction.x = (pos.x - xMin) / MouseControllPadding.x;
                if (xMax <= pos.x) direction.x = (pos.x - xMax) / MouseControllPadding.x;
                if (pos.y <= yMin) direction.z = (pos.y - yMin) / MouseControllPadding.y;
                if (yMax <= pos.y) direction.z = (pos.y - yMax) / MouseControllPadding.y;
            }

            return direction;
        }
    }
}