using TheGrowth.Core.Content;
using UnityEngine;

namespace TheGrowth.Model.Locations
{
    /// <summary>
    /// Конфиг локации: сцена и тип. Сцена грузится по имени (additive) и должна быть в Build Settings; на её корневом
    /// объекте — ровно один <c>LocationRoot</c> (Presentation). Имя сцены должно быть уникальным в проекте.
    /// </summary>
    [CreateAssetMenu(menuName = "[Game]/Location", fileName = "Location")]
    public sealed class LocationModel : ContentAsset
    {
        [SerializeField]
        [Tooltip("Scene name without path and extension. The scene must be in Build Settings.")]
        private string m_SceneName;

        [SerializeField]
        private LocationType m_Type;

        public string SceneName => m_SceneName;

        public LocationType Type => m_Type;
    }
}
