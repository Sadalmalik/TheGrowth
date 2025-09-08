using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private List<Ref<Location>> _allLocations = new List<Ref<Location>>();

        [JsonProperty]
        private List<Ref<Location>> _expeditionLocations = new List<Ref<Location>>();

        [JsonProperty]
        private Ref<Location> _activeLocation;

#endregion


#region Properties

        [JsonIgnore]
        [Inject]
        public Container Container;

        [JsonIgnore]
        [Inject]
        private LocationManager _locationManager;

        // Access
        [JsonIgnore]
        public GameStep CurrentGameStep => _currentStep;

        [JsonIgnore]
        private List<GameStep> _cachedLocations;

        [JsonIgnore]
        public List<Ref<Location>> AllLocations => _allLocations;

        [JsonIgnore]
        public List<Ref<Location>> ExpeditionLocations => _expeditionLocations;

        [JsonIgnore]
        public Location ActiveLocation
        {
            get => _activeLocation;
            set => _activeLocation = value;
        }

        [JsonIgnore]
        public BoardEntity ActiveBoard => ActiveLocation.Board;

#endregion


#region API

        private GameState()
        {
        }

        public static GameState Create(GameStep start)
        {
            // Game structure preset
            var state = new GameState
            {
                _currentStep = start
            };
            return state;
        }

        public void Start()
        {
            CurrentGameStep.OnStepStart(this);
        }

        public Inventory GetInventory(InventoryModel model)
        {
            var inventory = (Inventory) GetAll<Inventory>().FirstOrDefault(inv => inv.Model == model);
            if (inventory == null) throw new Exception($"Inventory '{model}' not exist in game state!");
            return inventory;
        }

        public void SetGameStep(GameStep next)
        {
            _currentStep.Value?.OnStepComplete(this);
            _currentStep = next;
            _currentStep.Value?.OnStepStart(this);
        }

        public Location GetOrCreateLocation(LocationModel model)
        {
            var location = _allLocations.FirstOrDefault(loc => loc.Value.Model == model);
            if (!location)
            {
                location = (Location)model.Create();
                _allLocations.Add(location);
                Add(location);
            }

            Container.InjectAt(location.Value);
            return location;
        }

        public async void GotoLocation(LocationModel model)
        {
            var nextLocation = GetOrCreateLocation(model);
            await Task.WhenAll(
                _locationManager.UnloadLocation(ActiveLocation),
                _locationManager.LoadLocation(nextLocation));
            ActiveLocation = nextLocation;
        }

        public override void OnPostLoad()
        {
            base.OnPostLoad();

            foreach (var location in AllLocations)
                Container.InjectAt(location.Value);

            _ = _locationManager.LoadLocation(ActiveLocation);
        }

#endregion
    }
}