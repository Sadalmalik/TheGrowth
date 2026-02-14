using Newtonsoft.Json;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class Charges : IEntityModelComponent
    {
        public int Amound;
        public bool ShowOnCard;

        public void OnEntityCreated(CompositeEntity card)
        {
            var component = card.AddComponent<Component>();
            component.Charges = Amound;
            component.ShowOnCard = ShowOnCard;
        }

        public class Component : EntityComponent
        {
            //private EntityCardView _view;
            [JsonProperty]
            private int _charges;

            [JsonProperty]
            private bool _showOnCard;

            [JsonIgnore]
            public int Charges
            {
                get => _charges;
                set => _charges = value;
            }

            [JsonIgnore]
            public bool ShowOnCard
            {
                get => _showOnCard;
                set => _showOnCard = value;
            }
        }
    }
}