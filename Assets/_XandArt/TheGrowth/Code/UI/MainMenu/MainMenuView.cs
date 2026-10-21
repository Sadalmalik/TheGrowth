using R3;
using TheGrowth.Architecture.MVVM;
using TheGrowth.UI.Root;
using UnityEngine;
using UnityEngine.UI;

namespace TheGrowth.UI.MainMenu
{
    /// <summary>Главное меню: «Продолжить» видна, если есть слот; фокус — на ней, иначе на «Новая игра».</summary>
    public sealed class MainMenuView : WindowView<MainMenuViewModel>
    {
        [SerializeField]
        private Button m_ContinueButton;

        [SerializeField]
        private Button m_NewGameButton;

        [SerializeField]
        private Button m_LoadButton;

        [SerializeField]
        private Button m_QuitButton;

        public Button ContinueButton => m_ContinueButton;

        public Button NewGameButton => m_NewGameButton;

        public Button LoadButton => m_LoadButton;

        public Button QuitButton => m_QuitButton;

        public override Selectable FirstSelected => IsSelectable(m_ContinueButton) ? m_ContinueButton : m_NewGameButton;

        protected override void OnBind()
        {
            m_ContinueButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.Continue()).AddTo(this);
            m_NewGameButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.NewGame()).AddTo(this);
            m_LoadButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.OpenLoad()).AddTo(this);
            m_QuitButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.Quit()).AddTo(this);
            Source.CanContinue.Subscribe(this, static (canContinue, view) => view.m_ContinueButton.gameObject.SetActive(canContinue)).AddTo(this);
        }
    }
}
