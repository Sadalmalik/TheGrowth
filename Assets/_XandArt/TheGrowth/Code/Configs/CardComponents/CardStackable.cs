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

            public int Limit => _limit;

            public int Space => _limit - _count;
        }
    }

    public static class StackExtension
    {
        public static bool TransferTo(this CompositeEntity from, CompositeEntity to)
        {
            var stackFrom = from.GetComponent<CardStackable.Component>();
            var stackInto = to.GetComponent<CardStackable.Component>();

            var space = stackInto.Space;
            if (stackFrom.Count < space)
            {
                stackInto.Count += stackFrom.Count;
                stackFrom.Count = 0;
                return true;
            }

            stackInto.Count += space;
            stackFrom.Count -= space;
            return false;
        }
    }
}