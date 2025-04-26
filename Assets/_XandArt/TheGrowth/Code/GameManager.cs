using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XandArt.TheGrowth
{
    public class GameManager : SerializedMonoBehaviour
    {
        [SerializeField]
        private GameObject _mainMenuScreen;
        
        [SerializeField]
        private LoadingScreen _loadingScreen;

        
        private IEnumerator Start()
        {
            Debug.Log("Started!");

            yield return null;
            Debug.Log("Do Some Loading!");
            yield return null;
            
            _loadingScreen.Hide();
        }

        private IEnumerator StartNewGameCoroutine()
        {
            var task = _loadingScreen.ShowAsync();
            yield return new WaitUntil(()=> task.IsCompleted);
            
            var operation = SceneManager.LoadSceneAsync("Game/Scenes/SampleScene", LoadSceneMode.Additive);
            yield return operation;
            
            Debug.Log("Scene loaded");
            
            _mainMenuScreen.SetActive(false);
            
            task = _loadingScreen.HideAsync();
            yield return new WaitUntil(()=> task.IsCompleted);

            CardManager.Instance.DealCards();
        }

#region API

        public void StartNewGame()
        {
            StartCoroutine(StartNewGameCoroutine());
        }

        public void ExitGame()
        {
            Application.Quit();
        }

#endregion
    }
}