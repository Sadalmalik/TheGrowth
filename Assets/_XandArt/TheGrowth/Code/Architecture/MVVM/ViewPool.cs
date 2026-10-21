using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheGrowth.Architecture.MVVM
{
    /// <summary>
    /// Пул экземпляров одного префаба. <see cref="Get"/> берёт свободный экземпляр (или создаёт) и включает его под
    /// родителем; <see cref="Release"/> отвязывает вьюху (<see cref="ViewBase.Unbind"/>), выключает объект и переносит
    /// под <see cref="Root"/>. Экземпляр, уничтоженный снаружи, из пула выпадает.
    /// </summary>
    public sealed class ViewPool<T> where T : Component
    {
        private readonly Stack<T> m_Free = new Stack<T>();

        /// <param name="prefab">Префаб (или объект-шаблон в памяти).</param>
        /// <param name="root">Родитель свободных экземпляров; null — корень сцены.</param>
        public ViewPool(T prefab, Transform root = null)
        {
            Prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            Root = root;
        }

        public T Prefab { get; }

        public Transform Root { get; }

        /// <summary>Создано экземпляров за жизнь пула.</summary>
        public int CountCreated { get; private set; }

        /// <summary>Свободных экземпляров (включая уничтоженные снаружи, до следующего <see cref="Get"/>).</summary>
        public int CountFree => m_Free.Count;

        public T Get(Transform parent)
        {
            T item = null;
            while (item == null && m_Free.Count > 0)
                item = m_Free.Pop();

            if (item == null)
            {
                item = Object.Instantiate(Prefab, parent, false);
                item.name = Prefab.name;
                CountCreated++;
            }
            else
            {
                item.transform.SetParent(parent, false);
            }

            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null)
                return;

            if (item is ViewBase view)
                view.Unbind();

            item.gameObject.SetActive(false);
            item.transform.SetParent(Root, false);
            item.transform.SetAsLastSibling();
            if (!m_Free.Contains(item))
                m_Free.Push(item);
        }

        /// <summary>Уничтожить свободные экземпляры.</summary>
        public void Clear()
        {
            while (m_Free.Count > 0)
            {
                var item = m_Free.Pop();
                if (item == null)
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(item.gameObject);
                else
                    Object.DestroyImmediate(item.gameObject);
            }
        }
    }
}
