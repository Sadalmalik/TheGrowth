using System.Collections.Generic;
using TheGrowth.Core.Content;
using UnityEngine;

namespace TheGrowth.Model.Decks
{
    /// <summary>
    /// Колода экспедиции — конфиг, на который ссылается локация (<c>LocationModel.Deck</c>): строки «карта + количество +
    /// не раскладывать» (<see cref="DeckEntry"/>). При старте экспедиции колода наполняется картами строк и раскладывается
    /// (<see cref="DealSystem"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "[Game]/Deck", fileName = "Deck")]
    public sealed class DeckModel : ContentAsset
    {
        [SerializeField]
        private List<DeckEntry> m_Entries = new List<DeckEntry>();

        public IReadOnlyList<DeckEntry> Entries => EntryList;

        /// <summary>Только редактор и тесты.</summary>
        internal List<DeckEntry> EntryList => m_Entries ??= new List<DeckEntry>();
    }
}
