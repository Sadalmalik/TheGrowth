using UnityEditor;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public static class Game
    {
        private static Container _container;
        private static PersistenceManager _persistenceManager;

        public static Container Container
            => _container;

        public static GameState CurrentGameState;
        
        public static void Initialize()
        {
            _container = new Container();

            _persistenceManager = _container.Add<PersistenceManager>();
            
            
            _container.Init();
        }

        public static void StartNewGame()
        {
            CurrentGameState = GameState.Create(RootConfig.Instance.startStep);
        }

        public static void LoadGame(string gameId)
        {
            CurrentGameState = _persistenceManager.Load<GameState>(gameId);
        }

        public static void SaveGame(string gameId)
        {
            _persistenceManager.Save(gameId, CurrentGameState);
        }

        [MenuItem("[TEST]/Do Tesst")]
        public static void Test()
        {
            Initialize();
            StartNewGame();
            
            CurrentGameState.Add(new CardEntity());
            CurrentGameState.Add(new CardEntity());

            var card = new CardEntity{HueMoe="lololo"};
            CurrentGameState.Inventory.Add(card);
            CurrentGameState.Add(card);
            
            card = new CardEntity{HueMoe="vilka"};
            CurrentGameState.Inventory.Add(card);
            CurrentGameState.Add(card);
            
            card = new CardEntity{HueMoe="fdghfgsjhefghdjf"};
            CurrentGameState.Inventory.Add(card);
            CurrentGameState.Add(card);

            SaveGame("AAA");
            _container.Dispose();
        }
    }
}