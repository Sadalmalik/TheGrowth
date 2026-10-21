using R3;
using TheGrowth.Architecture.MVVM;
using TheGrowth.UI.Root;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheGrowth.UI.Saves
{
    /// <summary>Окно слотов: заголовок по режиму, «Новое сохранение» (режим сохранения), список строк, «Назад».</summary>
    public sealed class SaveLoadView : WindowView<SaveLoadViewModel>
    {
        [SerializeField]
        private TMP_Text m_Title;

        [SerializeField]
        private Button m_NewSaveButton;

        [SerializeField]
        private Button m_BackButton;

        [SerializeField]
        private GameObject m_EmptyLabel;

        [SerializeField]
        private RectTransform m_SlotsContainer;

        [SerializeField]
        private SaveSlotView m_SlotPrefab;

        private WidgetList<SaveSlotViewModel, SaveSlotView> m_Slots;

        /// <summary>Строки в порядке слотов (тесты).</summary>
        public WidgetList<SaveSlotViewModel, SaveSlotView> Slots => m_Slots;

        public Button NewSaveButton => m_NewSaveButton;

        public Button BackButton => m_BackButton;

        public override Selectable FirstSelected
        {
            get
            {
                if (IsSelectable(m_NewSaveButton))
                    return m_NewSaveButton;

                if (m_Slots != null)
                {
                    foreach (var slot in m_Slots.Views)
                    {
                        if (IsSelectable(slot.LoadButton))
                            return slot.LoadButton;

                        if (IsSelectable(slot.OverwriteButton))
                            return slot.OverwriteButton;
                    }
                }

                return IsSelectable(m_BackButton) ? m_BackButton : base.FirstSelected;
            }
        }

        protected override void OnBind()
        {
            m_Title.text = Source.Title;
            m_NewSaveButton.gameObject.SetActive(Source.CanSave);
            m_Slots ??= new WidgetList<SaveSlotViewModel, SaveSlotView>(m_SlotPrefab, m_SlotsContainer);
            m_Slots.Bind(Source.Slots);

            m_NewSaveButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.SaveNew()).AddTo(this);
            m_BackButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.Close()).AddTo(this);
            Source.IsEmpty.Subscribe(this, static (empty, view) => view.m_EmptyLabel.SetActive(empty)).AddTo(this);
            Source.IsBusy.Subscribe(this, static (busy, view) => view.m_NewSaveButton.interactable = !busy).AddTo(this);
        }

        protected override void OnUnbind()
        {
            m_Slots?.Unbind();
        }
    }
}
