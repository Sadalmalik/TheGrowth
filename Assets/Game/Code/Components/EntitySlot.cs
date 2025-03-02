using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class EntitySlot : SerializedMonoBehaviour
    {
        public GameObject marker;
        
        public bool isTableSlot;
        [ShowIf(nameof(isTableSlot))]
        public Vector2Int index;
        public List<EntityCard> Cards;

        public EntityCard Top()
        {
            return Cards.Top();
        }
        
        public EntityCard Peek()
        {
            return Cards.Peek();
        }

        public void ShowMarker(bool show)
        {
            marker?.SetActive(show);
        }
        
        public Vector3 GetNewPosition()
        {
            return transform.position + transform.up * RootConfig.Instance.CardThickness * (1 + Cards.Count);
        }

        public Vector3 GetNewRotation()
        {
            return transform.rotation.eulerAngles +
                   Vector3.up * Random.Range(
                       -RootConfig.Instance.RandomAngle,
                       +RootConfig.Instance.RandomAngle);
        }
    }
}