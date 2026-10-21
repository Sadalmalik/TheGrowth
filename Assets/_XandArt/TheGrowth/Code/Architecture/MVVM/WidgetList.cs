using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using ObservableCollections;
using UnityEngine;

namespace TheGrowth.Architecture.MVVM
{
    /// <summary>
    /// Список виджетов по наблюдаемому списку (ObservableCollections): на элемент — вьюха из пула, привязанная к
    /// элементу; порядок вьюх и их порядок в иерархии контейнера — порядок элементов. Добавление, удаление, замена и
    /// перестановка меняют только затронутые вьюхи; сброс (очистка, сортировка) пересобирает список. Контейнер — только
    /// для виджетов списка (индекс в иерархии = индекс элемента).
    /// </summary>
    public sealed class WidgetList<TData, TView> : IDisposable where TData : class where TView : View<TData>
    {
        private readonly List<TView> m_Views = new List<TView>();
        private readonly Transform m_Container;
        private IReadOnlyObservableList<TData> m_Source;

        /// <param name="prefab">Вьюха элемента; свободные экземпляры пула лежат выключенными в конце контейнера.</param>
        public WidgetList(TView prefab, Transform container) : this(new ViewPool<TView>(prefab, container), container)
        {
        }

        public WidgetList(ViewPool<TView> pool, Transform container)
        {
            Pool = pool ?? throw new ArgumentNullException(nameof(pool));
            m_Container = container != null ? container : throw new ArgumentNullException(nameof(container));
        }

        public ViewPool<TView> Pool { get; }

        /// <summary>Вьюхи в порядке элементов.</summary>
        public IReadOnlyList<TView> Views => m_Views;

        public bool IsBound => m_Source != null;

        /// <summary>Привязать к списку (прежний отвязывается): вьюхи создаются для текущих элементов.</summary>
        public void Bind(IReadOnlyObservableList<TData> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Unbind();
            m_Source = source;
            source.CollectionChanged += HandleCollectionChanged;
            Rebuild();
        }

        /// <summary>Отвязать: вьюхи отвязываются и возвращаются в пул.</summary>
        public void Unbind()
        {
            if (m_Source == null)
                return;

            m_Source.CollectionChanged -= HandleCollectionChanged;
            m_Source = null;
            ReleaseAll();
        }

        public void Dispose()
        {
            Unbind();
        }

        private void HandleCollectionChanged(in NotifyCollectionChangedEventArgs<TData> change)
        {
            switch (change.Action)
            {
                case NotifyCollectionChangedAction.Add when change.NewStartingIndex >= 0:
                    if (change.IsSingleItem)
                    {
                        Insert(change.NewStartingIndex, change.NewItem);
                    }
                    else
                    {
                        for (var i = 0; i < change.NewItems.Length; i++)
                            Insert(change.NewStartingIndex + i, change.NewItems[i]);
                    }

                    break;
                case NotifyCollectionChangedAction.Remove when change.OldStartingIndex >= 0:
                    var count = change.IsSingleItem ? 1 : change.OldItems.Length;
                    for (var i = 0; i < count; i++)
                        RemoveAt(change.OldStartingIndex);

                    break;
                case NotifyCollectionChangedAction.Replace when change.IsSingleItem && change.NewStartingIndex >= 0:
                    RemoveAt(change.NewStartingIndex);
                    Insert(change.NewStartingIndex, change.NewItem);
                    break;
                case NotifyCollectionChangedAction.Move when change.OldStartingIndex >= 0 && change.NewStartingIndex >= 0:
                    var view = m_Views[change.OldStartingIndex];
                    m_Views.RemoveAt(change.OldStartingIndex);
                    m_Views.Insert(change.NewStartingIndex, view);
                    break;
                default:
                    Rebuild();
                    return;
            }

            SyncSiblings();
        }

        private void Rebuild()
        {
            ReleaseAll();
            var index = 0;
            foreach (var item in m_Source)
                Insert(index++, item);

            SyncSiblings();
        }

        private void Insert(int index, TData item)
        {
            var view = Pool.Get(m_Container);
            m_Views.Insert(index, view);
            view.Bind(item);
        }

        private void RemoveAt(int index)
        {
            var view = m_Views[index];
            m_Views.RemoveAt(index);
            Pool.Release(view);
        }

        private void ReleaseAll()
        {
            for (var i = m_Views.Count - 1; i >= 0; i--)
                Pool.Release(m_Views[i]);

            m_Views.Clear();
        }

        private void SyncSiblings()
        {
            for (var i = 0; i < m_Views.Count; i++)
                m_Views[i].transform.SetSiblingIndex(i);
        }
    }
}
