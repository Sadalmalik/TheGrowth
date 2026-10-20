using System.Collections.Generic;
using UnityEngine;

namespace XandArt.Architecture
{
    public static class ListExtensions
    {
        public static void Shuffle<T>(this List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--) 
            {
                var k = Random.Range(0, i+1);
                (list[i], list[k]) = (list[k], list[i]);
            }
        }

        public static T Top<T>(this List<T> list)
        {
            var last = list.Count - 1;
            if (last < 0) return default;
            return list[last];
        }

        public static T Peek<T>(this List<T> list)
        {
            var last = list.Count - 1;
            if (last < 0) return default;
            var item = list[last];
            list.RemoveAt(last);
            return item;
        }
    }
}