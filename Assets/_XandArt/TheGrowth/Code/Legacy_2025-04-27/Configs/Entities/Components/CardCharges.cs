using System;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public class ChargesComponentModel : IEntityComponentModel
    {
        public int Charges;
        
        public void OnEntityCreated(EntityCard card)
        {
            var component = card.gameObject.AddComponent<CardCharges>();
            component.Charges = Charges;
        }
    }

    public class CardCharges : MonoBehaviour
    {
        private CardView _view;
        private int _charges;
        
        public int Charges
        {
            get => _charges;
            set
            {
                _charges = value;
                _view?.rightText?.SetText(_charges.ToString());
            }
        }

        private void OnEnable()
        {
            _view = GetComponent<CardView>();
            _view?.rightText?.SetText(_charges.ToString());
        }
    }
}