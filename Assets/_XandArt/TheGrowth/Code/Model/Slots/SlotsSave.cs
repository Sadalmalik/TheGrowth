using System.Collections.Generic;

namespace TheGrowth.Model.Slots
{
    /// <summary>
    /// DTO секции <c>slots</c> (<see cref="SlotSystem"/>): какие слоты есть (инвентарь — всегда, слоты экспедиции — по её
    /// локации) и порядок сущностей в непустых слотах. Имена полей — ключи JSON: переименование поля — миграция.
    /// </summary>
    public sealed class SlotsSave
    {
        /// <summary>ID локации активной экспедиции: по её графу восстанавливаются слоты экспедиции; нет экспедиции — null.</summary>
        public string Expedition;

        /// <summary>Непустые слоты в порядке создания.</summary>
        public List<SlotSave> Slots = new List<SlotSave>();
    }

    /// <summary>Содержимое слота: ID слота и <c>InstanceId</c> сущностей по порядку.</summary>
    public sealed class SlotSave
    {
        public string Id;

        public List<int> Entities = new List<int>();
    }
}
