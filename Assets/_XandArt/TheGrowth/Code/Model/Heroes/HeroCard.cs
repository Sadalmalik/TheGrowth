using System;
using TheGrowth.Core.Entities;
using UnityEngine;

namespace TheGrowth.Model.Heroes
{
    /// <summary>
    /// Настройки карты героя: компонент конфига карты со ссылкой на персонажа (обязательна). Тег <see cref="Tag"/> ставится
    /// вместе с компонентом: скрипты отличают героя по тегу, C#-код (подходит ли способность) — по компоненту. Несоответствие
    /// тега и компонента — предупреждение валидатора контента.
    /// </summary>
    [Serializable]
    [ComponentKey("hero_card")]
    public sealed class HeroCardSettings : ComponentSettings<HeroCard>
    {
        /// <summary>Тег карт героев (<c>[card] has tag Hero</c>).</summary>
        public const string Tag = "Hero";

        [SerializeField]
        [Tooltip("Character of the hero card. Required.")]
        private CharacterModel m_Character;

        public HeroCardSettings()
        {
        }

        /// <summary>Только тесты.</summary>
        internal HeroCardSettings(CharacterModel character)
        {
            m_Character = character;
        }

        public CharacterModel Character => m_Character;
    }

    /// <summary>Карта героя в игре: персонаж конфига. Состояния нет.</summary>
    public sealed class HeroCard : ComponentBehaviour<HeroCardSettings>
    {
        public CharacterModel Character => Settings.Character;
    }
}
