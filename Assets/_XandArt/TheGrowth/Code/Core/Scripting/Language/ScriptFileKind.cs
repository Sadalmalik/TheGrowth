namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Тип файла скрипта; определяет допустимые контексты команд (<see cref="CommandContext"/>).
    /// </summary>
    public enum ScriptFileKind
    {
        /// <summary><c>.script</c> — скрипты карт и общеигровые хуки.</summary>
        Script,

        /// <summary><c>.dialog</c> — диалоги.</summary>
        Dialog,
    }
}
