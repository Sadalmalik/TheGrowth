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

        /// <summary>Объявление переменной: имя, дубль имени или прежнего имени, значение по умолчанию.</summary>
        InvalidVariable,

        /// <summary>Объявление тега: имя, дубль имени.</summary>
        InvalidTag,

        /// <summary>Граф поля локации: сцена графа и локации, граф у двух локаций, данные графа, теги слотов.</summary>
        InvalidGraph,

        /// <summary>Настройки поля (<c>FieldConfig</c>): в реестре проекта нет ни одного или их несколько.</summary>
        InvalidFieldConfig,

        /// <summary>Конфиг сущности: вид, компоненты (пустые, пропавшие типы, ключи), обязательные компоненты, теги, переменные.</summary>
        InvalidEntity,

        /// <summary>Поле с <c>[ModelKind]</c> ссылается на конфиг сущности другого вида.</summary>
        WrongModelKind,

        /// <summary>Тип компонента сущности: без ключа, неверный ключ, один ключ у двух типов, не сериализуется.</summary>
        InvalidComponentType,

        /// <summary>Файл скрипта: свойства (<c>Имя -> [...]</c>) в файле, не привязанном к сущности; файл сущности не найден.</summary>
        InvalidScript,
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
