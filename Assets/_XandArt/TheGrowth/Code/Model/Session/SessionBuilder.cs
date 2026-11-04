using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Rng;
using TheGrowth.Core.Scripting;
using TheGrowth.Model.Cards;
using TheGrowth.Model.Decks;
using TheGrowth.Model.Entities;
using TheGrowth.Model.Expeditions;
using TheGrowth.Model.Locations;
using TheGrowth.Model.Slots;
using TheGrowth.Model.Variables;

namespace TheGrowth.Model
{
    /// <summary>
    /// Состав сессии: все системы модели, без сцены. Один и тот же для игры и headless-тестов.
    /// Родительский (глобальный) контейнер должен содержать <see cref="IContentRegistry"/>, <see cref="ScriptLanguage"/>,
    /// <see cref="Core.Variables.VariableCatalog"/> и <see cref="Architecture.Events.EventBus"/>.
    /// </summary>
    public static class SessionBuilder
    {
        public static void Register(Container container)
        {
            container.Add(new ContentScriptLibrary());
            container.Add(new ScriptRunner());
            container.Add(new RngSystem());
            container.Add(new LocationSystem());
            container.Add(new VariablesSystem());
            container.Add(new EntityRegistry());
            container.Add(new SlotSystem());
            container.Add(new CardSystem());
            container.Add(new DealSystem());
            container.Add(new ExpeditionSystem());
        }
    }
}
