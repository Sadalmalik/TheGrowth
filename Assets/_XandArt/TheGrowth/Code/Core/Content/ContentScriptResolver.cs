using System;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Core.Content
{
    /// <summary>
    /// ID конфигов в скриптах по реестру контента: литерал в параметре типа конфига — <see cref="ContentAsset"/>
    /// этого типа (или наследника) с таким ID. Параметры других типов <c>UnityEngine.Object</c> не разрешаются.
    /// </summary>
    public sealed class ContentScriptResolver : IScriptContentResolver
    {
        private readonly IContentRegistry m_Registry;

        public ContentScriptResolver(IContentRegistry registry)
        {
            m_Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public bool TryResolve(Type type, string id, out object value)
        {
            value = null;
            if (type == null || !typeof(ContentAsset).IsAssignableFrom(type))
                return false;

            if (!m_Registry.TryGet(type, id, out var asset))
                return false;

            value = asset;
            return true;
        }

        public bool TryGetId(object value, out string id)
        {
            if (value is ContentAsset asset && m_Registry.Contains(asset))
            {
                id = asset.Id;
                return true;
            }

            id = null;
            return false;
        }
    }
}
