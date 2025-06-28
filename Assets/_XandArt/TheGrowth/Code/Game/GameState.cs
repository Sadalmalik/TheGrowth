using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class GameState : PersistentState
    {
        [JsonProperty]
        private AssetRef<GameStep> _currentStep;
        
        [JsonProperty]
        private Ref<Inventory> _inventory;

        [JsonProperty]
        private List<AssetRef<Location>> _locations = new List<AssetRef<Location>>();

        [JsonProperty]
        private AssetRef<Location> _activeLocation;
        
        // Access
        [JsonIgnore]
        public GameStep CurrentGameStep => _currentStep;
        
        [JsonIgnore] public Inventory Inventory => _inventory;

        [JsonIgnore]
        private List<GameStep> _cachedLocations;

        [JsonIgnore]
        public List<AssetRef<Location>> Locations => _locations;

        [JsonIgnore]
        public AssetRef<Location> ActiveLocation
        {
            get => _activeLocation;
            set => _activeLocation = value;
        }

        public void SetGameStep(GameStep next)
        {
            _currentStep.Value?.OnStepComplete(this);
            _currentStep = next;
            _currentStep.Value?.OnStepStart(this);
        }
        
        private GameState() { }
        
        public static GameState Create(GameStep start)
        {
            // Game structure preset
            var state = new GameState
            {
                _currentStep = start,
                _inventory = new Inventory()
            };
            state.Add(state._inventory);
            start.OnStepStart(state);
            return state;
        }
    }
}