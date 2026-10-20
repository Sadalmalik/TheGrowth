using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Core.Saves;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Новая игра: создать сессию из пустого сейва (тот же путь, что загрузка) → войти в стартовую локацию (<see cref="GameConfig.StartLocation"/> или
    /// <see cref="LocationId"/>). Старт сюжета (<c>StorySystem.Start(startStep)</c>) — когда появится StorySystem.
    /// Из Boot — запуск из сцены локации в редакторе (вместо главного меню).
    /// </summary>
    public sealed class NewGameRequest : IFlowRequest
    {
        /// <param name="locationId">Локация вместо стартовой из конфига; null — стартовая.</param>
        public NewGameRequest(string locationId = null)
        {
            LocationId = locationId;
        }

        /// <summary>
        /// Локация вместо стартовой (Play из сцены локации). Точка расширения T058: пресет (<c>StoryStep</c>),
        /// настраивающий сессию под эту локацию, — рядом, применяется между созданием сессии и входом в локацию.
        /// </summary>
        public string LocationId { get; }

        public bool IsAllowedIn(FlowState state)
        {
            return state is FlowState.Boot or FlowState.MainMenu;
        }

        public async UniTask ExecuteAsync(GameFlow flow, CancellationToken token)
        {
            var location = string.IsNullOrEmpty(LocationId) ? flow.Config.StartLocation : flow.GetLocation(LocationId);
            if (location == null)
                throw new InvalidOperationException($"{flow.Config} has no start location");

            flow.SetState(FlowState.Loading);
            flow.CreateSession(new SaveGame());
            await flow.LoadLocationAsync(location, token);
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(LocationId) ? nameof(NewGameRequest) : $"{nameof(NewGameRequest)}({LocationId})";
        }
    }
}
