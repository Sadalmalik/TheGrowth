using System.Collections;
using System.Collections.Generic;

namespace TheGrowth.Model.Slots
{
    /// <summary>
    /// Упорядоченный набор без повторов — значение эвалуаторов наборов в скриптах (<see cref="SlotSet"/>,
    /// <c>CardSet</c>). Неизменяемый; повтор при создании пропускается (остаётся первое вхождение). Операции сохраняют порядок
    /// левого операнда.
    /// </summary>
    public abstract class ItemSet<T> : IReadOnlyList<T> where T : class
    {
        private readonly List<T> m_Items = new List<T>();
        private readonly HashSet<T> m_Lookup = new HashSet<T>();

        protected ItemSet(IEnumerable<T> items)
        {
            if (items == null)
                return;

            foreach (var item in items)
            {
                if (item != null && m_Lookup.Add(item))
                    m_Items.Add(item);
            }
        }

        public int Count => m_Items.Count;

        public bool IsEmpty => m_Items.Count == 0;

        public T this[int index] => m_Items[index];

        public bool Contains(T item)
        {
            return item != null && m_Lookup.Contains(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>Элементы этого набора, которых нет в <paramref name="other"/>.</summary>
        protected IEnumerable<T> ExceptItems(ItemSet<T> other)
        {
            foreach (var item in m_Items)
            {
                if (other == null || !other.Contains(item))
                    yield return item;
            }
        }

        /// <summary>Элементы этого набора, затем элементы <paramref name="other"/>, которых здесь нет.</summary>
        protected IEnumerable<T> UnionItems(ItemSet<T> other)
        {
            foreach (var item in m_Items)
                yield return item;

            if (other == null)
                yield break;

            foreach (var item in other)
            {
                if (!Contains(item))
                    yield return item;
            }
        }

        /// <summary>Элементы этого набора, которые есть и в <paramref name="other"/>.</summary>
        protected IEnumerable<T> IntersectItems(ItemSet<T> other)
        {
            foreach (var item in m_Items)
            {
                if (other != null && other.Contains(item))
                    yield return item;
            }
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", m_Items)}]";
        }
    }
}
