using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TheGrowth.Model.Expeditions
{
    /// <summary>
    /// DTO секции <c>expedition</c> (<see cref="ExpeditionSystem"/>): идёт ли экспедиция (слоты и герой — в секции <c>slots</c>),
    /// счётчик ходов, номер хода, явно заданные переменные экспедиции <c>{ имя: значение }</c>. Сохранение — только без действия
    /// игрока, поэтому запросов конца хода и экспедиции в сейве нет. Имена полей — ключи JSON: переименование поля — миграция.
    /// </summary>
    public sealed class ExpeditionSave
    {
        public bool Active;

        public int TurnsLeft;

        public int TurnNumber;

        public Dictionary<string, JToken> Variables = new Dictionary<string, JToken>();
    }
}
