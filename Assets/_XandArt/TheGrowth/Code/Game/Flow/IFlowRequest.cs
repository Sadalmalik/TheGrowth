using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Запрос к <see cref="GameFlow"/>. Переходы исполняются строго по одному, в порядке очереди; ввод на это время
    /// заблокирован, показан экран загрузки. Исключение внутри перехода — лог и возврат в главное меню.
    /// Сохранение — не переход (<see cref="ISavePointRequest"/>): своя очередь, ждёт <see cref="GameFlow.CanSave"/>.
    /// </summary>
    public interface IFlowRequest
    {
        /// <summary>Допустим ли запрос в состоянии (колонка «Из состояний»); недопустимый пропускается с предупреждением.</summary>
        bool IsAllowedIn(FlowState state);

        /// <param name="token">Токен глобальной области: отменяется только при завершении приложения.</param>
        UniTask ExecuteAsync(GameFlow flow, CancellationToken token);
    }
}
