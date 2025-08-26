using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XandArt.TheGrowth
{
    public class UIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image imageBackground;
        public Image imagePortrait;
        public Image imageDecor;
        public TMP_Text label;
        
        private Transform m_LastParent;

        [HideInInspector]
        public Transform TargetTransform;

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_LastParent = transform.parent;
            transform.SetParent(transform.root);
            transform.SetAsLastSibling();
            imageBackground.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = Input.mousePosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            imageBackground.raycastTarget = true;
            transform.SetParent(
                TargetTransform != null
                    ? TargetTransform
                    : m_LastParent);
        }
    }
}