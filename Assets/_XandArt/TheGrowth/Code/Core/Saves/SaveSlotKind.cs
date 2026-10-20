namespace TheGrowth.Core.Saves
{
    /// <summary>Вид слота: ручное сохранение (без лимита) или автосейв (<see cref="SaveService.AutosaveSlotCount"/> по кругу).</summary>
    public enum SaveSlotKind
    {
        Manual = 0,
        Auto = 1,
    }
}
