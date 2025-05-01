using System;
using Newtonsoft.Json;
using UnityEngine;

namespace XandArt.TheGrowth
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
        public const string PathPropertyName = nameof(Path);
        public const string ValuePropertyName = nameof(Value);

        [JsonIgnore]
        private string _path;
        
        [JsonIgnore]
        private T _value;

        [JsonIgnore]
        public string Path
        {
            get => _path;
            set
            {
                if (_path != value)
                {
                    _path = value;
                    _value = null;
                }
            }
        }

        [JsonIgnore]
        public T Value
        {
            get
            {
                if (_value == null && !string.IsNullOrEmpty(_path))
                {
                    _value = Resources.Load<T>(_path);
                }

                return _value;
            }
            set
            {
                _value = value;
                _path = _value?.Path;
            }
        }
        
        public static implicit operator T(AssetRef<T> @ref)
        {
            return @ref.Value;
        }

        public static implicit operator AssetRef<T>(T entity)
        {
            return new AssetRef<T> { Value = entity };
        }
    }
}