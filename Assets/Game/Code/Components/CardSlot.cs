using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class CardSlot : SerializedMonoBehaviour
    {
        public bool isTableSlot;
        [ShowIf(nameof(isTableSlot))]
        public Vector2Int index;
        public List<CardEntity> Cards;

        public CardEntity Top()
        {
            return Cards.Top();
        }
        
        public CardEntity Peek()
        {
            return Cards.Peek();
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