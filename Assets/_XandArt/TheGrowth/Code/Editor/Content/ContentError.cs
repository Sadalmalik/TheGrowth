using Object = UnityEngine.Object;

namespace TheGrowth.EditorTools.Content
{
    public enum ContentErrorKind
    {
        /// <summary>Пустая (пропавшая) ссылка в реестре.</summary>
        MissingAsset,
        EmptyId,
        InvalidId,
        DuplicateId,

        /// <summary>Конфиг или файл скрипта есть в проекте, но не в реестре.</summary>
        NotInRegistry,
        MissingScriptText,
        InvalidScriptId,
        DuplicateScriptId,
    }

    /// <summary>Ошибка валидации контента: вид, путь ассета, сообщение, объект для выделения в консоли.</summary>
    public readonly struct ContentError
    {
        public ContentErrorKind Kind { get; }

        public string Path { get; }

        public string Message { get; }

        public Object Context { get; }

        public ContentError(ContentErrorKind kind, string path, string message, Object context)
        {
            Kind = kind;
            Path = path;
            Message = message;
            Context = context;
        }

        public override string ToString()
        {
            return $"{Path}: {Message}";
        }
    }
}
