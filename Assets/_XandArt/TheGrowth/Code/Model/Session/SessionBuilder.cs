using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Scripting;

namespace TheGrowth.Model
{
    /// <summary>
    /// Состав сессии: все системы модели, без сцены. Один и тот же для игры и headless-тестов.
    /// Родительский (глобальный) контейнер должен содержать <see cref="IContentRegistry"/> и <see cref="ScriptLanguage"/>.
    /// </summary>
    public static class SessionBuilder
    {
        public static void Register(Container container)
        {
            container.Add(new ContentScriptLibrary());
            container.Add(new ScriptRunner());
        }
    }
}
