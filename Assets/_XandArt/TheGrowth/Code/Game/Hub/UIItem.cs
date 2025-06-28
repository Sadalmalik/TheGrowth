using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XandArt.TheGrowth
{
    public class UIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image image;
        
        private Transform m_LastParent;

        public Transform TargetTransform;
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            m_LastParent = transform.parent;
            transform.SetParent(transform.root);
            transform.SetAsLastSibling();
            image.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = Input.mousePosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            image.raycastTarget = true;
            transform.SetParent(
                TargetTransform != null
                    ? TargetTransform
                    : m_LastParent);
        }
    }
}