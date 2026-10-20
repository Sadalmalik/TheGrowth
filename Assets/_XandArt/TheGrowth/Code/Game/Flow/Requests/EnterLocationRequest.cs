using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Model.Locations;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Смена локации в сессии: выгрузить текущую → загрузить новую (<see cref="GameFlow.LoadLocationAsync"/>). Вход в
    /// хаб — автосейв (<see cref="SaveGameRequest.Autosave"/>) в ближайшей точке сохранения после перехода; новая игра
    /// и загрузка в хаб входят без этого запроса и автосейв не делают.
    /// </summary>
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

            // Сохранение — своя очередь: не ждём, исполнится после перехода.
            if (location.Type == LocationType.Hub)
                flow.Request(SaveGameRequest.Autosave()).Forget();
        }

        public override string ToString()
        {
            return $"{nameof(EnterLocationRequest)}({LocationId})";
        }
    }
}
