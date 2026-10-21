using System;

namespace TheGrowth.Core.Variables
{
    /// <summary>Тип значения переменной.</summary>
    public enum VariableType
    {
        Bool,
        Int,
        String,
    }

    /// <summary>Где живёт значение переменной.</summary>
    public enum VariableScope
    {
        /// <summary>Игра: система сессии, секция <c>variables</c> слота. В скриптах — <c>$имя</c>.</summary>
        Game,

        /// <summary>Карта: у владельца (<see cref="IVariableOwner"/>), сохраняется вместе с ним. В скриптах — <c>[узел].имя</c>.</summary>
        Card,

        /// <summary>Профиль игрока: вне слотов, общий для всех сохранений (открытые концовки, галерея). В скриптах — <c>$имя</c>.</summary>
        Profile,
    }

    public static class VariableTypes
    {
        /// <summary>Тип значения в коде: <c>bool</c>, <c>int</c>, <c>string</c>.</summary>
        public static Type ToClrType(this VariableType type)
        {
            return type switch
            {
                VariableType.Bool => typeof(bool),
                VariableType.Int => typeof(int),
                VariableType.String => typeof(string),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown variable type"),
            };
        }

        /// <summary>Имя типа в сообщениях: <c>bool</c>, <c>int</c>, <c>string</c>.</summary>
        public static string ToText(this VariableType type)
        {
            return type switch
            {
                VariableType.Bool => "bool",
                VariableType.Int => "int",
                VariableType.String => "string",
                _ => type.ToString(),
            };
        }

        /// <summary>Имя типа значения в сообщениях; не тип переменной — имя типа .NET.</summary>
        public static string ToText(Type type)
        {
            if (type == typeof(bool))
                return "bool";

            if (type == typeof(int))
                return "int";

            return type == typeof(string) ? "string" : type?.Name ?? "null";
        }

        /// <summary>Значение по умолчанию типа: <c>false</c>, <c>0</c>, <c>""</c>.</summary>
        public static object GetZeroValue(this VariableType type)
        {
            return type switch
            {
                VariableType.Bool => false,
                VariableType.Int => 0,
                _ => string.Empty,
            };
        }

        public static string ToText(this VariableScope scope)
        {
            return scope switch
            {
                VariableScope.Game => "game",
                VariableScope.Card => "card",
                VariableScope.Profile => "profile",
                _ => scope.ToString(),
            };
        }
    }
}
