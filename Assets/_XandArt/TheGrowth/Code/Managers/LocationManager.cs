using System.Threading.Tasks;
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

        public async void LoadLocation(Location location)
        {
            var screen = _menuManager.LoadingScreen;
            await screen.ShowAsync();

            _menuManager.SetMainScreenActive(false);
            
            var result = new TaskCompletionSource<bool>();
            var operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(location.Scene, LoadSceneMode.Additive);
            if (operation != null)
            {
                operation.completed += op => { result.SetResult(true); };
                await result.Task;
            }

            await screen.HideAsync();
        }
    }
}