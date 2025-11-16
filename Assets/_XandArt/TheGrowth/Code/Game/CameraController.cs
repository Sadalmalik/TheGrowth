using System;
using DG.Tweening;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class CameraController : SingletonMonoBehaviour<CameraController>
    {
        public Transform Root;
        public Transform ZoomAxis;
        public float ZoomMin = -35;
        public float ZoomMax = -90;
        public float ZoomFactor = 0.05f;

        public void MoveTo(Vector3 position, Action onComplete=null)
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
    }
}