using Newtonsoft.Json;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class GameState : PersistentState
    {
        [JsonProperty] private AssetRef<GameStep> _currentStep;
        [JsonProperty] private Ref<Inventory> _inventory;

        [JsonIgnore]
        public GameStep CurrentGameStep
        {
            get => _currentStep;
            set => _currentStep = value;
        }
        
        [JsonIgnore] public Inventory Inventory => _inventory;
        
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
            return state;
        }
    }
}