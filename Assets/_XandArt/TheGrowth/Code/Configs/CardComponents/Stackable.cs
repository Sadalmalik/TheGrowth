using Newtonsoft.Json;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class Stackable : IEntityModelComponent
    {
        public int Limit;
        
        public void OnEntityCreated(CompositeEntity card)
        {
            var component = card.AddComponent<Component>();
            component._count = 1;
            component._limit = Limit;
        }

        public class Component : EntityComponent
        {
            [JsonProperty]
            internal int _count;
            
            [JsonProperty]
            internal int _limit;
            
            [JsonIgnore]
            public int Count
            {
                get => _count;
                set
                {
                    Debug.Log($"TEST - Set Stack {_count} -> {value} for {Owner}");
                    _count = value;
                }
            }

            [JsonIgnore]
            public int Limit => _limit;

            [JsonIgnore]
            public int Space => _limit - _count;
        }
    }

    public static class StackExtension
    {
        public static bool TransferTo(this CompositeEntity from, CompositeEntity to)
        {
            var stackFrom = from.GetComponent<Stackable.Component>();
            var stackInto = to.GetComponent<Stackable.Component>();

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
        
        public static bool TransferTo(this Stackable.Component from, Stackable.Component to)
        {
            var space = to.Space;
            if (from.Count < space)
            {
                to.Count += from.Count;
                from.Count = 0;
                return true;
            }

            to.Count += space;
            from.Count -= space;
            return false;
        }
    }
}