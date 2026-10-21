using R3;
using TheGrowth.Architecture.MVVM;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheGrowth.UI.Saves
{
    /// <summary>Строка слота: превью, дата, описание, кнопки «Загрузить», «Перезаписать», «Удалить» по режиму окна.</summary>
    public sealed class SaveSlotView : View<SaveSlotViewModel>
    {
        [SerializeField]
        private RawImage m_Thumbnail;

        [SerializeField]
        private TMP_Text m_Title;

        [SerializeField]
        private TMP_Text m_Details;

        [SerializeField]
        private Button m_LoadButton;

        [SerializeField]
        private Button m_OverwriteButton;

        [SerializeField]
        private Button m_DeleteButton;

        public Button LoadButton => m_LoadButton;

        public Button OverwriteButton => m_OverwriteButton;

        public Button DeleteButton => m_DeleteButton;

        protected override void OnBind()
        {
            m_Title.text = Source.Title;
            m_Details.text = Source.Details;
            m_LoadButton.gameObject.SetActive(Source.CanLoad);
            m_OverwriteButton.gameObject.SetActive(Source.CanOverwrite);

            m_LoadButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.Load()).AddTo(this);
            m_OverwriteButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.Overwrite()).AddTo(this);
            m_DeleteButton.OnClickAsObservable().Subscribe(this, static (_, view) => view.Source.Delete()).AddTo(this);
            Source.Thumbnail.Subscribe(this, static (texture, view) =>
            {
                view.m_Thumbnail.texture = texture;
                view.m_Thumbnail.color = texture != null ? Color.white : new Color(1f, 1f, 1f, 0.08f);
            }).AddTo(this);
        }

        protected override void OnUnbind()
        {
            m_Thumbnail.texture = null;
        }
    }
}
