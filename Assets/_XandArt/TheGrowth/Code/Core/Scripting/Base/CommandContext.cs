namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Где разрешена команда (кондишен, эвалуатор). Проверяется парсером по типу файла
    /// (<see cref="ScriptFileKind"/>).
    /// </summary>
    public enum CommandContext
    {
        /// <summary>В любых файлах.</summary>
        Any,

        /// <summary>Только в диалогах (<c>.dialog</c>): реплики, выборы, сцена диалога.</summary>
        DialogOnly,

        /// <summary>Только вне диалогов (<c>.script</c>): перемещение карт, анимации.</summary>
        GameplayOnly,
    }
}
