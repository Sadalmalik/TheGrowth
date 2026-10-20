using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XandArt.TheGrowth
{
    public class UIButton : SerializedMonoBehaviour
    {
        [SerializeField]
        private TMP_Text label;

        [SerializeField]
        private Button button;

        public event Action<UIButton> OnClick;

        public string Title
        {
            get => label.text;
            set => label.SetText(value);
        }
        
        private void Awake()
        {
            button.onClick.AddListener(OnClickHandler);
        }

        private void OnClickHandler()
        {
            OnClick?.Invoke(this);
        }
    }
}