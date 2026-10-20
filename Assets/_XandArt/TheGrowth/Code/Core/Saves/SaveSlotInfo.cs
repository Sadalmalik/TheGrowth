namespace TheGrowth.Core.Saves
{
    /// <summary>Строка списка слотов: ID и описание (<c>meta.json</c>). Описание не читается — слот повреждён.</summary>
    public sealed class SaveSlotInfo
    {
        public SaveSlotInfo(string slotId, SaveMeta meta, string error = null)
        {
            SlotId = slotId;
            Meta = meta;
            Error = error;
        }

        public string SlotId { get; }

        /// <summary>Описание слота; null — слот повреждён (<see cref="Error"/>).</summary>
        public SaveMeta Meta { get; }

        public string Error { get; }

        public bool IsBroken => Meta == null;

        public override string ToString()
        {
            return IsBroken ? $"{SlotId} (broken: {Error})" : $"{SlotId} ({Meta.Kind}, {Meta.SavedAtUtc:u}, {Meta.Location})";
        }
    }
}
