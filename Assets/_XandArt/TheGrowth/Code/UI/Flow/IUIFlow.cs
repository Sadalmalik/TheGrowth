using Cysharp.Threading.Tasks;
using R3;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Saves;

namespace TheGrowth.UI.Flow
{
    /// <summary>Состояние игры, по которому UI выбирает окна: копия состояний <c>GameFlow</c> без зависимости от Game.</summary>
    public enum UIFlowState
    {
        Boot = 0,
        MainMenu = 1,
        Loading = 2,
        Hub = 3,
        Expedition = 4,
    }

    public static class UIFlowStates
    {
        /// <summary>В локации (хаб или экспедиция): пауза, сохранение.</summary>
        public static bool IsInLocation(this UIFlowState state)
        {
            return state is UIFlowState.Hub or UIFlowState.Expedition;
        }
    }

    /// <summary>
    /// Всё, что UI нужно от Game (глобальная область): состояние для выбора окон и действия меню. UI ниже Game и не знает
    /// <c>GameFlow</c> и его запросов: интерфейс объявлен здесь, реализация — адаптер в Game, который превращает действия в
    /// запросы <c>GameFlow</c>. Список слотов и превью UI читает из <see cref="SaveService"/> напрямую; сохраняет и
    /// загружает — только через этот интерфейс. Действия не ждут перехода и не бросают исключений.
    /// </summary>
    public interface IUIFlow : ISharedInterface
    {
        ReadOnlyReactiveProperty<UIFlowState> State { get; }

        /// <summary>Сейчас точка сохранения (сохранение исполнится сразу, иначе — когда она наступит).</summary>
        bool CanSave { get; }

        void NewGame();

        void Load(string slotId);

        /// <summary>
        /// Ручное сохранение: <paramref name="slotId"/> null — новый слот, иначе перезапись ручного слота. Ждёт точки
        /// сохранения. Результат — записанный слот или null (ошибка записи, сессия закончилась раньше).
        /// </summary>
        UniTask<SaveSlotInfo> SaveAsync(string slotId);

        void ExitToMenu();

        void Quit();
    }
}
