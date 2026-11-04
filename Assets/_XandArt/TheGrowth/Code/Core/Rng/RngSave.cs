using System.Collections.Generic;

namespace TheGrowth.Core.Rng
{
    /// <summary>
    /// DTO секции <c>rng</c> (<see cref="RngSystem"/>): состояние генератора — четыре числа <see cref="Xoshiro128"/>. Имена
    /// полей — ключи JSON: переименование поля — миграция.
    /// </summary>
    public sealed class RngSave
    {
        public List<uint> State = new List<uint>();
    }
}
