using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class EntitySlot : SerializedMonoBehaviour
    {
        public GameObject marker;
        
        public bool isTableSlot;
        [ShowIf(nameof(isTableSlot))]
        public Vector2Int index;
        public List<EntityCard> Cards;

        public bool IsEmpty => Cards.Count == 0;
        
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
            return transform.position + transform.up * CardsViewConfig.Instance.cardThickness * (1 + Cards.Count);
        }

        public Vector3 GetNewRotation()
        {
            return transform.rotation.eulerAngles +
                   Vector3.up * Random.Range(
                       -CardsViewConfig.Instance.randomAngle,
                       +CardsViewConfig.Instance.randomAngle);
        }
    }
}