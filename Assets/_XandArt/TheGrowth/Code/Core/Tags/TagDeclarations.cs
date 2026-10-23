using System.Collections.Generic;
using TheGrowth.Core.Content;
using UnityEngine;

namespace TheGrowth.Core.Tags
{
    /// <summary>
    /// Конфиг объявлений тегов: общий реестр тегов карт и слотов поля. Конфигов может быть несколько по темам; все
    /// объявления из реестра контента собирает <see cref="TagCatalog"/>, имена уникальны по всем конфигам.
    /// </summary>
    [CreateAssetMenu(menuName = "[Game]/Tags", fileName = "Tags")]
    public sealed class TagDeclarations : ContentAsset
    {
        [SerializeField]
        private List<TagDeclaration> m_Tags = new List<TagDeclaration>();

        public IReadOnlyList<TagDeclaration> Tags => m_Tags;

        /// <summary>Только редактор (создание примера) и тесты.</summary>
        internal void SetTags(IEnumerable<TagDeclaration> tags)
        {
            m_Tags = new List<TagDeclaration>(tags);
        }
    }
}
