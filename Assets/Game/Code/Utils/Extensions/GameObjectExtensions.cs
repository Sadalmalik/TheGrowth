using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public static class GameObjectExtensions
    {
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