using System;
using System.Collections.Generic;
using TheGrowth.Architecture.IOC;

namespace TheGrowth.Core.Content
{
    /// <summary>
    /// Доступ к контенту по типу и строковому ID. Регистрируется в глобальном контейнере (ассет
    /// <see cref="ContentRegistry"/>); статического доступа нет. Поиск по типу включает наследников:
    /// <c>Get&lt;CardModel&gt;</c> находит и <c>HeroCard : CardModel</c>.
    /// </summary>
    public interface IContentRegistry : ISharedInterface
    {
        /// <summary>Все конфиги реестра (пропавшие ссылки — null).</summary>
        IReadOnlyList<ContentAsset> Assets { get; }

        /// <summary>Все файлы скриптов реестра.</summary>
        IReadOnlyList<ScriptFileEntry> Scripts { get; }

        bool TryGet<T>(string id, out T asset) where T : ContentAsset;

        /// <summary>Конфиг по ID; нет — <see cref="KeyNotFoundException"/>.</summary>
        T Get<T>(string id) where T : ContentAsset;

        /// <summary>Конфиги типа <typeparamref name="T"/> и его наследников, в порядке реестра.</summary>
        IReadOnlyList<T> GetAll<T>() where T : ContentAsset;

        /// <summary>Конфиг типа <paramref name="type"/> (или наследника) по ID.</summary>
        bool TryGet(Type type, string id, out ContentAsset asset);

        /// <summary>Объект — тот самый конфиг реестра, что находится по его типу и ID.</summary>
        bool Contains(ContentAsset asset);

        bool TryGetScript(string id, out ScriptFileEntry script);
    }
}
