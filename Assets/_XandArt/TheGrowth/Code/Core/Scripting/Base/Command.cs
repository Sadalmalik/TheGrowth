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

        /// <summary>
        /// ID строки (<c>#k3f9a2</c> в конце первой строки команды): ключ локализации и проверка позиции
        /// при загрузке сохранения. null — ID нет. Выставляет парсер, печатает принтер.
        /// </summary>
        public string LineId { get; set; }

        public abstract UniTask Execute(ScriptContext context);
    }
}
