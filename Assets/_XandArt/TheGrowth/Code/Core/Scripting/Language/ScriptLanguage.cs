using System;
using TheGrowth.Architecture.IOC;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Язык скриптов как сервис глобальной области: реестр шаблонов строится один раз на приложение
    /// (рефлексия по сборкам), системы сессии берут его из родительского контейнера.
    /// </summary>
    public sealed class ScriptLanguage : SharedObject
    {
        public ScriptRegistry Registry { get; }

        public ScriptLanguage(ScriptRegistry registry)
        {
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>Язык из загруженных сборок игры (<see cref="ScriptRegistry.FromLoadedAssemblies"/>).</summary>
        public static ScriptLanguage FromLoadedAssemblies()
        {
            return new ScriptLanguage(ScriptRegistry.FromLoadedAssemblies());
        }
    }
}
