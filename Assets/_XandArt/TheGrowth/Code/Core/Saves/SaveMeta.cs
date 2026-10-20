using System;

namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// Описание слота (<c>meta.json</c>): по нему строится список слотов без чтения секций. Время записи и вид слота
    /// ставит <see cref="SaveService"/>, версию формата — <see cref="SaveArchive"/>; версию игры, время в игре, шаг
    /// сюжета и локацию — тот, кто сохраняет (<c>GameFlow</c>). Имена полей — ключи JSON (camelCase).
    /// </summary>
    public sealed class SaveMeta
    {
        /// <summary>Версия формата архива (<see cref="SaveArchive.FormatVersion"/>), не версия секций.</summary>
        public int FormatVersion;

        /// <summary>Версия игры (<c>Application.version</c>), записавшей слот.</summary>
        public string GameVersion;

        /// <summary>Время записи, UTC.</summary>
        public DateTime SavedAtUtc;

        /// <summary>Время в игре, секунды.</summary>
        public double PlayTimeSeconds;

        /// <summary>Название шага сюжета; пусто, пока нет StorySystem.</summary>
        public string StoryStep;

        /// <summary>ID локации, в которой сделано сохранение.</summary>
        public string Location;

        public SaveSlotKind Kind;
    }
}
