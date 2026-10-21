using R3;
using TheGrowth.Architecture.MVVM;
using TheGrowth.UI.Root;
using UnityEngine;
using UnityEngine.UI;

namespace TheGrowth.UI.Pause
{
    /// <summary>Пауза: кнопки по VM; фокус — на «Продолжить».</summary>
    public sealed class PauseView : WindowView<PauseViewModel>
    {
        [SerializeField]
        private Button m_ResumeButton;

        [SerializeField]
        private Button m_SaveButton;

        [SerializeField]
        private Button m_LoadButton;

        [SerializeField]
        private Button m_ExitToMenuButton;

        [SerializeField]
        private Button m_QuitButton;

        public Button ResumeButton => m_ResumeButton;

        public Button SaveButton => m_SaveButton;

        public Button LoadButton => m_LoadButton;

        public Button ExitToMenuButton => m_ExitToMenuButton;

        public Button QuitButton => m_QuitButton;

        protected override void OnBind()
        {
            m_ResumeButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.Resume()).AddTo(this);
            m_SaveButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.OpenSave()).AddTo(this);
            m_LoadButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.OpenLoad()).AddTo(this);
            m_ExitToMenuButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.ExitToMenu()).AddTo(this);
            m_QuitButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.Quit()).AddTo(this);
        }
    }
}
