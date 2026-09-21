using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sadalmalik.Utils
{
    public static class HierarchyUtils
    {
        public static void SafeDestroy(GameObject obj)
        {
            if (Application.isPlaying)
                GameObject.Destroy(obj);
            else
                GameObject.DestroyImmediate(obj);
        }

        public static void SafeDestroy(GameObject obj, float delay)
        {
            if (Application.isPlaying)
                GameObject.Destroy(obj, delay);
            else
                GameObject.DestroyImmediate(obj);
        }
    }
}