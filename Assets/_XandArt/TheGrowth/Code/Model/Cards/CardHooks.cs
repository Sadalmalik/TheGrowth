using System.Collections.Generic;

namespace TheGrowth.Model.Cards
{
    /// <summary>
    /// Хуки карты — секции файла скрипта карты. Секции нет — хук ничего не делает. Вызывает <see cref="CardSystem"/>:
    /// хуки размещения — при перемещении, создании и уничтожении через неё в слотах с <c>SlotRules.InvokePlaceEvent</c>,
    /// <see cref="OnFlipped"/> — при смене стороны, <see cref="OnStep"/> и <see cref="OnPlacedFirstTime"/> — ход экспедиции и
    /// раскладка (<see cref="CardSystem.InvokeHook"/>).
    /// </summary>
    public static class CardHooks
    {
        /// <summary>После начальной раскладки, в порядке раздачи, у героя — последним.</summary>
        public const string OnPlacedFirstTime = "OnPlacedFirstTime";

        /// <summary>Карту положили в слот (перемещение, создание).</summary>
        public const string OnPlaced = "OnPlaced";

        /// <summary>Карта сменила сторону (лицом вверх / рубашкой).</summary>
        public const string OnFlipped = "OnFlipped";

        /// <summary>Ход экспедиции.</summary>
        public const string OnStep = "OnStep";

        /// <summary>На карту (верхнюю в слоте) легла другая карта: <c>[covering card]</c>.</summary>
        public const string OnCovered = "OnCovered";

        /// <summary>Карта стала верхней: верхнюю карту убрали (перемещение, уничтожение) — <c>[covering card]</c>.</summary>
        public const string OnUnCovered = "OnUnCovered";

        public static readonly IReadOnlyList<string> All = new[] { OnPlacedFirstTime, OnPlaced, OnFlipped, OnStep, OnCovered, OnUnCovered };
    }
}
