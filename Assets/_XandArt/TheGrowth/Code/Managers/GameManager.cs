using System;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class GameManager : SharedObject, ITickable
    {
        public const string LastSavePref = "last-save";

        [Inject]
        private PersistenceManager _persistenceManager;

        [Inject]
        private LocationManager _locationManager;

        public GameState CurrentGameState { get; private set; }

        public event Action<GameState> OnGameStateWillBeUnload;
        public event Action<GameState> OnGameStateLoaded;
        
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
            _locationManager.LoadLocation(CurrentGameState.ActiveLocation);
        }

        public bool TryLoadLastGame()
        {
            Debug.Log("Game: TryLoadLastGame");
            
            string lastSave = PlayerPrefs.GetString(LastSavePref, null);
            if (string.IsNullOrEmpty(lastSave)) return false;
            CurrentGameState = _persistenceManager.Load<GameState>(lastSave);
            _locationManager.LoadLocation(CurrentGameState.ActiveLocation);
            return true;
        }

        public void LoadGame(string gameId)
        {
            Debug.Log("Game: LoadGame");
            
            PlayerPrefs.SetString(LastSavePref, gameId);
            CurrentGameState = _persistenceManager.Load<GameState>(gameId);
            _locationManager.LoadLocation(CurrentGameState.ActiveLocation);
        }

        public void SaveGame(string gameId)
        {
            Debug.Log("Game: SaveGame");
            
            PlayerPrefs.SetString(LastSavePref, gameId);
            _persistenceManager.Save(gameId, CurrentGameState);
        }

#endregion

    }
}