using System;

namespace TheGrowth.Core.Entities
{
    /// <summary>Вид сущности: где она лежит. Задаётся конфигом (<see cref="EntityModel.Kind"/>).</summary>
    public enum EntityKind
    {
        /// <summary>Карта: лежит в слоте (клетка поля, колода, рука, инвентарь).</summary>
        Card = 0,

        /// <summary>Покрытие: на карте или объекте механики (по сути статус, своя вьюха); в слотах не лежит.</summary>
        Modifier = 1,

        /// <summary>Объект механики: в слоте связи поля (стена, телепорт).</summary>
        Mechanic = 2,
    }

    public static class EntityKinds
    {
        /// <summary>Вид в сообщениях: <c>card</c>, <c>modifier</c>, <c>mechanic</c>.</summary>
        public static string ToText(this EntityKind kind)
        {
            return kind switch
            {
                EntityKind.Card => "card",
                EntityKind.Modifier => "modifier",
                EntityKind.Mechanic => "mechanic",
                _ => kind.ToString(),
            };
        }

        public static bool IsDefined(EntityKind kind)
        {
            return Enum.IsDefined(typeof(EntityKind), kind);
        }
    }
}
