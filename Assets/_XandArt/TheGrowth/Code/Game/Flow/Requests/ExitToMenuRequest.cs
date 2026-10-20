using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Выход в меню: автосейв, если сессия в точке сохранения (<see cref="GameFlow.AutosaveAsync"/>, до выгрузки) →
    /// выгрузить локацию → уничтожить сессию → MainMenu.
    /// </summary>
    public sealed class ExitToMenuRequest : IFlowRequest
    {
        public bool IsAllowedIn(FlowState state)
        {
            return state is FlowState.Hub or FlowState.Expedition;
        }

        public async UniTask ExecuteAsync(GameFlow flow, CancellationToken token)
        {
            await flow.AutosaveAsync(token);
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
