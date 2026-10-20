using System;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Content;
using TheGrowth.Core.Saves;
using UnityEngine;

namespace TheGrowth.Model.Locations
{
    /// <summary>
    /// Текущая локация сессии (секция <c>locations</c>). Текущую локацию сообщает <c>GameFlow</c> при входе
    /// (<see cref="Enter"/>); сцены система не знает. Локации из сейва нет в контенте (удалена или ID изменён после
    /// обновления игры) — предупреждение и <see cref="Current"/> = null: <c>GameFlow</c> после загрузки входит в
    /// стартовую локацию, остальное состояние сейва сохраняется.
    /// </summary>
    public sealed class LocationSystem : SaveableSystem<LocationSave>
    {
        public const string Key = "locations";

#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private IContentRegistry m_Content;
#pragma warning restore CS0649

        public override string SaveKey => Key;

        /// <summary>Локация, в которую вошла сессия; null — ещё не входила или локации из сейва нет в контенте.</summary>
        public LocationModel Current { get; private set; }

        public void Enter(LocationModel location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            Current = location;
        }

        protected override LocationSave Write()
        {
            return new LocationSave { Current = Current != null ? Current.Id : null };
        }

        protected override void Read(LocationSave save)
        {
            Current = null;
            if (string.IsNullOrEmpty(save.Current))
                return;

            if (m_Content.TryGet<LocationModel>(save.Current, out var location))
                Current = location;
            else
                Debug.LogWarning($"[Saves] Location '{save.Current}' from the save is not in the content");
        }

        protected override void ReadDefaults()
        {
            Current = null;
        }
    }
}
