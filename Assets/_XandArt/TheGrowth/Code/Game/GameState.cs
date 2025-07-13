using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class GameState : PersistentState
    {
#region Layout links

        [JsonProperty]
        private AssetRef<GameStep> _currentStep;
        
        [JsonProperty]
        private Ref<Inventory> _inventory;

        [JsonProperty]
        private List<Ref<LocationEntity>> _allLocations = new List<Ref<LocationEntity>>();
        
        [JsonProperty]
        private List<Ref<LocationEntity>> _expeditionLocations = new List<Ref<LocationEntity>>();

        [JsonProperty]
        private Ref<LocationEntity> _activeLocation;

#endregion


#region Properties

        [JsonIgnore]
        [Inject]
        public Container Container;
        
        // Access
        [JsonIgnore]
        public GameStep CurrentGameStep => _currentStep;
        
        [JsonIgnore] public Inventory Inventory => _inventory;

        [JsonIgnore]
        private List<GameStep> _cachedLocations;

        [JsonIgnore]
        public List<Ref<LocationEntity>> AllLocations => _allLocations;

        [JsonIgnore]
        public List<Ref<LocationEntity>> ExpeditionLocations => _expeditionLocations;

        [JsonIgnore]
        public LocationEntity ActiveLocation
        {
            get => _activeLocation;
            set => _activeLocation = value;
        }

#endregion


#region API

        public void SetGameStep(GameStep next)
        {
            _currentStep.Value?.OnStepComplete(this);
            _currentStep = next;
            _currentStep.Value?.OnStepStart(this);
        }

        public LocationEntity GetOrCreateLocation(LocationModel model)
        {
            var location = _allLocations.FirstOrDefault(loc => loc.Value.Model == model);
            if (!location)
            {
                location = (LocationEntity)model.Create();
                _allLocations.Add(location);
                Add(location);
            }
            return location;
        }

        public override void OnPostLoad()
        {
            base.OnPostLoad();
            
            Container.InjectAt(ActiveLocation);
            ActiveLocation.Load();
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

#endregion
    }
}