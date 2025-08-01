﻿using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class LocationManager : IShared
    {
        [Inject]
        private MenuManager _menuManager;

        public void Init()
        {
        }

        public void Dispose()
        {
        }

        public async Task LoadLocation(LocationEntity locationEntity)
        {
            Debug.LogWarning($"Load location: {locationEntity.Model.Scene}");
            // var screen = _menuManager.LoadingScreen;
            // await screen.ShowAsync();
            using var screenTask = await LoadingTracker.CreateAsync();

            _menuManager.SetMainScreenActive(false);
            
            var result = new TaskCompletionSource<bool>();
            var operation = SceneManager.LoadSceneAsync(locationEntity.Model.Scene, LoadSceneMode.Additive);
            if (operation != null)
            {
                operation.completed += op => { result.SetResult(true); };
                await result.Task;
            }
        }

        public async Task UnloadLocation(LocationEntity locationEntity)
        {
            Debug.LogWarning($"Unload location: {locationEntity.Model.Scene}");
            // var screen = _menuManager.LoadingScreen;
            // await screen.ShowAsync();
            
            using var screenTask = await LoadingTracker.CreateAsync();

            _menuManager.SetMainScreenActive(false);
            
            var result = new TaskCompletionSource<bool>();
            var operation = SceneManager.UnloadSceneAsync(locationEntity.Model.Scene);
            if (operation != null)
            {
                operation.completed += op => { result.SetResult(true); };
                await result.Task;
            }
            
            // await screen.HideAsync();
        }
    }
}