using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Запрос перехода к <see cref="GameFlow"/>. Запросы исполняются строго по одному, в порядке очереди; ввод на это
    /// время заблокирован, показан экран загрузки. Исключение внутри запроса — лог и возврат в главное меню.
    /// Запросы сохранения и загрузки (E02) встают сюда же; сохранение — не переход (ввод не блокирует, ждёт
    /// <see cref="GameFlow.CanSave"/>), для него понадобится признак запроса.
    /// </summary>
    public interface IFlowRequest
    {
        /// <summary>Допустим ли запрос в состоянии (колонка «Из состояний»); недопустимый пропускается с предупреждением.</summary>
        bool IsAllowedIn(FlowState state);

        /// <param name="token">Токен глобальной области: отменяется только при завершении приложения.</param>
        UniTask ExecuteAsync(GameFlow flow, CancellationToken token);
    }
}
