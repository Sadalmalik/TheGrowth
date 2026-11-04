using System;
using TheGrowth.Core.Entities;
using UnityEngine;

namespace TheGrowth.Model.Heroes
{
    /// <summary>
    /// Настройки карты способности: компонент конфига карты со ссылкой на персонажа-владельца. Владельца нет — способность
    /// игрока: подходит любому герою. Раскладка экспедиции кладёт в руку только подходящие герою способности.
    /// </summary>
    [Serializable]
    [ComponentKey("ability_card")]
    public sealed class AbilityCardSettings : ComponentSettings<AbilityCard>
    {
        [SerializeField]
        [Tooltip("Character the ability belongs to. Empty — a player ability: it fits any hero.")]
        private CharacterModel m_Owner;

        public AbilityCardSettings()
        {
        }

        /// <summary>Только тесты.</summary>
        internal AbilityCardSettings(CharacterModel owner)
        {
            m_Owner = owner;
        }

        /// <summary>Персонаж-владелец; null — способность игрока.</summary>
        public CharacterModel Owner => m_Owner;

        /// <summary>Способность подходит персонажу: владельца нет или это он.</summary>
        public bool Fits(CharacterModel character)
        {
            return m_Owner == null || m_Owner == character;
        }
    }

    /// <summary>Карта способности в игре: владелец конфига. Состояния нет (заряды — переменная карты <c>charges</c>).</summary>
    public sealed class AbilityCard : ComponentBehaviour<AbilityCardSettings>
    {
        /// <summary>Персонаж-владелец; null — способность игрока.</summary>
        public CharacterModel Owner => Settings.Owner;

        public bool IsPlayerAbility => Settings.Owner == null;

        /// <summary>
        /// Способность подходит карте героя: владельца нет или он совпадает с персонажем героя (<see cref="HeroCard"/>). У карты
        /// без компонента героя персонажа нет: ей подходят только способности игрока.
        /// </summary>
        public bool Fits(Entity hero)
        {
            return Settings.Fits(hero?.Get<HeroCard>()?.Character);
        }
    }
}
