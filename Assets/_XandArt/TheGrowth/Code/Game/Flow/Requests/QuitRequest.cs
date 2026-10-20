using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Выход из игры из любого состояния: как <see cref="ExitToMenuRequest"/> с автосейвом (если есть сессия) → дождаться
    /// конца всех записей сейвов → <c>Application.Quit</c>. Закрытие окна (<c>Application.wantsToQuit</c>) откладывается
    /// до этого запроса (<c>GameStarter</c>).
    /// </summary>
    public sealed class QuitRequest : IFlowRequest
    {
        public bool IsAllowedIn(FlowState state)
        {
            return true;
        }

        public async UniTask ExecuteAsync(GameFlow flow, CancellationToken token)
        {
            if (flow.Session != null)
            {
                await flow.AutosaveAsync(token);
                flow.SetState(FlowState.Loading);
                await flow.UnloadLocationAsync();
                flow.DestroySession();
            }

            await flow.Saves.WaitIdleAsync();
            flow.SetState(FlowState.MainMenu);
            flow.QuitApplication();
        }

        public override string ToString()
        {
            return nameof(QuitRequest);
        }
    }
}
