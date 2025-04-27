using System;
using Newtonsoft.Json;

namespace XandArt.TheGrowth
{
    [Serializable]
    public class Entity
    {
        [JsonProperty(Order = -1000, PropertyName = "$guid")]
        public Guid Guid { get; internal set; } = Guid.Empty;

        public Entity(bool makeGuid = true)
        {
            if (makeGuid)
                Guid = Guid.NewGuid();
        }

        public virtual void OnPreSave() {}
        public virtual void OnPostLoad() {}
    }
}