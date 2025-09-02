using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public enum LoadingScreenState
    {
        Hidden,
        Shown,
        ShowAnimation,
        HideAnimation
    }

    public partial class LoadingScreen : SerializedMonoBehaviour, IShared
    {
        [SerializeField]
        private CanvasGroup _group;

        [SerializeField]
        private float _fadeDuration;

        [SerializeField]
        private Transform _roller;

        [SerializeField]
        private Vector3 _rotationSpeed;

        public void Init()
        {
            Debug.Log($"TEST - INTI");
            LoadingTracker.Created += OnCreated;
            LoadingTracker.Disposed += OnDisposed;
        }

        public void Dispose()
        {
            LoadingTracker.Created -= OnCreated;
            LoadingTracker.Disposed -= OnDisposed;
        }

        private Task OnCreated(LoadingTracker tracker)
        {
            Debug.Log($"TEST - OnCreated");
            return ShowAsync();
        }

        private Task OnDisposed(LoadingTracker tracker)
        {
            Debug.Log($"TEST - OnDisposed");
            return HideAsync();
        }

        private void Update()
        {
            if (_roller != null)
                _roller.rotation = Quaternion.Euler(_rotationSpeed * Time.unscaledTime);
        }

        public async Task ShowAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            _group.DOFade(1, _fadeDuration)
                .OnStart(() =>
                {
                    _group.blocksRaycasts = true;
                    _group.gameObject.SetActive(true);
                })
                .OnComplete(() => tcs.SetResult(true));
            await tcs.Task;
        }

        public async Task HideAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            _group.DOFade(0, _fadeDuration)
                .OnComplete(() =>
                {
                    _group.blocksRaycasts = false;
                    _group.gameObject.SetActive(false);
                    tcs.SetResult(false);
                });
            await tcs.Task;
        }
    }
}