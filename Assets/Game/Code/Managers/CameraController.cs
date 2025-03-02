using DG.Tweening;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class CameraController : SingletonMonoBehaviour<CameraController>
    {
        public Transform Root;
        
        public void MoveTo(Vector3 position)
        {
            Root.DOMove(position, RootConfig.Instance.cameraMoveDuration)
                .SetEase(Ease.InOutCubic);
        }
    }
}