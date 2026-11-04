using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TheGrowth.Model.Entities
{
    /// <summary>DTO секции <c>entities</c> (<see cref="EntityRegistry"/>). Имена полей — ключи JSON: переименование поля — миграция.</summary>
    public sealed class EntitiesSave
    {
        /// <summary>Следующий <c>InstanceId</c> (счётчик сессии).</summary>
        public int NextId = 1;

        /// <summary>Карты и объекты механики по возрастанию ID; модификаторы — внутри владельца.</summary>
        public List<EntitySave> Entities = new List<EntitySave>();
    }

    /// <summary>
    /// Сущность в сейве: <c>InstanceId</c>, ID конфига, явно заданные переменные, состояния компонентов по ключу компонента
    /// (не по имени типа), модификаторы по порядку. Место карты — в секции <c>slots</c>.
    /// </summary>
    public sealed class EntitySave
    {
        public int Id;

        public string Model;

        /// <summary>Явно заданные переменные <c>{ имя: значение }</c>; нет — null.</summary>
        public Dictionary<string, JToken> Variables;

        /// <summary>Состояния компонентов <c>{ ключ: DTO }</c>; нет — null.</summary>
        public Dictionary<string, JToken> Components;

        /// <summary>Модификаторы по порядку; нет — null.</summary>
        public List<EntitySave> Modifiers;
    }
}
