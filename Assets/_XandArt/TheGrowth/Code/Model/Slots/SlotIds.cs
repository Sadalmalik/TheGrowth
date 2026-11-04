namespace TheGrowth.Model.Slots
{
    /// <summary>
    /// ID слотов — стабильные читаемые строки (сейв, отладка, скрипты): <c>&lt;область&gt;/&lt;слот&gt;</c>. Инвентарь игрока —
    /// одна колода хаба; слоты экспедиции — с префиксом <see cref="ExpeditionPrefix"/>: служебные, клетки поля
    /// (<c>exp/field/&lt;ID слота графа&gt;</c>) и слоты связей для объектов механики (<c>exp/link/&lt;ID связи&gt;</c>).
    /// </summary>
    public static class SlotIds
    {
        /// <summary>Инвентарь игрока: колода хаба, существует всегда.</summary>
        public const string Inventory = "hub/inventory";

        public const string ExpeditionPrefix = "exp/";

        /// <summary>Колода экспедиции.</summary>
        public const string Deck = ExpeditionPrefix + "deck";

        /// <summary>Рука (способности).</summary>
        public const string Hand = ExpeditionPrefix + "hand";

        /// <summary>Сброс.</summary>
        public const string Discard = ExpeditionPrefix + "discard";

        /// <summary>Рюкзак: найденное в экспедиции.</summary>
        public const string Backpack = ExpeditionPrefix + "backpack";

        public const string FieldPrefix = ExpeditionPrefix + "field/";

        public const string LinkPrefix = ExpeditionPrefix + "link/";

        /// <summary>Клетка поля по ID слота графа.</summary>
        public static string Field(string graphSlotId)
        {
            return FieldPrefix + graphSlotId;
        }

        /// <summary>Слот связи поля по ID связи графа.</summary>
        public static string Link(string graphLinkId)
        {
            return LinkPrefix + graphLinkId;
        }
    }
}
