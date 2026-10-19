using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Выход из игры из любого состояния: как <see cref="ExitToMenuRequest"/> (если есть сессия), затем
    /// <c>Application.Quit</c>. Отложить выход до конца автосейва (<c>Application.wantsToQuit</c>) — E02.
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
                flow.SetState(FlowState.Loading);
                await flow.UnloadLocationAsync();
                flow.DestroySession();
            }

            flow.SetState(FlowState.MainMenu);
            flow.QuitApplication();
        }

        public override string ToString()
        {
            return nameof(QuitRequest);
        }
    }
}
