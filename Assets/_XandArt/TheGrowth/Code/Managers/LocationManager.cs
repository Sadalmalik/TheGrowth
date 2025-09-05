using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class LocationManager : IShared
    {
        [Inject]
        private MenuManager _menuManager;

        public event Action<Location> OnLocationLoaded;
        public event Action<Location> OnLocationUnloaded;
        
        public void Init()
        {
        }

        public void Dispose()
        {
        }

        public async Task LoadLocation(Location location)
        {
            if (location == null)
                return;

            using var screenTask = await LoadingTracker.CreateAsync();

            _menuManager.SetMainScreenActive(false);
            
            var result = new TaskCompletionSource<bool>();
            var operation = SceneManager.LoadSceneAsync(location.Model.Scene, LoadSceneMode.Additive);
            if (operation != null)
            {
                operation.completed += op => { result.SetResult(true); };
                await result.Task;
                await location.OnLoad();
                OnLocationLoaded?.Invoke(location);
                    
            }
        }

        public async Task UnloadLocation(Location location)
        {
            if (location == null)
                return;

            using var screenTask = await LoadingTracker.CreateAsync();

            _menuManager.SetMainScreenActive(false);
            
            await location.OnUnload();
            OnLocationUnloaded?.Invoke(location);
            
            var result = new TaskCompletionSource<bool>();
            var operation = SceneManager.UnloadSceneAsync(location.Model.Scene);
            if (operation != null)
            {
                operation.completed += op => { result.SetResult(true); };
                await result.Task;
            }
            
            // await screen.HideAsync();
        }
    }
}