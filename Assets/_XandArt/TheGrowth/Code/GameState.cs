using Newtonsoft.Json;

namespace XandArt.TheGrowth
{
    public class GameState : PersistentState
    {
        [JsonProperty] private Ref<GlobalState> _gameState;

        [JsonIgnore] public GlobalState GlobalState => _gameState;

        public static GameState Create()
        {
            var state = new GameState
            {
                _gameState = new GlobalState()
            };
            state.Add(state._gameState);
            return state;
        }

        private GameState()
        {
        }
    }
}