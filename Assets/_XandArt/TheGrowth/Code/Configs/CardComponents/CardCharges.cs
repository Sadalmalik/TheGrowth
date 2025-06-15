using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class CardChargesComponent : IEntityModelComponent
    {
        public int Charges;
        
        public void OnEntityCreated(CompositeEntity card)
        {
            var component = card.AddComponent<CardCharges>();
            component.Charges = Charges;
        }
    }

    public class CardCharges : EntityComponent
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
            // _view = GetComponent<CardView>();
            // _view?.rightText?.SetText(_charges.ToString());
        }
    }
}