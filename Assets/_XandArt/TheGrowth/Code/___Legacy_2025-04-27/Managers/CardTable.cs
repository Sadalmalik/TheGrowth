using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class CardTable : SingletonMonoBehaviour<CardTable>
    {
        public SlotEntity slotPrefab;
        
        [Space]
        [OnValueChanged(nameof(InternalRebuild))]
        public Vector2Int size = new Vector2Int(5, 5);
        
        [OnValueChanged(nameof(InternalRebuild))]
        public Vector3 xStep = Vector3.right;
        
        [OnValueChanged(nameof(InternalRebuild))]
        public Vector3 yStep = Vector3.forward;

        [Space]
        [TableMatrix(SquareCells = true)]
        public SlotEntity[,] Grid;

        public List<SlotEntity> slots = new List<SlotEntity>();
        
        public SlotEntity this[Vector2Int pos]
            => Grid[pos.x, pos.y];

        [Button]
        public void Clear()
        {
        }
        
        [Button]
        public void Rebuild()
        {
        }
        
        private void InternalRebuild()
        {
        }
    }
}