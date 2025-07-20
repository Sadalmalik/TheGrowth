using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using XandArt.Architecture;
using XandArt.Architecture.IOC;
using XandArt.Architecture.Utils;

namespace XandArt.TheGrowth
{
    public class LocationEntity : Entity
    {
        [JsonIgnore]
        public LocationModel Model => (LocationModel)_model;

        [JsonIgnore]
        [Inject]
        private LocationManager _locationManager;

        [JsonIgnore]
        [Inject]
        private MenuManager _menuManager;

        public ExpeditionHierarchy Hierarchy { get; private set; }

        public async Task Load()
        {
            await _locationManager.LoadLocation(this);
            var scene = SceneManager.GetSceneByName(Model.Scene);
            SceneManager.SetActiveScene(scene);
            Hierarchy = scene.Find<ExpeditionHierarchy>();
            Debug.Log($"Scene: {Model.Scene} \nHierarchy: {Hierarchy}");
        }

        public async Task Unload()
        {
            Hierarchy = null;
            await _locationManager.UnloadLocation(this);
        }
    }
}