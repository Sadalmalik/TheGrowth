using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XandArt.TheGrowth
{
    public class HoldButton : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler
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

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log("Button Pressed Down");
            _isHolding = true;
            _isBlocked = false;
            _progress = 0f;
            _progressFill.fillAmount = 0;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log("Button Released");
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
                    t = 1;
                    Evaluate();
                }

                _progressFill.fillAmount = t;
            }
        }

        private void Evaluate()
        {
            Debug.Log("Button Evaluate");
            
            if (_block)
                _isBlocked = true;
            else
                _progress = 0;
            
            UISystemProfilerApi.AddMarker("HoldButton.onClick", this);
            m_OnClick.Invoke();
        }
    }
}