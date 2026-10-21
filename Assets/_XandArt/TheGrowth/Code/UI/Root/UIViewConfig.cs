using System.Collections.Generic;
using TheGrowth.Architecture.MVVM;
using UnityEngine;

namespace TheGrowth.UI.Root
{
    /// <summary>
    /// Таблица «тип VM → префаб» для <see cref="UIViewFactory"/>: список префабов вьюх, тип VM берётся из вьюхи
    /// (<see cref="ViewBase.SourceType"/>). Один префаб на тип. Ассет — <c>Content/UI/</c>, на него ссылается корневая
    /// вьюха (<see cref="UIRootView"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "[Game]/UI/View Config", fileName = "UIViewConfig")]
    public sealed class UIViewConfig : ScriptableObject
    {
        [SerializeField]
        private List<ViewBase> m_Views = new List<ViewBase>();

        public IReadOnlyList<ViewBase> Views => m_Views;

        /// <summary>Заполнить таблицу (сборка UI в редакторе, тесты).</summary>
        internal void SetViews(IEnumerable<ViewBase> views)
        {
            m_Views = new List<ViewBase>(views);
        }
    }
}
