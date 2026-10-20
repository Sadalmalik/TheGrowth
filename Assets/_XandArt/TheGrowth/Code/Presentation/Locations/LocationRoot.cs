using System;
using System.Collections.Generic;
using TheGrowth.Architecture.IOC;
using TheGrowth.Model.Locations;
using UnityEngine;

namespace TheGrowth.Presentation.Locations
{
    /// <summary>
    /// Корень сцены локации: ровно один, на корневом объекте сцены. Связывает сцену с кодом без статики и без поиска
    /// по сценам: <c>GameFlow</c> берёт его из корневых объектов загруженной сцены (<see cref="Find"/>) и передаёт
    /// в <see cref="LocationScope.Create"/> (<see cref="Initialize"/>). Корень регистрируется в контейнере локации
    /// (системы локации получают его через <c>[Inject] LocationRoot</c> — якоря слотов, камера, позже), а при
    /// <c>Init</c> контейнера инжектит и инициализирует <see cref="ILocationBehaviour"/> под собой, включая неактивные.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocationRoot : MonoBehaviour, IShared
    {
        private readonly List<ILocationBehaviour> m_Behaviours = new List<ILocationBehaviour>();
        private LocationScope m_Scope;
        private int m_InitializedCount;

        /// <summary>Область, к которой привязан корень; null — не привязан или уже уничтожен.</summary>
        public LocationScope Scope => m_Scope;

        /// <summary>Поведения сцены в порядке инициализации (порядок иерархии).</summary>
        public IReadOnlyList<ILocationBehaviour> Behaviours => m_Behaviours;

        /// <summary>
        /// Привязать к области: регистрация в её контейнере. Вызывается из <c>configure</c> в
        /// <see cref="LocationScope.Create"/>, до <c>Init</c> контейнера: инжект и инициализация поведений идут при
        /// <c>Init</c>, когда все системы локации уже зарегистрированы.
        /// </summary>
        public void Initialize(LocationScope scope)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope));

            if (m_Scope != null)
                throw new InvalidOperationException($"Location root '{name}' is already bound to {m_Scope}");

            if (scope.Container.Active)
                throw new InvalidOperationException($"Location root '{name}': {scope} is already initialized, bind the root in {nameof(LocationScope)}.{nameof(LocationScope.Create)}");

            m_Scope = scope;
            scope.Container.Add(this);
        }

#region IShared

        void IShared.Init()
        {
            GetComponentsInChildren(true, m_Behaviours);

            var container = m_Scope.Container;
            foreach (var behaviour in m_Behaviours)
                container.InjectAt(behaviour.GetType(), behaviour);

            for (m_InitializedCount = 0; m_InitializedCount < m_Behaviours.Count; m_InitializedCount++)
                m_Behaviours[m_InitializedCount].OnLocationInit();
        }

        /// <summary>Поведения — в обратном порядке, только инициализированные; ошибка одного не мешает остальным.</summary>
        void IShared.Dispose()
        {
            for (var i = m_InitializedCount - 1; i >= 0; i--)
            {
                try
                {
                    m_Behaviours[i].OnLocationDispose();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            m_Behaviours.Clear();
            m_InitializedCount = 0;
            m_Scope = null;
        }

#endregion

#region Lookup

        /// <summary>Корень локации среди корневых объектов сцены: ровно один, иначе <see cref="InvalidOperationException"/>.</summary>
        public static LocationRoot Find(IReadOnlyList<GameObject> roots, string sceneName)
        {
            if (TryFind(roots, out var root, out var error))
                return root;

            throw new InvalidOperationException($"Scene '{sceneName}': {error}");
        }

        /// <summary>Ровно один <see cref="LocationRoot"/> на корневых объектах; вложенные не ищутся.</summary>
        public static bool TryFind(IReadOnlyList<GameObject> roots, out LocationRoot root, out string error)
        {
            root = null;
            error = null;
            if (roots != null)
            {
                foreach (var item in roots)
                {
                    if (item == null || !item.TryGetComponent<LocationRoot>(out var found))
                        continue;

                    if (root != null)
                    {
                        error = $"more than one {nameof(LocationRoot)} on root objects ('{root.name}', '{found.name}')";
                        root = null;
                        return false;
                    }

                    root = found;
                }
            }

            if (root == null)
                error = $"no {nameof(LocationRoot)} on root objects";

            return root != null;
        }

#endregion
    }
}
