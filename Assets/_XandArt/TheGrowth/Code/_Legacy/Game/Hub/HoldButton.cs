using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XandArt.TheGrowth
{
    public class HoldButton : Selectable
    {
        [SerializeField]
        private float _duration = 0.25f;

        [SerializeField]
        private bool _block = true; 

        [SerializeField]
        private Image _progressFill; 
        
        // Event delegates triggered on click.
        [SerializeField]
        private Button.ButtonClickedEvent m_OnClick = new Button.ButtonClickedEvent();
        
        public Button.ButtonClickedEvent onClick
        {
            get { return m_OnClick; }
            set { m_OnClick = value; }
        }
        
        private bool _isHolding = false;
        private bool _isBlocked = false; 
        private float _progress = 0f;

        public void OnDisable()
        {
            _isHolding = false;
            _isBlocked = false;
            _progress = 0f;
            _progressFill.fillAmount = 0;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable)
            {
                return;
            }
            
            _isHolding = true;
            _isBlocked = false;
            _progress = 0f;
            _progressFill.fillAmount = 0;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (!interactable)
            {
                return;
            }
            
            _isHolding = false;
            _isBlocked = false;
            _progress = 0f;
            _progressFill.fillAmount = 0;
        }

        void Update()
        {
            if (_isHolding && !_isBlocked)
            {
                _progress += Time.deltaTime;
                var t = _progress / _duration;

                if (t > 1)
                {
                    _progressFill.fillAmount = 1;
                    Evaluate();
                }
                else
                {
                    _progressFill.fillAmount = t;
                }
            }
        }

        private void Evaluate()
        {
            if (_block)
                _isBlocked = true;
            else
                _progress = 0;
            
            UISystemProfilerApi.AddMarker("HoldButton.onClick", this);
            m_OnClick.Invoke();

            if (!interactable)
            {
                _isHolding = false;
                _isBlocked = false;
                _progress = 0f;
                _progressFill.fillAmount = 0;
            }
        }
    }
}