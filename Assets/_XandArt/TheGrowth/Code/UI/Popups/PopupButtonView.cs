using R3;
using TheGrowth.Architecture.MVVM;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheGrowth.UI.Popups
{
    /// <summary>Кнопка всплывающего окна: надпись и нажатие. Вьюха данных (<see cref="PopupButton"/>), не VM.</summary>
    public sealed class PopupButtonView : View<PopupButton>
    {
        [SerializeField]
        private Button m_Button;

        [SerializeField]
        private TMP_Text m_Label;

        public Button Button => m_Button;

        protected override void OnBind()
        {
            m_Label.text = Source.Label;
            m_Button.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.Click()).AddTo(this);
        }
    }
}
