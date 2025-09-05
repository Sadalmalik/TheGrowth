using UnityEngine;

namespace XandArt.Architecture
{
    public interface IEntityView
    {
        public GameObject Object { get; }

        public Entity Data { get; internal set; }
    }
}