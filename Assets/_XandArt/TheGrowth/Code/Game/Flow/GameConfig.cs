using TheGrowth.Core.Content;
using TheGrowth.Model.Locations;
using UnityEngine;

namespace TheGrowth.Game.Flow
{
    /// <summary>
    /// Настройки старта игры: стартовая локация новой игры (позже — стартовый шаг сюжета). Конфиг контента, а не поле
    /// <c>GameStarter</c>: это данные игры, их проверяет валидатор контента и читают тесты без сцены. Читает только
    /// <see cref="GameFlow"/>, поэтому тип — в слое Game. В проекте один, на него ссылается <c>GameStarter</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "[Game]/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ContentAsset
    {
        [SerializeField]
        private LocationModel m_StartLocation;

        public LocationModel StartLocation => m_StartLocation;
    }
}
