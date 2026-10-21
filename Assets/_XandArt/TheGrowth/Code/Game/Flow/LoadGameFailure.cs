using System;
using TheGrowth.Core.Saves;

namespace TheGrowth.Game.Flow
{
    /// <summary>Неудачная загрузка слота (<see cref="GameFlow.LoadFailed"/>).</summary>
    public sealed class LoadGameFailure
    {
        public LoadGameFailure(string slotId, Exception exception, bool returnedToMenu)
        {
            SlotId = slotId;
            Exception = exception;
            ReturnedToMenu = returnedToMenu;
        }

        public string SlotId { get; }

        public Exception Exception { get; }

        /// <summary>Причина для игрока: вид <see cref="SaveException"/> или <see cref="SaveErrorKind.Unknown"/>.</summary>
        public SaveErrorKind Kind => SaveException.KindOf(Exception);

        /// <summary>Ошибка после уничтожения старой сессии: игра вернулась в главное меню. Иначе состояние не менялось.</summary>
        public bool ReturnedToMenu { get; }

        public override string ToString()
        {
            return $"Slot '{SlotId}': {Kind}{(ReturnedToMenu ? ", returned to main menu" : string.Empty)}";
        }
    }
}
