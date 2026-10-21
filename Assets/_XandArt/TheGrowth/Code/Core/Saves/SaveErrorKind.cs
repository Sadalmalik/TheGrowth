namespace TheGrowth.Core.Saves
{
    /// <summary>
    /// Причина <see cref="SaveException"/>: по ней игроку показывается понятный текст (UI), подробности — в сообщении
    /// исключения и в логе.
    /// </summary>
    public enum SaveErrorKind
    {
        /// <summary>Прочее: ошибка записи, ошибка кода.</summary>
        Unknown = 0,

        /// <summary>Слота нет (удалён) или ID слота недопустим.</summary>
        NotFound = 1,

        /// <summary>Архив, описание или секция повреждены, файл не читается, миграция не смогла привести данные.</summary>
        Corrupted = 2,

        /// <summary>Формат архива или версия секции новее, чем умеет клиент: сейв из более новой версии игры.</summary>
        NewerVersion = 3,

        /// <summary>Секция старой версии, а шага миграции до текущей нет.</summary>
        MissingMigration = 4,
    }
}
