using System;
using Newtonsoft.Json;

namespace XandArt.TheGrowth
{
    [Serializable]
    public class Entity
    {
        [JsonIgnore]
        public EntityView View;
        
        public virtual void OnPreSave() {}
        public virtual void OnPostLoad() {}
    }
}