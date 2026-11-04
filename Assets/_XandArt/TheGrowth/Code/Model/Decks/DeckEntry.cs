using System;
using TheGrowth.Core.Entities;
using UnityEngine;

namespace TheGrowth.Model.Decks
{
    /// <summary>
    /// Строка колоды (<see cref="DeckModel"/>): конфиг карты, количество, «не раскладывать» — карты строки при старте экспедиции
    /// остаются в колоде (для скриптов). Количество не меньше 1, конфиг — вида Card (проверяет валидатор контента).
    /// </summary>
    [Serializable]
    public sealed class DeckEntry
    {
        [SerializeField]
        [ModelKind(EntityKind.Card)]
        private EntityModel m_Model;

        [SerializeField]
        [Min(1)]
        private int m_Count = 1;

        [SerializeField]
        [Tooltip("Don't deal: the cards of the row stay in the expedition deck at the start (for scripts).")]
        private bool m_KeepInDeck;

        public DeckEntry()
        {
        }

        /// <summary>Только тесты.</summary>
        internal DeckEntry(EntityModel model, int count = 1, bool keepInDeck = false)
        {
            m_Model = model;
            m_Count = count;
            m_KeepInDeck = keepInDeck;
        }

        public EntityModel Model => m_Model;

        public int Count => m_Count;

        /// <summary>Не раскладывать: карты остаются в колоде экспедиции.</summary>
        public bool KeepInDeck => m_KeepInDeck;

        public override string ToString()
        {
            var keep = m_KeepInDeck ? ", keep in deck" : string.Empty;
            return $"{(m_Model != null ? m_Model.Id : "<none>")} x{m_Count}{keep}";
        }
    }
}
