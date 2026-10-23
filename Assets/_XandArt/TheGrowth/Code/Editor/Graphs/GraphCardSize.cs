using System;
using TheGrowth.Core.Content;
using TheGrowth.EditorTools.Content;
using TheGrowth.Model.Graphs;
using UnityEditor;
using UnityEngine;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Размер карты для редактора поля: <see cref="FieldConfig.CardSize"/> конфига из реестра контента проекта; реестра или
    /// конфига нет — <see cref="FieldConfig.DefaultCardSize"/> (отсутствие конфига — ошибка валидатора контента). Тесты
    /// подменяют размер на время (<see cref="Override"/>). Найденный конфиг запоминается (гизмо читает размер каждый кадр),
    /// удалённый ищется заново.
    /// </summary>
    public static class GraphCardSize
    {
        private static Vector2? m_Override;
        private static FieldConfig m_Config;

        /// <summary>X — ширина карты (вдоль оси X графа), Y — длина (вдоль оси Z).</summary>
        public static Vector2 Current => m_Override ?? Load();

        /// <summary>Конфиг настроек поля из реестра контента проекта или null.</summary>
        public static FieldConfig FindConfig()
        {
            if (m_Config != null)
                return m_Config;

            var registry = AssetDatabase.LoadAssetAtPath<ContentRegistry>(ContentRegistryBuilder.RegistryPath);
            if (registry == null)
                return null;

            var configs = registry.GetAll<FieldConfig>();
            m_Config = configs.Count > 0 ? configs[0] : null;
            return m_Config;
        }

        /// <summary>Подменить размер карты до <see cref="IDisposable.Dispose"/> (тесты); прежняя подмена восстанавливается.</summary>
        internal static IDisposable Override(Vector2 size)
        {
            var scope = new OverrideScope(m_Override);
            m_Override = size;
            return scope;
        }

        private static Vector2 Load()
        {
            var config = FindConfig();
            return config != null ? config.CardSize : FieldConfig.DefaultCardSize;
        }

        private sealed class OverrideScope : IDisposable
        {
            private readonly Vector2? m_Previous;
            private bool m_IsDisposed;

            public OverrideScope(Vector2? previous)
            {
                m_Previous = previous;
            }

            public void Dispose()
            {
                if (m_IsDisposed)
                    return;

                m_IsDisposed = true;
                m_Override = m_Previous;
            }
        }
    }
}
