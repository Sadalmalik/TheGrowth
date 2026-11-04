namespace TheGrowth.Model.Cards
{
    /// <summary>
    /// Свойства файла скрипта карты (<c>Имя -> [выражение]</c>), объявлены в <see cref="CardLanguage"/>. Вычисляются по
    /// запросу (<see cref="CardSystem.TryEvaluate{T}"/>) с <c>[active card]</c> = карта.
    /// </summary>
    public static class CardProperties
    {
        /// <summary>Набор слотов, куда игрок может переместить карту.</summary>
        public const string AllowedMoves = "AllowedMoves";

        /// <summary>Набор слотов начальной раскладки: карта занимает случайный свободный из них (раскладка, шаг 5 E03).</summary>
        public const string SpawnSlot = "SpawnSlot";
    }
}
