using TheGrowth.Core.Content;
using TheGrowth.Model.Graphs;
using UnityEngine;

namespace TheGrowth.Model.Locations
{
    /// <summary>
    /// Конфиг локации: сцена, тип и поле (граф слотов). Сцена грузится по имени (additive) и должна быть в Build Settings;
    /// на её корневом объекте — ровно один <c>LocationRoot</c> (Presentation). Имя сцены должно быть уникальным в
    /// проекте. Граф записывает редактор поля из компонента графа на сцене; совпадение проверяет валидатор контента.
    /// </summary>
    [CreateAssetMenu(menuName = "[Game]/Location", fileName = "Location")]
    public sealed class LocationModel : ContentAsset
    {
        [SerializeField]
        [Tooltip("Scene name without path and extension. The scene must be in Build Settings.")]
        private string m_SceneName;

        [SerializeField]
        private LocationType m_Type;

        [SerializeField]
        [Tooltip("Field graph of the scene. Written by the graph editor from the Graph component of the scene.")]
        private GraphModel m_Graph;

        public string SceneName => m_SceneName;

        public LocationType Type => m_Type;

        /// <summary>Поле локации; null — у локации нет поля.</summary>
        public GraphModel Graph => m_Graph;
    }
}
