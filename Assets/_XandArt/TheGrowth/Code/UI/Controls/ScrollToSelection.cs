using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheGrowth.UI.Controls
{
    /// <summary>
    /// Навигация геймпадом по прокручиваемому списку: выбранный элемент внутри содержимого прокручивается в видимую
    /// область (вертикально). Мышь прокручивает как обычно: подстройка — только при смене выбора.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    public sealed class ScrollToSelection : MonoBehaviour
    {
        [SerializeField]
        private ScrollRect m_ScrollRect;

        private GameObject m_LastSelected;

        private void Awake()
        {
            if (m_ScrollRect == null)
                m_ScrollRect = GetComponent<ScrollRect>();
        }

        private void LateUpdate()
        {
            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected == m_LastSelected)
                return;

            m_LastSelected = selected;
            var content = m_ScrollRect.content;
            var viewport = m_ScrollRect.viewport != null ? m_ScrollRect.viewport : (RectTransform)m_ScrollRect.transform;
            if (selected == null || content == null || !selected.transform.IsChildOf(content))
                return;

            var target = (RectTransform)selected.transform;
            var hidden = content.rect.height - viewport.rect.height;
            if (hidden <= 0f)
                return;

            // Границы выбранного элемента в координатах окна прокрутки.
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            var bottom = viewport.InverseTransformPoint(corners[0]).y;
            var top = viewport.InverseTransformPoint(corners[1]).y;
            var viewportRect = viewport.rect;
            var shift = 0f;
            if (top > viewportRect.yMax)
                shift = top - viewportRect.yMax;
            else if (bottom < viewportRect.yMin)
                shift = bottom - viewportRect.yMin;

            if (Mathf.Approximately(shift, 0f))
                return;

            m_ScrollRect.verticalNormalizedPosition = Mathf.Clamp01(m_ScrollRect.verticalNormalizedPosition + shift / hidden);
        }
    }
}
