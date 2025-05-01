using System;
using Unity.Plastic.Newtonsoft.Json;

namespace XandArt.TheGrowth
{
    /// <summary>
    /// Reference to entity.
    /// It allows effectytive serialization for game state.
    /// Use it in all serialized data for referencing Entities from PersistentState.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    [Serializable]
    public struct Ref<T> where T : Entity
    {
        // Protection from renaming
        public const string GuidPropertyName = nameof(Guid);
        public const string ValuePropertyName = nameof(Value);

        [JsonIgnore]
        private Guid _guid;
        
        [JsonIgnore]
        private T _value;

        [JsonIgnore]
        public Guid Guid
        {
            get => _guid;
            set
            {
                if (_guid != value)
                {
                    _guid = value;
                    _value = null;
                }
            }
        }

        [JsonIgnore]
        public T Value
        {
            get
            {
                if (_value == null && _guid != Guid.Empty)
                {
                    if (PersistentState.Active.EntitiesByGUIDs.ContainsKey(_guid))
                    {
                        _value = (T)PersistentState.Active.EntitiesByGUIDs[_guid];
                    }
                    else
                    {
                        throw new NullReferenceException($"Entity with guid {_guid} not exists!");
                        //_guid = Guid.Empty;
                    }
                }

                return _value;
            }
            set
            {
                _value = value;
                _guid = _value?.Guid ?? Guid.Empty;
            }
        }
        
        public static implicit operator T(Ref<T> @ref)
        {
            return @ref.Value;
        }

        public static implicit operator Ref<T>(T entity)
        {
            return new Ref<T> { Value = entity };
        }
    }
}