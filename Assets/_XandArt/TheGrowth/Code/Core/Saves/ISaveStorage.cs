using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;

namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// Хранилище слотов (глобальная область): слот — один двоичный объект по ID. Реализации — модули-интеграции
    /// (файлы, браузер), подключаются в Game. API асинхронный, чтобы реализация могла быть асинхронной (Web);
    /// пул потоков реализация использует только там, где он есть. Вызывается из главного потока и по одной операции за
    /// раз (<see cref="SaveService"/>); ID слота уже проверен (латиница, цифры, «_», «-»).
    /// </summary>
    public interface ISaveStorage : ISharedInterface
    {
        /// <summary>ID всех слотов, порядок не задан.</summary>
        UniTask<IReadOnlyList<string>> ListAsync(CancellationToken token);

        /// <summary>
        /// Поток слота на чтение или null, если слота нет. Поток с поиском (seekable): упаковка читает центральный
        /// каталог zip и только нужные записи. Закрывает вызывающий.
        /// </summary>
        UniTask<Stream> OpenReadAsync(string slotId, CancellationToken token);

        /// <summary>
        /// Атомарная запись: <paramref name="write"/> пишет содержимое слота в поток; слот заменяется только после
        /// успешного завершения. Ошибка во время записи (в том числе в <paramref name="write"/>) не меняет прежнее
        /// содержимое слота и пробрасывается.
        /// </summary>
        UniTask WriteAsync(string slotId, Action<Stream> write, CancellationToken token);

        /// <summary>Удалить слот; слота нет — без ошибки.</summary>
        UniTask DeleteAsync(string slotId, CancellationToken token);
    }
}
