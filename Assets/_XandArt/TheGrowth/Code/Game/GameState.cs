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
        private Ref<Inventory> _inventory;

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

        // Access
        [JsonIgnore]
        public GameStep CurrentGameStep => _currentStep;

        [JsonIgnore]
        public Inventory Inventory => _inventory;

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
        public EntityBoard ActiveBoard => ActiveLocation.Board;

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
                _currentStep = start,
                _inventory = new Inventory()
            };
            state.Add(state._inventory);
            return state;
        }

        public void Start()
        {
            CurrentGameStep.OnStepStart(this);
        }

        public void SetGameStep(GameStep next)
        {
            _currentStep.Value?.OnStepComplete(this);
            _currentStep = next;
            _currentStep.Value?.OnStepStart(this);
        }

        public T Create<T>() where T : Entity, new()
        {
            var entity = new T();
            Add(entity);
            return entity;
        }

        public Entity Create(EntityModel model)
        {
            var entity = model.Create();
            Add(entity);
            return entity;
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

            if (ActiveLocation != null)
                await ActiveLocation.Unload();

            if (nextLocation != null)
                await nextLocation.Load();

            ActiveLocation = nextLocation;
            // var task1 = ActiveLocation.Unload();
            // var task2 = nextLocation.Load();

            //await Task.WhenAll(task1, task2);
        }

        public override void OnPostLoad()
        {
            base.OnPostLoad();

            foreach (var location in AllLocations)
                Container.InjectAt(location.Value);

            ActiveLocation?.Load();
        }

#endregion
    }
}