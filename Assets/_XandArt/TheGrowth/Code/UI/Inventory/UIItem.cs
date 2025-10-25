﻿using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    [SelectionBase]
    public class UIItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image imageBackground;
        public Image imagePortrait;
        public Image imageDecor;
        public TMP_Text label;

        public InventoryModel Inventory;
        public CompositeEntity Data;

        private Transform m_LastParent;

        [HideInInspector]
        public Transform TargetTransform;

        public void Setup(CardVisual visual)
        {
            if (visual == null) return;
            
            imageDecor.sprite = visual.Decor;
            imageDecor.gameObject.SetActive(visual.Decor != null);

            imagePortrait.sprite = visual.Portrait;
            imagePortrait.gameObject.SetActive(visual.Portrait != null);
        }
        
        public void Setup(CompositeEntity entity)
        {
            Data = entity;
            
            var visual = entity.Model.GetComponent<CardVisual>();
            Setup(visual);

            var stack = entity.GetComponent<Stackable.Component>();
            if (stack != null && stack.Limit > 1)
            {
                label.SetText(stack.Count.ToString());
            }
            else
            {
                label.SetText(string.Empty);
            }
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
            TargetTransform = null;
        }
    }
}