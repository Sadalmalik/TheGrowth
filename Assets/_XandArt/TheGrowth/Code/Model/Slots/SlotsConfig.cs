using TheGrowth.Core.Content;
using UnityEngine;

namespace TheGrowth.Model.Slots
{
    /// <summary>
    /// Ёмкость служебных слотов (баланс, не константы кода): инвентарь игрока (<see cref="SlotIds.Inventory"/>) и рука
    /// экспедиции (<see cref="SlotIds.Hand"/>); 0 — без ограничения. Остальные служебные слоты — без ограничения. Читает
    /// <see cref="SlotSystem"/> при создании сессии; в проекте ровно один (проверяет валидатор контента), лежит в
    /// <c>Content/Slots/</c>. Конфига нет — значения по умолчанию этого конфига.
    /// </summary>
    [CreateAssetMenu(menuName = "[Game]/Slots Config", fileName = "SlotsConfig")]
    public sealed class SlotsConfig : ContentAsset
    {
        /// <summary>Инвентарь по умолчанию — без ограничения.</summary>
        public const int DefaultInventoryCapacity = 0;

        /// <summary>Рука по умолчанию — 3 карты.</summary>
        public const int DefaultHandCapacity = 3;

        [SerializeField]
        [Min(0)]
        [Tooltip("How many cards the player inventory (hub) holds. 0 — unlimited.")]
        private int m_InventoryCapacity = DefaultInventoryCapacity;

        [SerializeField]
        [Min(0)]
        [Tooltip("How many cards the expedition hand (abilities) holds. 0 — unlimited.")]
        private int m_HandCapacity = DefaultHandCapacity;

        /// <summary>Ёмкость инвентаря; 0 — без ограничения.</summary>
        public int InventoryCapacity => Mathf.Max(0, m_InventoryCapacity);

        /// <summary>Ёмкость руки экспедиции; 0 — без ограничения.</summary>
        public int HandCapacity => Mathf.Max(0, m_HandCapacity);

        /// <summary>Только тесты.</summary>
        internal void SetCapacities(int inventory, int hand)
        {
            m_InventoryCapacity = inventory;
            m_HandCapacity = hand;
        }
    }
}
