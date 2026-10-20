namespace TheGrowth.Model.Locations
{
    /// <summary>DTO секции <c>locations</c> (<see cref="LocationSystem"/>). Имена полей — ключи JSON: переименование поля — миграция.</summary>
    public sealed class LocationSave
    {
        /// <summary>ID текущей локации; null — локации нет.</summary>
        public string Current;
    }
}
