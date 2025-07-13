using System;
using Newtonsoft.Json;

namespace XandArt.Architecture
{
    [Serializable]
    public class Entity
    {
        [JsonProperty(Order = -1000, PropertyName = "$guid")]
        public Guid Guid { get; internal set; } = Guid.Empty;

        [JsonProperty]
        internal AssetRef<AbstractEntityModel> _model;
        
        public Entity(bool makeGuid = true)
        {
            if (makeGuid)
                Guid = Guid.NewGuid();
        }

        public virtual void OnInit() {}
        public virtual void OnPreSave() {}
        public virtual void OnPostLoad() {}
    }
}