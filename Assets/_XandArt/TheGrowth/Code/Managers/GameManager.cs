using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class GameManager : SharedObject, ITickable
    {
        public const string LastSavePref = "last-save";

        [Inject]
        protected PersistenceManager PersistenceManager;

        public GameState CurrentGameState { get; private set; }

        public void Init()
        {
        }

        public void Dispose()
        {
        }

        public void Tick()
        {
            
        }

#region Game API

        public bool HasLastSave()
        {
            string lastSave = PlayerPrefs.GetString(LastSavePref, null);
            return !string.IsNullOrEmpty(lastSave);
        }
        
        public void StartNewGame()
        {
            Debug.Log("Game: StartNewGame");
            CurrentGameState = GameState.Create(RootConfig.Instance.startStep);
            CurrentGameState.OnPostLoad();
        }

        public bool TryLoadLastGame()
        {
            Debug.Log("Game: TryLoadLastGame");
            
            string lastSave = PlayerPrefs.GetString(LastSavePref, null);
            if (string.IsNullOrEmpty(lastSave)) return false;
            CurrentGameState = PersistenceManager.Load<GameState>(lastSave);
            return true;
        }

        public void LoadGame(string gameId)
        {
            Debug.Log("Game: LoadGame");
            
            PlayerPrefs.SetString(LastSavePref, gameId);
            CurrentGameState = PersistenceManager.Load<GameState>(gameId);
        }

        public void SaveGame(string gameId)
        {
            Debug.Log("Game: SaveGame");
            
            PlayerPrefs.SetString(LastSavePref, gameId);
            PersistenceManager.Save(gameId, CurrentGameState);
        }

#endregion

    }
}