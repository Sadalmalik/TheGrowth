using System.Collections.Generic;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class CardSlot : MonoBehaviour
    {
        public List<CardEntity> Cards;

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