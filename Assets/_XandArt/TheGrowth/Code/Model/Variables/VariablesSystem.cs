using TheGrowth.Architecture.Events;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Saves;
using TheGrowth.Core.Variables;

namespace TheGrowth.Model.Variables
{
    /// <summary>
    /// Переменные области «игра» (система сессии, секция <c>variables</c>). В сейв пишутся только явно заданные значения:
    /// новое значение по умолчанию в объявлении действует и на старые сейвы. Чтение: значение по прежнему имени — под
    /// новым, необъявленное или другого типа — предупреждение (<see cref="VariableValues.Read"/>). Изменение —
    /// <see cref="VariableChanged"/> в <see cref="EventBus"/>.
    /// Требует в контейнере (глобальном) <see cref="VariableCatalog"/> и <see cref="EventBus"/>.
    /// </summary>
    public sealed class VariablesSystem : SaveableSystem<VariablesSave>, IGameVariables
    {
        public const string Key = "variables";

        private const string LogSource = "[Saves] Section '" + Key + "'";

#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private VariableCatalog m_Catalog;

        [Inject]
        private EventBus m_Events;
#pragma warning restore CS0649

        public override string SaveKey => Key;

        public VariableStore Variables { get; private set; }

        public override void Init()
        {
            Variables = new VariableStore(VariableScope.Game, m_Events);
        }

        protected override VariablesSave Write()
        {
            return new VariablesSave { Values = VariableValues.Write(Variables) };
        }

        protected override void Read(VariablesSave save)
        {
            VariableValues.Read(Variables, m_Catalog, save.Values, LogSource);
        }

        protected override void ReadDefaults()
        {
            Variables.Clear();
        }
    }
}
