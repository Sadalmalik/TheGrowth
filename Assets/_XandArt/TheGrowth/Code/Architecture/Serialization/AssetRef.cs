using System;
using Newtonsoft.Json;

namespace XandArt.Architecture
{
    /// <summary>
    /// Reference to unity asset.
    /// It allows effectytive serialization for game state.
    /// Use it in all serialized data for referencing Assets from PersistentState.
    /// </summary>
    /// <typeparam name="T">Asset type, i.e. ScriptableAsset (for prefabs)</typeparam>
    [Serializable]
    public struct AssetRef<T> where T : ScriptableAsset
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
                    _value = AssetGuidsManager.GetAsset<T>(_guid);
                }

                return _value;
            }
            set
            {
                _value = value;
                _guid = _value?.Guid ?? Guid.Empty;
            }
        }
        
        public static implicit operator T(AssetRef<T> @ref)
        {
            return @ref.Value;
        }

        public static implicit operator AssetRef<T>(T asset)
        {
            return new AssetRef<T> { Value = asset };
        }
    }
}