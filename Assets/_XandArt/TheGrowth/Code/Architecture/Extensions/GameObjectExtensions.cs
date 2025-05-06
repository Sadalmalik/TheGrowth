using UnityEngine;

namespace XandArt.Architecture
{
    public static class GameObjectExtensions
    {
        public static void SafeDestroy(this Object go)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(go);
            }
            else
            {
                Object.DestroyImmediate(go);
            }
        }
        
        
        public static void SafeDestroy(this GameObject go)
        {
            if (Application.isPlaying)
            {
                GameObject.Destroy(go);
            }
            else
            {
                GameObject.DestroyImmediate(go);
            }
        }
    }
}