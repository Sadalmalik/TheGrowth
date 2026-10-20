using TheGrowth.Architecture.IOC;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Источник разобранных файлов скриптов по ID. Нужен <see cref="ScriptRunner"/> для переходов в
    /// другие файлы и восстановления позиции. Регистрируется в контейнере (сессии или выше).
    /// </summary>
    public interface IScriptLibrary : ISharedInterface
    {
        bool TryGetFile(string id, out ScriptFile file);
    }
}
