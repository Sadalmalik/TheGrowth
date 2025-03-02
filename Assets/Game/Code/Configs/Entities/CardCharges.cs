using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class ChargesComponent : ICardComponent
    {
        public int Charges;
        
        public void OnEntityCreated(EntityCard card)
        {
            var component = card.gameObject.AddComponent<CardCharges>();
            component.charges = Charges;
        }
    }

    public class CardCharges : MonoBehaviour
    {
        public int charges;
    }
}