using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    [Serializable]
    public struct AssetReference<T> where T : UnityEngine.Object
    {
        [JsonProperty]
        private string _Ref;

        [JsonIgnore]
        private T _Asset;

        [JsonIgnore]
        public T Value
        {
            get => _Asset ?? (string.IsNullOrEmpty(_Ref) ? null : _Asset = Resources.Load<T>(_Ref));
            set => _Ref = (_Asset = value).name;
        }

        public override string ToString()
        {
            return $"[AssetReference<{typeof(T).Name}>:{Value}]";
        }

        public static implicit operator T(AssetReference<T> assetReference) => assetReference.Value;
    }
}