using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class GridBuilder : MonoBehaviour
    {
        public Transform prefab;
        public bool local = true;
        public bool cented = true;

        public Vector2Int size = new Vector2Int(5, 5);
        public Vector3 xStep = Vector3.right;
        public Vector3 yStep = Vector3.forward;

        [Button("Build")]
        public void ExpressionLabel()
        {
            transform.Clear();

            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                var t = Instantiate(prefab, transform);
                t.gameObject.name = $"{prefab.name}: {x}, {y}";
                float px = x;
                float py = y;
                if (cented)
                {
                    px -= size.x * 0.5f - 0.5f;
                    py -= size.y * 0.5f - 0.5f;
                }
                if (local)
                {
                    t.localPosition = px * xStep + py * yStep;
                }
                else
                {
                    t.position = px * xStep + py * yStep;
                }
            }
        }

        [Button("Clear")]
        public void Clear()
        {
            transform.Clear();
        }
    }
}