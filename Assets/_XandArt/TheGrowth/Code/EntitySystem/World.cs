using System;
using System.Collections.Generic;

namespace XandArt.TheGrowth
{
    [Serializable]
    public class World
    {
        public List<Entity> Entities { get; private set; } = new List<Entity>();

        public virtual void OnPreSave()
        {
            foreach (var entity in Entities)
            {
                entity.OnPreSave();
            }
        }

        public virtual void OnPostLoad()
        {
            foreach (var entity in Entities)
            {
                entity.OnPostLoad();
            }
        }
    }
}