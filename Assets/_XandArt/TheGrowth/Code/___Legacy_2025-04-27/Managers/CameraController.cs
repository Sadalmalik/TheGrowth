using DG.Tweening;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class CameraController : SingletonMonoBehaviour<CameraController>
    {
        public Transform Root;
        
        public void MoveTo(Vector3 position)
        {
            Root.DOMove(position, CardsViewConfig.Instance.cameraMoveDuration)
                .SetEase(Ease.InOutCubic);
        }
    }
}