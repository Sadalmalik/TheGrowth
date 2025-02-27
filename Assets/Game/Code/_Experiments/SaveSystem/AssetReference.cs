using System;
using Newtonsoft.Json;
using UnityEditor;
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
            get
            {
                if (_Asset != null) return _Asset;
                if (string.IsNullOrEmpty(_Ref)) return null;
                _Asset = Resources.Load<T>(_Ref);
                return _Asset;
            }
            set => _Ref = GetPath(_Asset = value);
        }

        public override string ToString()
        {
            return $"[AssetReference<{typeof(T).Name}>: {_Ref} - {Value}]";
        }

        public static implicit operator T(AssetReference<T> assetReference) => assetReference.Value;

        private const string _resourcesFolder = "Resources";

        private static string GetPath(UnityEngine.Object o)
        {
            var path = AssetDatabase.GetAssetPath(o);
            var idx = path.IndexOf(_resourcesFolder, StringComparison.Ordinal) + _resourcesFolder.Length + 1;
            var ext = path.LastIndexOf('.');
            if (ext<0) path.Substring(idx);
            path = path.Substring(idx, ext - idx);
            return path;
        }
    }
}