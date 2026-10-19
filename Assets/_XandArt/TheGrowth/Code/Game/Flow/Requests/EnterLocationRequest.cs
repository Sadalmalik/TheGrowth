using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheGrowth.Game.Flow
{
    /// <summary>Смена локации в сессии: выгрузить текущую → загрузить новую (<see cref="GameFlow.LoadLocationAsync"/>).</summary>
    public sealed class EnterLocationRequest : IFlowRequest
    {
        public EnterLocationRequest(string locationId)
        {
            LocationId = locationId;
        }

        public string LocationId { get; }

        public bool IsAllowedIn(FlowState state)
        {
            return state is FlowState.Hub or FlowState.Expedition;
        }

        public async UniTask ExecuteAsync(GameFlow flow, CancellationToken token)
        {
            var location = flow.GetLocation(LocationId);
            flow.SetState(FlowState.Loading);
            await flow.UnloadLocationAsync();
            await flow.LoadLocationAsync(location, token);
        }

        public override string ToString()
        {
            return $"{nameof(EnterLocationRequest)}({LocationId})";
        }
    }
}
