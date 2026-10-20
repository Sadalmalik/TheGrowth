using UnityEditor.Toolbars;

namespace TheGrowth.EditorTools.Flow
{
    /// <summary>
    /// Переключатель «Run as game» рядом с кнопками Play. Включён — Play через сцену Main (<see cref="PlayModeStart"/>),
    /// выключен — обычный Play Unity.
    /// </summary>
    public static class RunAsGameToolbar
    {
        private const string ElementPath = "TheGrowth/Run as game";

        private const string Tooltip = "Play starts from the Main scene; an open location scene starts a new game in this location.\n"
            + "Off: regular Unity Play of the open scene.";

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement Create()
        {
            var content = new MainToolbarContent("Run as game", Tooltip);
            return new MainToolbarToggle(content, PlayModeStart.RunAsGame, value => PlayModeStart.RunAsGame = value);
        }
    }
}
