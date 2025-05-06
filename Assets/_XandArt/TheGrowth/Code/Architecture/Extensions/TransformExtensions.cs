using System.Collections.Generic;
using UnityEngine;

namespace XandArt.Architecture
{
    public static class TransformExtensions
    {
        public static void SafeDestroy(this Transform transform)
        {
            transform.gameObject.SafeDestroy();
        }

        public static void Clear(this Transform transform)
        {
            var toDestroy = new List<Transform>();
            foreach (Transform children in transform)
            {
                toDestroy.Add(children);
            }
            foreach (var children in toDestroy)
            {
                children.SafeDestroy();
            }
            toDestroy.Clear();
        }
    }
}