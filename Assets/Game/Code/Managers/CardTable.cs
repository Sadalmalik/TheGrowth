using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class CardTable : SingletonMonoBehaviour<CardTable>
    {
        public EntitySlot slotPrefab;
        
        [Space]
        [OnValueChanged(nameof(InternalRebuild))]
        public Vector2Int size = new Vector2Int(5, 5);
        [OnValueChanged(nameof(InternalRebuild))]
        public Vector3 xStep = Vector3.right;
        [OnValueChanged(nameof(InternalRebuild))]
        public Vector3 yStep = Vector3.forward;

        [Space]
        [TableMatrix(SquareCells = true)]
        public EntitySlot[,] Grid;

        public List<EntitySlot> slots = new List<EntitySlot>();
        
        public EntitySlot this[Vector2Int pos]
            => Grid[pos.x, pos.y];

        [Button]
        public void Clear()
        {
            transform.Clear();
            Grid = new EntitySlot[0, 0];
            slots.Clear();
        }
        
        [Button]
        public void Rebuild()
        {
            transform.Clear();
            Grid = new EntitySlot[0, 0];
            slots.Clear();
            InternalRebuild();
        }
        
        private void InternalRebuild()
        {
            if (size.x <= 0 || size.y <= 0)
            {
                transform.Clear();
                Grid = new EntitySlot[0, 0];
                return;
            }
            
            List<EntitySlot> temp = new List<EntitySlot>();
            if (Grid != null)
            {
                temp.AddRange(Grid.Cast<EntitySlot>().Distinct());
                temp.Remove(null);
            }
            
            Grid = new EntitySlot[size.x, size.y];

            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                var slot = GetSlot();
                slot.isTableSlot = true;
                slot.index = new Vector2Int(x, y);
                slot.name = $"{slotPrefab.name} ({x}, {y})";
                var px = x - size.x * 0.5f + 0.5f;
                var py = y - size.y * 0.5f + 0.5f;
                slot.transform.localPosition = px * xStep + py * yStep;
                Grid[x, y] = slot;
            }

            // Clean remains
            foreach (var slot in temp)
            {
                slot.gameObject.SafeDestroy();
            }
            
            slots.Clear();
            slots.AddRange(Grid.Cast<EntitySlot>());

            EntitySlot GetSlot()
            {
                if (temp.Count>0)
                    return temp.Peek();
                return (EntitySlot) PrefabUtility.InstantiatePrefab(slotPrefab, transform);
            }
        }
    }
}