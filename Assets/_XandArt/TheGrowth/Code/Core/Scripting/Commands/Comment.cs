namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Комментарий <c>// ...</c>: пустая команда в дереве, чтобы комментарий пережил пересохранение.
    /// Шаблона нет: строку-комментарий распознаёт парсер, печатает принтер.
    /// </summary>
    public sealed class Comment : InstantCommand
    {
        /// <summary>Текст после <c>//</c> как есть (с ведущим пробелом, если он был).</summary>
        public string Text;

        protected override void Run(ScriptContext context)
        {
        }
    }
}
