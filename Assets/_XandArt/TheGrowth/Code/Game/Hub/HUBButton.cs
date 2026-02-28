using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XandArt.TheGrowth
{
    public class HUBButton : WidgetBase
    {
        [SerializeField]
        private Button _button;

        [SerializeField]
        private HoldButton _holdButton;

        [SerializeField]
        private HUBRoom _gotoRoom;

        [SerializeField]
        private List<Command> OnClickedActions;

        private HUB _hub;

        public Button Button => _button;

        public event Action<HUBButton> OnClicked;

        public override void Init()
        {
            if (_button)
                _button.onClick.AddListener(HandleClick);
            if (_holdButton)
                _holdButton.onClick.AddListener(HandleClick);
            _hub = GetComponentInParent<HUB>();
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(this);

            OnClickedActions.ExecuteAll(Game.BaseContext);

            if (_gotoRoom != null)
                _hub.GoToRoom(_gotoRoom);
        }
    }
}