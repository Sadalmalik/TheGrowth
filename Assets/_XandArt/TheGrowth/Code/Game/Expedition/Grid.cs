using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class Grid : SerializedMonoBehaviour
    {
        public GridCell cellPrefab;
        
        [Space]
        [OnValueChanged(nameof(InternalRebuild))]
        public Vector2Int size = new Vector2Int(5, 5);
        [OnValueChanged(nameof(InternalRebuild))]
        public Vector3 xStep = Vector3.right;
        [OnValueChanged(nameof(InternalRebuild))]
        public Vector3 yStep = Vector3.forward;
        
        [Space]
        [TableMatrix(SquareCells = true)]
        public GridCell[,] Cells;
        
        private void InternalRebuild()
        {
            if (size.x <= 0 || size.y <= 0)
            {
                transform.Clear();
                Cells = new GridCell[0, 0];
                return;
            }
            
            var temp = new List<GridCell>();
            if (Cells != null)
            {
                temp.AddRange(Cells.Cast<GridCell>().Distinct());
                temp.Remove(null);
            }
            
            Cells = new GridCell[size.x, size.y];

            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                var cell = GetSlot();
                cell.index = new Vector2Int(x, y);
                cell.name = $"{cellPrefab.name} ({x}, {y})";
                var px = x - size.x * 0.5f + 0.5f;
                var py = y - size.y * 0.5f + 0.5f;
                cell.transform.localPosition = px * xStep + py * yStep;
                Cells[x, y] = cell;
            }

            // Clean remains
            foreach (var slot in temp)
            {
                slot.gameObject.SafeDestroy();
            }

            GridCell GetSlot()
            {
                if (temp.Count>0)
                    return temp.Peek();
                var result = PrefabUtility.InstantiatePrefab(cellPrefab, transform);
                return (GridCell) result;
            }
        }
    }
}