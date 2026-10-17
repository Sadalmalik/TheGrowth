using Cysharp.Threading.Tasks;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Команда скрипта. Асинхронна: следующая команда выполняется после завершения текущей.
    /// </summary>
    public abstract class Command
    {
        /// <summary>
        /// Место команды в исходном тексте. Выставляет парсер; используется в сообщениях об ошибках.
        /// Свойство, а не поле: в данные команды не входит.
        /// </summary>
        public ScriptLocation Location { get; set; }

        public abstract UniTask Execute(ScriptContext context);
    }
}
