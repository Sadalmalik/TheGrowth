using System;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// ID конфигов в скриптах: литерал-идентификатор в параметре типа конфига (наследник
    /// <c>UnityEngine.Object</c>). Реализация — реестр контента (позже); в тестах и импортёре — заглушки.
    /// </summary>
    public interface IScriptContentResolver
    {
        /// <summary>Конфиг типа <paramref name="type"/> по ID. false — такого ID нет (ошибка разбора).</summary>
        bool TryResolve(Type type, string id, out object value);

        /// <summary>ID конфига для принтера. false — объект не из реестра.</summary>
        bool TryGetId(object value, out string id);
    }
}
