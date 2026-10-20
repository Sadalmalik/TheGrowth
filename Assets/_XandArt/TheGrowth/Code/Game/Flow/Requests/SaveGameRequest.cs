using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Core.Saves;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Сохранение — ручное или автосейв (<see cref="ISavePointRequest"/>): ждёт ближайшей точки сохранения
    /// (<see cref="GameFlow.CanSave"/>), затем снимает состояние сессии и пишет слот. Ввод не блокирует, экран загрузки
    /// не показывает, очередь переходов не держит: переход, отправленный скриптом, исполняется, пока сохранение ждёт
    /// конца этого скрипта. Ошибка записи — лог, игра продолжается.
    /// Скрипт не может ждать собственное сохранение: пока он исполняется, исполнитель не простаивает и точка сохранения
    /// не наступит. Команда, которая сохраняет (<c>autosave</c>), отправляет запрос и не ждёт его.
    /// </summary>
    public sealed class SaveGameRequest : ISavePointRequest
    {
        private SaveGameRequest(SaveSlotKind kind, string slotId)
        {
            Kind = kind;
            SlotId = slotId;
        }

        public SaveSlotKind Kind { get; }

        /// <summary>Ручной слот для перезаписи; null — новый ручной слот или автосейв по кругу.</summary>
        public string SlotId { get; }

        /// <summary>Записанный слот после исполнения; null — не сохранено (ошибка, сессия закончилась раньше точки сохранения).</summary>
        public SaveSlotInfo Saved { get; private set; }

        /// <param name="slotId">Ручной слот для перезаписи; null — новый слот.</param>
        public static SaveGameRequest Manual(string slotId = null)
        {
            return new SaveGameRequest(SaveSlotKind.Manual, slotId);
        }

        /// <summary>Автосейв: <see cref="SaveService.AutosaveSlotCount"/> слота по кругу.</summary>
        public static SaveGameRequest Autosave()
        {
            return new SaveGameRequest(SaveSlotKind.Auto, null);
        }

        public bool IsAllowedIn(FlowState state)
        {
            return state is FlowState.Hub or FlowState.Expedition;
        }

        public async UniTask ExecuteAsync(GameFlow flow, CancellationToken token)
        {
            Saved = await flow.SaveAsync(Kind, SlotId, token);
        }

        public override string ToString()
        {
            if (Kind == SaveSlotKind.Auto)
                return $"{nameof(SaveGameRequest)}(auto)";

            return SlotId == null ? $"{nameof(SaveGameRequest)}(new manual)" : $"{nameof(SaveGameRequest)}({SlotId})";
        }
    }
}
