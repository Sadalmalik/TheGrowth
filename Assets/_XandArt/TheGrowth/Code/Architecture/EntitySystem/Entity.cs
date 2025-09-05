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

        [JsonIgnore]
        public AbstractEntityModel Model => _model;
        
        [JsonIgnore]
        public IEntityView View { get; private set; }
        
        public Entity(bool makeGuid = true)
        {
            if (makeGuid)
                Guid = Guid.NewGuid();
        }

        public void Init()
        {
            OnInit();
            OnInitOrPostLoad();
        }

        public void SetView(IEntityView view)
        {
            View = view;
            View.Data = this;
        }

        public virtual void OnInit() {}
        public virtual void OnDestroy() {}
        public virtual void OnPreSave() {}
        public virtual void OnPostLoad() {}
        public virtual void OnInitOrPostLoad() {}
        

        public override string ToString()
        {
            return $"{Model?.name ?? GetType().Name}#{Guid}";
        }
    }
}