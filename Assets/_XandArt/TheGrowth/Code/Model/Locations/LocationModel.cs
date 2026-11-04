using TheGrowth.Core.Content;
using TheGrowth.Model.Decks;
using TheGrowth.Model.Graphs;
using UnityEngine;

namespace TheGrowth.Model.Locations
{
    /// <summary>
    /// Конфиг локации: сцена, тип, поле (граф слотов) и колода экспедиции. Сцена грузится по имени (additive) и должна быть
    /// в Build Settings; на её корневом объекте — ровно один <c>LocationRoot</c> (Presentation). Имя сцены должно быть
    /// уникальным в проекте. Граф записывает редактор поля из компонента графа на сцене; совпадение проверяет валидатор
    /// контента. Колода раскладывается на поле при старте экспедиции (<see cref="DealSystem"/>).
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

        [SerializeField]
        [Tooltip("Expedition deck: dealt on the field at the start of the expedition.")]
        private DeckModel m_Deck;

        public string SceneName => m_SceneName;

        public LocationType Type => m_Type;

        /// <summary>Поле локации; null — у локации нет поля.</summary>
        public GraphModel Graph => m_Graph;

        /// <summary>Колода экспедиции; null — колоды нет (раскладывается только герой).</summary>
        public DeckModel Deck => m_Deck;
    }
}
