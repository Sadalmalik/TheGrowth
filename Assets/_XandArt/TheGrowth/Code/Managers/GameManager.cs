using System;
using UnityEngine;
using XandArt.Architecture;
using XandArt.Architecture.Events;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public struct GameLoadedEvent
    {
        public GameState GameState;
    }
    
    public class GameManager : SharedObject, ITickable
    {
        public const string LastSavePref = "last-save";

        [Inject]
        private PersistenceManager _persistenceManager;

        [Inject]
        private LocationManager _locationManager;

        public GameState CurrentGameState { get; private set; }

        public Camera MainCamera { get; private set; }

        public event Action<GameState> OnGameStateWillBeUnload;
        public event Action<GameState> OnGameStateLoaded;

        public GameManager(Camera mainCamera)
        {
            MainCamera = mainCamera;
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

        public void SaveAndExit()
        {
            Debug.Log("Game: SaveAndExit");

            PlayerPrefs.SetString(LastSavePref, "last_game");
            _persistenceManager.Save("last_game", CurrentGameState);
            _ = _locationManager.UnloadLocation(CurrentGameState.ActiveLocation);
            CurrentGameState = null;

            Game.BaseContext.GetRequired<GlobalData>().currentState = null;
        }

        public void StartNewGame()
        {
            Debug.Log("Game: StartNewGame");
            CurrentGameState = GameState.Create();
            container.InjectAt(CurrentGameState);
            Game.BaseContext.GetRequired<GlobalData>().currentState = CurrentGameState;
            CurrentGameState.OnPostLoad();
            CurrentGameState.Start(RootConfig.Instance.startStep);
            
            EventBus.Global.Invoke(new GameLoadedEvent
            {
                GameState = CurrentGameState
            });
        }

        public bool TryLoadLastGame()
        {
            Debug.Log("Game: TryLoadLastGame");

            string lastSave = PlayerPrefs.GetString(LastSavePref, null);
            if (string.IsNullOrEmpty(lastSave)) return false;
            CurrentGameState = _persistenceManager.Load<GameState>(lastSave);
            container.InjectAt(CurrentGameState);
            Game.BaseContext.GetRequired<GlobalData>().currentState = CurrentGameState;
            _locationManager.LoadLocation(CurrentGameState.ActiveLocation);
            return true;
        }

        public void LoadGame(string gameId)
        {
            Debug.Log("Game: LoadGame");

            PlayerPrefs.SetString(LastSavePref, gameId);
            CurrentGameState = _persistenceManager.Load<GameState>(gameId);
            container.InjectAt(CurrentGameState);
            Game.BaseContext.GetRequired<GlobalData>().currentState = CurrentGameState;
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