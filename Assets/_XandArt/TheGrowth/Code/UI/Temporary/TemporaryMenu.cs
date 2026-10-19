using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TheGrowth.UI.Temporary
{
    /// <summary>
    /// ВРЕМЕННО (до UI на MVVM, E08): главное меню («Новая игра», «Выход») и кнопка «В меню» в локации. Про
    /// <c>GameFlow</c> не знает: события кнопок и переключение панелей по состоянию связывает <c>GameStarter</c>.
    /// </summary>
    public sealed class TemporaryMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_MainMenuPanel;

        [SerializeField]
        private Button m_NewGameButton;

        [SerializeField]
        private Button m_QuitButton;

        [SerializeField]
        private GameObject m_LocationPanel;

        [SerializeField]
        private Button m_ExitToMenuButton;

        public event Action NewGameClicked;

        public event Action QuitClicked;

        public event Action ExitToMenuClicked;

        public void Show(bool mainMenu, bool location)
        {
            if (m_MainMenuPanel != null)
                m_MainMenuPanel.SetActive(mainMenu);

            if (m_LocationPanel != null)
                m_LocationPanel.SetActive(location);
        }

        private void Awake()
        {
            Show(false, false);
            Listen(m_NewGameButton, HandleNewGame, true);
            Listen(m_QuitButton, HandleQuit, true);
            Listen(m_ExitToMenuButton, HandleExitToMenu, true);
        }

        private void OnDestroy()
        {
            Listen(m_NewGameButton, HandleNewGame, false);
            Listen(m_QuitButton, HandleQuit, false);
            Listen(m_ExitToMenuButton, HandleExitToMenu, false);
        }

        private static void Listen(Button button, UnityAction handler, bool subscribe)
        {
            if (button == null)
                return;

            if (subscribe)
                button.onClick.AddListener(handler);
            else
                button.onClick.RemoveListener(handler);
        }

        private void HandleNewGame()
        {
            NewGameClicked?.Invoke();
        }

        private void HandleQuit()
        {
            QuitClicked?.Invoke();
        }

        private void HandleExitToMenu()
        {
            ExitToMenuClicked?.Invoke();
        }
    }
}
