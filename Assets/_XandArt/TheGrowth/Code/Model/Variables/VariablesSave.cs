using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TheGrowth.Model.Variables
{
    /// <summary>
    /// DTO секции <c>variables</c> (<see cref="VariablesSystem"/>): явно заданные переменные игры <c>{ имя: значение }</c>.
    /// Имена полей — ключи JSON: переименование поля — миграция.
    /// </summary>
    public sealed class VariablesSave
    {
        public Dictionary<string, JToken> Values = new Dictionary<string, JToken>();
    }
}
