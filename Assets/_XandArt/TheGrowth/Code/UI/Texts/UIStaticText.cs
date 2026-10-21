using TMPro;
using UnityEngine;

namespace TheGrowth.UI.Texts
{
    /// <summary>Статическая надпись префаба по ключу <see cref="UITexts"/>: текст ставится при создании объекта.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class UIStaticText : MonoBehaviour
    {
        [SerializeField]
        private string m_Key;

        public string Key => m_Key;

        /// <summary>Задать ключ и сразу текст (сборка префабов в редакторе).</summary>
        public void SetKey(string key)
        {
            m_Key = key;
            Apply();
        }

        public void Apply()
        {
            if (TryGetComponent<TMP_Text>(out var text))
                text.text = UITexts.Get(m_Key);
        }

        private void Awake()
        {
            Apply();
        }
    }
}
