using TheGrowth.Architecture.MVVM;
using TheGrowth.UI.Root;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheGrowth.UI.Popups
{
    /// <summary>Всплывающее окно: сообщение и кнопки (список виджетов); фокус — на кнопке отмены.</summary>
    public sealed class PopupView : WindowView<PopupViewModel>
    {
        [SerializeField]
        private TMP_Text m_Message;

        [SerializeField]
        private RectTransform m_ButtonsContainer;

        [SerializeField]
        private PopupButtonView m_ButtonPrefab;

        private WidgetList<PopupButton, PopupButtonView> m_Buttons;

        public override Selectable FirstSelected
        {
            get
            {
                if (m_Buttons != null)
                {
                    foreach (var button in m_Buttons.Views)
                    {
                        if (button.Source is { IsCancel: true } && IsSelectable(button.Button))
                            return button.Button;
                    }
                }

                return base.FirstSelected;
            }
        }

        /// <summary>Вьюхи кнопок в порядке кнопок (тесты).</summary>
        public WidgetList<PopupButton, PopupButtonView> Buttons => m_Buttons;

        protected override void OnBind()
        {
            m_Message.text = Source.Message;
            m_Buttons ??= new WidgetList<PopupButton, PopupButtonView>(m_ButtonPrefab, m_ButtonsContainer);
            m_Buttons.Bind(Source.Buttons);
        }

        protected override void OnUnbind()
        {
            m_Buttons?.Unbind();
        }
    }
}
