using System.Collections.Generic;

namespace Sadalmalik.Utils
{
    public static class ListExtensions
    {
        public static void Resize<T>(this List<T> list, int size) where T : new()
        {
            Resize(list, size, new T());
        }

        public static void Resize<T>(this List<T> list, int size, T filler)
        {
            int currentSize = list.Count;
            if(size < currentSize)
            {
                list.RemoveRange(size, currentSize - size);
            }
            else if(size > currentSize)
            {
                if (size > list.Capacity)
                    list.Capacity = size;
                for(int i=currentSize;i<size;i++)
                    list[i] = filler;
            }
        }
    }
}