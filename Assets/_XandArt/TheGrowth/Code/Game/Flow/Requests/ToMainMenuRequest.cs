using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheGrowth.Game.Flow
{
    /// <summary>Первый запрос после старта: Boot → MainMenu.</summary>
    public sealed class ToMainMenuRequest : IFlowRequest
    {
        public bool IsAllowedIn(FlowState state)
        {
            return state == FlowState.Boot;
        }

        public UniTask ExecuteAsync(GameFlow flow, CancellationToken token)
        {
            flow.SetState(FlowState.MainMenu);
            return UniTask.CompletedTask;
        }

        public override string ToString()
        {
            return nameof(ToMainMenuRequest);
        }
    }
}
