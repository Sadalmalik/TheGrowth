using System;
using System.Collections.Generic;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public class LoadingMenu : WidgetBase
    {
        [SerializeField]
        private Transform root;
        
        [SerializeField]
        private UIButton buttonPrefab;

        private List<UIButton> _buttons;
        
        public event Action<string> OnSaveSelected;

        public void SetSaves(List<string> saves)
        {
            _buttons ??= new List<UIButton>();

            var i = 0;
            var savesCount = saves.Count;
            UIButton button;
            for (; i < savesCount; i++)
            {
                if (_buttons.Count <= i)
                    _buttons.Add(button = CreateButton());
                else
                    button = _buttons[i];

                button.gameObject.SetActive(true);
                button.Title = saves[i];
            }

            var buttonsCount = _buttons.Count;
            for (; i < buttonsCount; i++)
            {
                _buttons[i].gameObject.SetActive(false);
            }
        }

        private UIButton CreateButton()
        {
            var button = Instantiate(buttonPrefab, root);
            button.OnClick += ClickHandler;
            return button;
        }

        private void ClickHandler(UIButton button)
        {
            OnSaveSelected?.Invoke(button.Title);
        }
    }
}