using System.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public class LoadingScreen : SerializedMonoBehaviour
    {
        [SerializeField]
        private CanvasGroup _group;

        [SerializeField]
        private float _fadeDuration;

        [SerializeField]
        private Transform _roller;

        [SerializeField]
        private Vector3 _rotationSpeed;

        private void Update()
        {
            if (_roller != null)
                _roller.rotation = Quaternion.Euler(_rotationSpeed * Time.unscaledTime);
        }

        [Button]
        public void Show()
        {
            if (!Application.isPlaying) return;
            _group.blocksRaycasts = true;
            _group.DOFade(1, _fadeDuration);
        }

        public Task ShowAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            _group.blocksRaycasts = true;
            _group.DOFade(1, _fadeDuration).OnComplete(()=>tcs.SetResult(true));
            return tcs.Task;
        }

        [Button]
        public void Hide()
        {
            if (!Application.isPlaying) return;
            _group.DOFade(0, _fadeDuration).OnComplete(() =>
            {
                _group.blocksRaycasts = false;
            });
        }

        public Task HideAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            _group.DOFade(0, _fadeDuration).OnComplete(()=>
            {
                _group.blocksRaycasts = false;
                tcs.SetResult(true);
            });
            return tcs.Task;
        }
    }
}