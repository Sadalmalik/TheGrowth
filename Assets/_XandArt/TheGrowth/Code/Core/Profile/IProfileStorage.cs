using System;
using System.IO;
using TheGrowth.Architecture.IOC;

namespace TheGrowth.Core.Profile
{
    /// <summary>
    /// Хранилище профиля игрока (глобальная область): один объект вне слотов сохранений. Реализации — модули-интеграции
    /// (файл), подключаются в Game. API синхронный: профиль маленький, читается один раз при старте и пишется при
    /// изменении (<see cref="ProfileService"/>).
    /// </summary>
    public interface IProfileStorage : ISharedInterface
    {
        /// <summary>Поток профиля на чтение или null, если профиля ещё нет. Закрывает вызывающий.</summary>
        Stream OpenRead();

        /// <summary>
        /// Атомарная запись: <paramref name="write"/> пишет профиль в поток; прежний профиль заменяется только после
        /// успешного завершения. Ошибка (в том числе в <paramref name="write"/>) не меняет прежний профиль и пробрасывается.
        /// </summary>
        void Write(Action<Stream> write);

        /// <summary>
        /// Копия текущего профиля рядом с ним, с пометкой <paramref name="tag"/>, — чтобы следующая запись не уничтожила
        /// данные, которые не удалось прочитать. Сам профиль не меняется. Имя копии или null, если профиля нет. Ошибка
        /// пробрасывается.
        /// </summary>
        string KeepCopy(string tag);
    }
}
