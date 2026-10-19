using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Выход в меню: выгрузить локацию → уничтожить сессию → MainMenu. Автосейв перед выходом, если можно
    /// сохраниться, — E02.
    /// </summary>
    public sealed class ExitToMenuRequest : IFlowRequest
    {
        public bool IsAllowedIn(FlowState state)
        {
            return state is FlowState.Hub or FlowState.Expedition;
        }

        public async UniTask ExecuteAsync(GameFlow flow, CancellationToken token)
        {
            flow.SetState(FlowState.Loading);
            await flow.UnloadLocationAsync();
            flow.DestroySession();
            flow.SetState(FlowState.MainMenu);
        }

        public override string ToString()
        {
            return nameof(ExitToMenuRequest);
        }
    }
}
