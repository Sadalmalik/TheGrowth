using UnityEngine;

namespace Sadalmalik.Utils
{
    public static class ObjectUtils
    {
        public static T Create<T>(string name = null) where T : Component => new GameObject(name ?? typeof(T).Name).AddComponent<T>();
        
        public static void SetLayerRecursively(Transform target, int layer)
        {
            target.gameObject.layer = layer;
            foreach (Transform child in target)
                SetLayerRecursively(child, layer);
        }
    }
}