using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class CardStackable : IEntityModelComponent
    {
        public int Initial;
        public int Limit;
        
        public void OnEntityCreated(CompositeEntity card)
        {
            var component = card.AddComponent<Component>();
            component._count = Initial;
            component._limit = Limit;
        }

        public class Component : EntityComponent
        {
            internal int _count;
            internal int _limit;
            
            public int Count
            {
                get => _count;
                set => _count = value;
            }
        }
    }
}