using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XandArt.Architecture;
using XandArt.Architecture.IOC;

namespace XandArt.TheGrowth
{
    public class MenuManager : SerializedMonoBehaviour, IShared
    {
        [SerializeField]
        private GameObject _mainMenuScreen;

        [SerializeField]
        private GameObject _continueGameButton;

        [SerializeField]
        private LoadingMenu _loadingMenu;

        [SerializeField]
        private InputField _saveName;

        [SerializeField]
        private LoadingScreen _loadingScreen;

        [Inject]
        private PersistenceManager _persistenceManager;
        [Inject]
        private GameManager _gameManager;

        public void Init()
        {
            _continueGameButton.SetActive(_gameManager.HasLastSave());
            _loadingMenu.OnSaveSelected += HandleSaveSelected;
            _loadingScreen.Hide();
        }

        public void Dispose()
        {
        }

        private void HandleSaveSelected(string save)
        {
            _gameManager.LoadGame(save);
        }

#region API

        public void ContinueGame()
        {
            _gameManager.TryLoadLastGame();
        }

        public void StartNewGame()
        {
            _gameManager.StartNewGame();
            //StartCoroutine(StartNewGameCoroutine());
        }

        public void UpdateLoadingMenu()
        {
            _loadingMenu.SetSaves(_persistenceManager.GetAllSaves());
        }

        public void ExitGame()
        {
            Application.Quit();
        }

#endregion


#region Legacy

        private IEnumerator StartNewGameCoroutine()
        {
            var task = _loadingScreen.ShowAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            var operation = SceneManager.LoadSceneAsync("Game/Scenes/SampleScene", LoadSceneMode.Additive);
            yield return operation;

            Debug.Log("Scene loaded");

            _mainMenuScreen.SetActive(false);

            task = _loadingScreen.HideAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            CardManager.Instance.DealCards();
        }

#endregion
    }
}