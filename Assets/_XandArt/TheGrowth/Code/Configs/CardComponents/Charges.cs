using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class Charges : IEntityModelComponent
    {
        public int Amound;
        
        public void OnEntityCreated(CompositeEntity card)
        {
            var component = card.AddComponent<Component>();
            component.Charges = Amound;
        }
        
        public class Component : EntityComponent
        {
            private CardView _view;
            private int _charges;
            
            public int Charges
            {
                get => _charges;
                set => _charges = value;
            }

            // private void OnEnable()
            // {
            //     _view = GetComponent<CardView>();
            //     _view?.rightText?.SetText(_charges.ToString());
            // }
        }
    }
}