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
            CurrentGameState = GameState.Create(RootConfig.Instance.startStep);
            container.InjectAt(CurrentGameState);
            Game.BaseContext.GetRequired<GlobalData>().currentState = CurrentGameState;
            CurrentGameState.OnPostLoad();
            CurrentGameState.Start();
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