using System.Collections.Generic;
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
#region Savable

        [JsonProperty]
        private Ref<EntityBoard> _board;

#endregion

#region Model

        [JsonIgnore]
        public LocationModel Model => (LocationModel)_model;

        [JsonIgnore]
        [Inject]
        private GameManager _gameManager;
        
        [JsonIgnore]
        [Inject]
        private LocationManager _locationManager;

        [JsonIgnore]
        [Inject]
        private MenuManager _menuManager;

        [JsonIgnore]
        public ExpeditionHierarchy Hierarchy { get; private set; }

        [JsonIgnore]
        public EntityBoard Board => _board;

#endregion


#region Lifecycle

        public async Task Load()
        {
            await _locationManager.LoadLocation(this);
            var scene = SceneManager.GetSceneByName(Model.Scene);
            SceneManager.SetActiveScene(scene);
            Hierarchy = scene.Find<ExpeditionHierarchy>();

            var gameState = _gameManager.CurrentGameState;
            if (Hierarchy.tableGrid)
            {
                if (Board == null)
                {
                    _board = gameState.Create<EntityBoard>();
                }
                Board!.Initialize(gameState, Hierarchy.tableGrid);
            }
        }

        public async Task Unload()
        {
            Hierarchy = null;
            await _locationManager.UnloadLocation(this);
        }

#endregion
    }
}