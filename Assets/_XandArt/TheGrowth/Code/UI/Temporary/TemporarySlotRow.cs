using System;
using UnityEngine;
using UnityEngine.UI;

namespace TheGrowth.UI.Temporary
{
    /// <summary>ВРЕМЕННО (до UI на MVVM, E08): строка списка слотов <see cref="TemporaryMenu"/> — описание, «Загрузить», «Удалить».</summary>
    public sealed class TemporarySlotRow : MonoBehaviour
    {
        [SerializeField]
        private Text m_Label;

        [SerializeField]
        private Button m_LoadButton;

        [SerializeField]
        private Button m_DeleteButton;

        public void Bind(string label, bool canLoad, Action load, Action delete)
        {
            if (m_Label != null)
                m_Label.text = label;

            if (m_LoadButton != null)
            {
                m_LoadButton.interactable = canLoad;
                m_LoadButton.onClick.RemoveAllListeners();
                m_LoadButton.onClick.AddListener(() => load?.Invoke());
            }

            if (m_DeleteButton != null)
            {
                m_DeleteButton.onClick.RemoveAllListeners();
                m_DeleteButton.onClick.AddListener(() => delete?.Invoke());
            }
        }
    }
}
