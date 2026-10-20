using System;
using System.Collections.Generic;
using TheGrowth.Architecture.IOC;
using UnityEngine;

namespace TheGrowth.Core.Content
{
    /// <summary>
    /// Реестр контента: все <see cref="ContentAsset"/> и файлы скриптов проекта. Собирается редактором
    /// (<c>[TheGrowth]/Content/Rebuild Registry</c>, автоматически при импорте и перед билдом), в игре только
    /// читается. Списки отсортированы по области ID и ID. Индексы строятся лениво на каждый запрошенный тип;
    /// при дубле ID в пределах типа находится первый (дубли ловит валидатор).
    /// В контейнер добавляется как есть: жизненного цикла у ассета нет, <see cref="IShared"/> — пустой.
    /// </summary>
    public sealed class ContentRegistry : ScriptableObject, IContentRegistry, IShared
    {
        [SerializeField]
        private List<ContentAsset> m_Assets = new List<ContentAsset>();

        [SerializeField]
        private List<ScriptFileEntry> m_Scripts = new List<ScriptFileEntry>();

        [NonSerialized]
        private Dictionary<Type, Dictionary<string, ContentAsset>> m_ById;

        [NonSerialized]
        private Dictionary<Type, object> m_ByType;

        [NonSerialized]
        private Dictionary<string, ScriptFileEntry> m_ScriptsById;

        public IReadOnlyList<ContentAsset> Assets => m_Assets;

        public IReadOnlyList<ScriptFileEntry> Scripts => m_Scripts;

#region Lookup

        public bool TryGet<T>(string id, out T asset) where T : ContentAsset
        {
            if (TryGet(typeof(T), id, out var found))
            {
                asset = (T)found;
                return true;
            }

            asset = null;
            return false;
        }

        public T Get<T>(string id) where T : ContentAsset
        {
            if (TryGet<T>(id, out var asset))
                return asset;

            throw new KeyNotFoundException($"{typeof(T).Name} '{id}' not found in content registry '{name}'");
        }

        public IReadOnlyList<T> GetAll<T>() where T : ContentAsset
        {
            m_ByType ??= new Dictionary<Type, object>();
            if (m_ByType.TryGetValue(typeof(T), out var cached))
                return (List<T>)cached;

            var list = new List<T>();
            foreach (var asset in m_Assets)
            {
                if (asset != null && asset is T item)
                    list.Add(item);
            }

            m_ByType.Add(typeof(T), list);
            return list;
        }

        public bool TryGet(Type type, string id, out ContentAsset asset)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            asset = null;
            return !string.IsNullOrEmpty(id) && GetIndex(type).TryGetValue(id, out asset);
        }

        public bool Contains(ContentAsset asset)
        {
            return asset != null && TryGet(asset.GetType(), asset.Id, out var found) && found == asset;
        }

        public bool TryGetScript(string id, out ScriptFileEntry script)
        {
            if (m_ScriptsById == null)
            {
                m_ScriptsById = new Dictionary<string, ScriptFileEntry>(StringComparer.Ordinal);
                foreach (var entry in m_Scripts)
                {
                    if (!string.IsNullOrEmpty(entry.Id) && !m_ScriptsById.ContainsKey(entry.Id))
                        m_ScriptsById.Add(entry.Id, entry);
                }
            }

            script = default;
            return id != null && m_ScriptsById.TryGetValue(id, out script);
        }

        private Dictionary<string, ContentAsset> GetIndex(Type type)
        {
            m_ById ??= new Dictionary<Type, Dictionary<string, ContentAsset>>();
            if (m_ById.TryGetValue(type, out var index))
                return index;

            index = new Dictionary<string, ContentAsset>(StringComparer.Ordinal);
            foreach (var asset in m_Assets)
            {
                if (asset == null || !type.IsInstanceOfType(asset) || string.IsNullOrEmpty(asset.Id) || index.ContainsKey(asset.Id))
                    continue;

                index.Add(asset.Id, asset);
            }

            m_ById.Add(type, index);
            return index;
        }

#endregion

#region Editing

        /// <summary>Только редактор (сборка реестра) и тесты.</summary>
        internal void SetContent(IEnumerable<ContentAsset> assets, IEnumerable<ScriptFileEntry> scripts)
        {
            m_Assets = new List<ContentAsset>(assets ?? Array.Empty<ContentAsset>());
            m_Scripts = new List<ScriptFileEntry>(scripts ?? Array.Empty<ScriptFileEntry>());
            Invalidate();
        }

        internal bool HasContent(IReadOnlyList<ContentAsset> assets, IReadOnlyList<ScriptFileEntry> scripts)
        {
            if (assets.Count != m_Assets.Count || scripts.Count != m_Scripts.Count)
                return false;

            for (var i = 0; i < assets.Count; i++)
            {
                if (assets[i] != m_Assets[i])
                    return false;
            }

            for (var i = 0; i < scripts.Count; i++)
            {
                if (!scripts[i].Equals(m_Scripts[i]))
                    return false;
            }

            return true;
        }

        /// <summary>Сбросить индексы: состав или ID конфигов изменились.</summary>
        internal void Invalidate()
        {
            m_ById = null;
            m_ByType = null;
            m_ScriptsById = null;
        }

        private void OnEnable()
        {
            Invalidate();
        }

        private void OnValidate()
        {
            Invalidate();
        }

#endregion

#region IShared

        void IShared.Init()
        {
        }

        void IShared.Dispose()
        {
        }

#endregion
    }
}
