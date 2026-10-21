using System;
using System.Collections.Generic;
using TheGrowth.Architecture.MVVM;
using UnityEngine;

namespace TheGrowth.UI.Root
{
    /// <summary>
    /// Вьюхи окон по требованию: префаб по типу VM из <see cref="UIViewConfig"/> (точный тип, затем базовые), экземпляры
    /// — из пула префаба. <see cref="Create"/> ставит вьюху под узел слоя и привязывает к VM, <see cref="Release"/>
    /// отвязывает и возвращает в пул (выключенной, под <c>poolRoot</c>).
    /// </summary>
    public sealed class UIViewFactory
    {
        private readonly Dictionary<Type, ViewPool<ViewBase>> m_Pools = new Dictionary<Type, ViewPool<ViewBase>>();

        public UIViewFactory(UIViewConfig config, Transform poolRoot)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            foreach (var prefab in config.Views)
            {
                if (prefab == null)
                {
                    Debug.LogError($"[UI] {config.name}: empty view entry", config);
                    continue;
                }

                if (prefab is not IWindowView)
                {
                    Debug.LogError($"[UI] {config.name}: '{prefab.name}' is not a window view", config);
                    continue;
                }

                var type = prefab.SourceType;
                if (m_Pools.ContainsKey(type))
                {
                    Debug.LogError($"[UI] {config.name}: second view for {type.Name} ('{prefab.name}') is ignored", config);
                    continue;
                }

                m_Pools.Add(type, new ViewPool<ViewBase>(prefab, poolRoot));
            }
        }

        public bool CanCreate(Type viewModelType)
        {
            return FindPool(viewModelType) != null;
        }

        /// <summary>Вьюха окна под <paramref name="parent"/>, привязанная к <paramref name="viewModel"/>. Префаба нет — исключение.</summary>
        public IWindowView Create(WindowViewModel viewModel, Transform parent)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            var pool = FindPool(viewModel.GetType()) ?? throw new InvalidOperationException($"No view prefab for {viewModel.GetType().Name} in the UI view config");
            var view = pool.Get(parent);
            try
            {
                view.BindObject(viewModel);
            }
            catch
            {
                pool.Release(view);
                throw;
            }

            return (IWindowView)view;
        }

        public void Release(IWindowView window)
        {
            var view = window?.View;
            if (view == null)
                return;

            window.StopAnimation();
            var pool = FindPool(view.SourceType);
            if (pool != null)
            {
                pool.Release(view);
                return;
            }

            view.Unbind();
            Destroy(view.gameObject);
        }

        /// <summary>Пул вьюх типа VM (тесты, отладка) или null.</summary>
        public ViewPool<ViewBase> GetPool(Type viewModelType)
        {
            return FindPool(viewModelType);
        }

        /// <summary>Уничтожить свободные экземпляры всех пулов.</summary>
        public void Clear()
        {
            foreach (var pool in m_Pools.Values)
                pool.Clear();
        }

        private ViewPool<ViewBase> FindPool(Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (m_Pools.TryGetValue(current, out var pool))
                    return pool;
            }

            return null;
        }

        private static void Destroy(GameObject gameObject)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(gameObject);
            else
                UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
