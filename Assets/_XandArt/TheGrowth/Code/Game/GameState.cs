using Newtonsoft.Json;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class GameState : PersistentState
    {
        [JsonProperty] private AssetRef<StoryStep> _currentStep;
        [JsonProperty] private Ref<Inventory> _inventory;

        [JsonIgnore] public StoryStep CurrentStoryStep => _currentStep;

        private GameState() { }
        
        public static GameState Create(StoryStep start)
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