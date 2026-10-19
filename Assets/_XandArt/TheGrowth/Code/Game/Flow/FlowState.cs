namespace TheGrowth.Game.Flow
{
    /// <summary>Состояние автомата <see cref="GameFlow"/>. Диалог и пауза — флаги-оверлеи, не состояния.</summary>
    public enum FlowState
    {
        Boot = 0,
        MainMenu = 1,
        Loading = 2,
        Hub = 3,
        Expedition = 4,
    }
}
