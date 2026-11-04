using System;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Tags;

namespace TheGrowth.Core.Content
{
    /// <summary>
    /// ID контента в скриптах по реестру контента: литерал в параметре типа конфига — <see cref="ContentAsset"/>
    /// этого типа (или наследника) с таким ID; в параметре-теге (<see cref="Tag"/>) — тег, объявленный в реестре тегов
    /// (<see cref="TagCatalog"/> из конфигов реестра). Параметры других типов <c>UnityEngine.Object</c> не разрешаются.
    /// </summary>
    public sealed class ContentScriptResolver : IScriptContentResolver
    {
        private readonly IContentRegistry m_Registry;
        private readonly TagCatalog m_Tags;

        public ContentScriptResolver(IContentRegistry registry)
        {
            m_Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            m_Tags = TagCatalog.FromContent(registry);
        }

        public bool TryResolve(Type type, string id, out object value)
        {
            value = null;
            if (type == typeof(Tag))
                return TryResolveTag(id, out value);

            if (type == null || !typeof(ContentAsset).IsAssignableFrom(type))
                return false;

            if (!m_Registry.TryGet(type, id, out var asset))
                return false;

            value = asset;
            return true;
        }

        public bool TryGetId(object value, out string id)
        {
            switch (value)
            {
                case Tag tag:
                    id = tag.Name;
                    return true;
                case ContentAsset asset when m_Registry.Contains(asset):
                    id = asset.Id;
                    return true;
                default:
                    id = null;
                    return false;
            }
        }

        private bool TryResolveTag(string name, out object value)
        {
            value = m_Tags.Contains(name) ? new Tag(name) : null;
            return value != null;
        }
    }
}
