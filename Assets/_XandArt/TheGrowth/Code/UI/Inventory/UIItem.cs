using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class UIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image imageBackground;
        public Image imagePortrait;
        public Image imageDecor;
        public TMP_Text label;

        public UIInventory Inventory;
        public Entity Data;
        
        private Transform m_LastParent;

        [HideInInspector]
        public Transform TargetTransform;

        public void Set(CardVisual visual)
        {
            imageDecor.sprite = visual.Decor;
            imagePortrait.sprite = visual.Portrait;
        }
        
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