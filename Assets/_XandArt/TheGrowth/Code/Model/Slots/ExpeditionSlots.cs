using System;
using System.Collections.Generic;
using TheGrowth.Model.Locations;

namespace TheGrowth.Model.Slots
{
    /// <summary>
    /// Слоты экспедиции: служебные (колода, рука, сброс, рюкзак) и поле из графа локации. Создаются при старте экспедиции и
    /// уничтожаются в конце (<see cref="SlotSystem.CreateExpedition"/>, <see cref="SlotSystem.DestroyExpedition"/>); после
    /// загрузки сейва восстанавливаются по локации без сцены.
    /// </summary>
    public sealed class ExpeditionSlots
    {
        private readonly List<Slot> m_All = new List<Slot>();

        internal ExpeditionSlots(LocationModel location, Slot deck, Slot hand, Slot discard, Slot backpack, FieldSlots field)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
            Deck = deck;
            Hand = hand;
            Discard = discard;
            Backpack = backpack;
            Field = field;
            m_All.Add(deck);
            m_All.Add(hand);
            m_All.Add(discard);
            m_All.Add(backpack);
            m_All.AddRange(field.Cells);
            m_All.AddRange(field.Links);
        }

        public LocationModel Location { get; }

        public Slot Deck { get; }

        public Slot Hand { get; }

        public Slot Discard { get; }

        public Slot Backpack { get; }

        public FieldSlots Field { get; }

        /// <summary>Все слоты экспедиции: служебные, клетки, слоты связей.</summary>
        public IReadOnlyList<Slot> All => m_All;
    }
}
