using System;
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
        [OnValueChanged(nameof(Rebuild))]
        public Vector2Int size = new Vector2Int(5, 5);
        [OnValueChanged(nameof(Rebuild))]
        public Vector3 xStep = Vector3.right;
        [OnValueChanged(nameof(Rebuild))]
        public Vector3 yStep = Vector3.forward;

        [Space]
        [TableMatrix(SquareCells = true)]
        public EntitySlot[,] grid;

        public List<EntitySlot> slots = new List<EntitySlot>();
        
        public EntitySlot this[Vector2Int pos]
            => grid[pos.x, pos.y];
        
        [Button]
        private void Rebuild()
        {
            if (size.x <= 0 || size.y <= 0)
            {
                transform.Clear();
                grid = new EntitySlot[0, 0];
                return;
            }
            
            List<EntitySlot> temp = new List<EntitySlot>();
            if (grid != null)
            {
                temp.AddRange(grid.Cast<EntitySlot>().Distinct());
                temp.Remove(null);
            }
            
            grid = new EntitySlot[size.x, size.y];

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
                grid[x, y] = slot;
            }

            // Clean remains
            foreach (var slot in temp)
            {
                slot.gameObject.SafeDestroy();
            }
            
            slots.Clear();
            slots.AddRange(grid.Cast<EntitySlot>());

            EntitySlot GetSlot()
            {
                if (temp.Count>0)
                    return temp.Peek();
                return (EntitySlot) PrefabUtility.InstantiatePrefab(slotPrefab, transform);
            }
        }
    }
}