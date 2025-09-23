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
    public class Location : Entity
    {
#region Savable

        [JsonProperty]
        private Ref<BoardEntity> _board;

        [JsonProperty]
        private int _steps;

        [JsonProperty]
        private bool _isSaved;
        
#endregion


#region Model

        [JsonIgnore]
        public LocationModel Model => (LocationModel)_model;

        [JsonIgnore]
        [Inject]
        private GameManager _gameManager;

        [JsonIgnore]
        [Inject]
        private MenuManager _menuManager;

        [JsonIgnore]
        public ExpeditionHierarchy Hierarchy { get; private set; }

        [JsonIgnore]
        public BoardEntity Board => _board;
        
        [JsonIgnore]
        public bool IsSaved => _isSaved;

        [JsonIgnore]
        public int Steps
        {
            get => _steps;
            set => _steps = value;
        }
        
#endregion


#region Lifecycle

        public override void OnPreSave()
        {
            _isSaved = true;
        }

        public async Task OnLoad()
        {
            Debug.Log($"TEST - Location.Load: {Model} ( {Model.Scene} )");
            
            var scene = SceneManager.GetSceneByName(Model.Scene);
            SceneManager.SetActiveScene(scene);
            
            Hierarchy = scene.Find<ExpeditionHierarchy>();
            if (Hierarchy == null) return;

            var gameState = _gameManager.CurrentGameState;

            if (Hierarchy.tableGrid)
            {
                if (Board == null)
                {
                    _board = gameState.Create<BoardEntity>();
                }

                Board!.Initialize(gameState, Hierarchy.tableGrid);
            }
        }

        public async Task OnUnload()
        {
            Hierarchy = null;
            if (Board == null) return;
            
            var gameState = _gameManager.CurrentGameState;
            Board.Dispose(gameState);
            gameState.Destroy(Board);
            _board = null;
        }

#endregion
    }
}