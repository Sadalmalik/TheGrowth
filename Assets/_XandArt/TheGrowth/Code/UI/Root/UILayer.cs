namespace TheGrowth.UI.Root
{
    /// <summary>
    /// Слой UI: узел корневой вьюхи и свойство корневой VM (<see cref="UIRootViewModel"/>), одно окно на слой.
    /// Порядок значений — порядок отрисовки снизу вверх: всплывающие окна поверх всех, кроме экрана загрузки.
    /// </summary>
    public enum UILayer
    {
        MainMenu = 0,
        Pause = 1,
        SaveLoad = 2,
        Popup = 3,
        Loading = 4,
    }

    public static class UILayers
    {
        /// <summary>Число слоёв (размер таблиц по слою).</summary>
        public const int Count = (int)UILayer.Loading + 1;
    }
}
