using TheGrowth.Core.Scripting;
using TheGrowth.Model.Locations;

namespace TheGrowth.Model.Expeditions
{
    /// <summary>Итог экспедиции: <c>end expedition with victory</c> / <c>end expedition with defeat</c>.</summary>
    public enum ExpeditionResult
    {
        [ScriptToken("victory")]
        Victory,

        [ScriptToken("defeat")]
        Defeat,
    }

    /// <summary>
    /// Экспедиция закончилась (<see cref="ExpeditionSystem.Ended"/> и <c>EventBus</c>): итоги уже подведены — герой, способности и
    /// (при победе) рюкзак в инвентаре, слоты экспедиции уничтожены. Для сюжета и UI.
    /// </summary>
    public readonly struct ExpeditionEnded
    {
        public ExpeditionEnded(LocationModel location, ExpeditionResult result)
        {
            Location = location;
            Result = result;
        }

        public LocationModel Location { get; }

        public ExpeditionResult Result { get; }

        public override string ToString()
        {
            return $"expedition {Location?.Id ?? "null"} ended: {Result}";
        }
    }
}
