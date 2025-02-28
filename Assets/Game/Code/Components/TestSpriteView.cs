using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class TestSpriteView : SerializedMonoBehaviour
    {
        public MeshFilter filter;
        public Sprite sprite;

        public AtlasConfig atlas;
        public Vector2Int entry;
        
        [Button]
        private void RebuildMesh()
        {
            string[] items = sprite.uv.Select(v => v.ToString()).ToArray();
            Debug.Log(string.Join("\n", items));
            
        }
        
        [Button]
        private void RebuildMeshFromAtlas()
        {
            filter.mesh = atlas.GetMeshForSprite(entry);
            string[] items =  filter.sharedMesh.vertices.Select(v => v.ToString()).ToArray();
            Debug.Log(string.Join("\n", items));
        }
        
    }
}