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

        /// <summary>Колода (<c>DeckModel</c>): строка без конфига, количество меньше 1.</summary>
        InvalidDeck,

        /// <summary>
        /// Раскладка локации с колодой: у поля нет клетки <c>PlayerSpawn</c>, нет поля; предупреждение — набор <c>SpawnSlot</c>
        /// карты колоды на пустом поле пуст или меньше числа её карт.
        /// </summary>
        InvalidDeal,
    }

    /// <summary>
    /// Ошибка валидации контента: вид, путь ассета, сообщение, объект для выделения в консоли. Предупреждение
    /// (<see cref="IsWarning"/>) показывается, но билд не останавливает.
    /// </summary>
    public readonly struct ContentError
    {
        public ContentErrorKind Kind { get; }

        public string Path { get; }

        public string Message { get; }

        public Object Context { get; }

        public bool IsWarning { get; }

        public ContentError(ContentErrorKind kind, string path, string message, Object context, bool isWarning = false)
        {
            Kind = kind;
            Path = path;
            Message = message;
            Context = context;
            IsWarning = isWarning;
        }

        public override string ToString()
        {
            return $"{Path}: {Message}";
        }
    }
}
