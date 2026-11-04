using TheGrowth.Core.Content;
using UnityEngine;

namespace TheGrowth.Model.Heroes
{
    /// <summary>
    /// Персонаж — конфиг, на который ссылаются карта героя (<see cref="HeroCardSettings"/>) и карты способностей
    /// (<see cref="AbilityCardSettings"/>, владелец). Пока минимальный: ID и имя строкой (локализации ещё нет); портреты, Spine,
    /// эмоции — с диалогами (E12).
    /// </summary>
    [CreateAssetMenu(menuName = "[Game]/Character", fileName = "Character")]
    public sealed class CharacterModel : ContentAsset
    {
        [SerializeField]
        [Tooltip("Name shown to the player. Not localized yet.")]
        private string m_DisplayName;

        /// <summary>Имя для игрока (строкой, без локализации).</summary>
        public string DisplayName => m_DisplayName;

        /// <summary>Только тесты.</summary>
        internal void SetDisplayName(string displayName)
        {
            m_DisplayName = displayName;
        }
    }
}
