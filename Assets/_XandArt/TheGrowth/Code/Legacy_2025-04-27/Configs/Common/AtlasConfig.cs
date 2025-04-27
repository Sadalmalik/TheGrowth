using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.TheGrowth
{
    [CreateAssetMenu(
        fileName = "Atlas",
        menuName = "[Game]/Atlas",
        order = 0)]
    public class AtlasConfig : SerializedScriptableObject
    {
        [PreviewField(200)]
        [OnValueChanged(nameof(Invalidate))]
        public Texture targetTexture;

        public Material targetMaterial;

        [OnValueChanged(nameof(Invalidate))]
        public Vector2Int tiles;

        [OnValueChanged(nameof(Invalidate))]
        public float pixelPerUnit = 100;

        private Dictionary<Vector2Int, Mesh> _meshes;

        public Mesh GetMeshForSprite(Vector2Int index)
        {
            if (_meshes == null)
                BuildMeshes();
            return _meshes[index];
        }

        private void Invalidate()
        {
            if (_meshes == null)
                return;
            foreach (var mesh in _meshes.Values)
                mesh.SafeDestroy();
            _meshes.Clear();
            _meshes = null;
        }

        public void BuildMeshes()
        {
            _meshes = new Dictionary<Vector2Int, Mesh>();
            float aspect = 0.5f / pixelPerUnit;
            Vector2 tileSize = new Vector2(targetTexture.width / (float) tiles.x, targetTexture.height / (float) tiles.y);
            Vector2 reverseTextureSize = new Vector2(1f / targetTexture.width, 1f / targetTexture.height);
            Vector2Int index = Vector2Int.zero;
            
            for (index.y = 0; index.y < tiles.y; index.y++)
            for (index.x = 0; index.x < tiles.x; index.x++)
            {
                Mesh mesh = new Mesh
                {
                    vertices = new[]
                    {
                        new Vector3(-aspect * tileSize.x, 0, -aspect * tileSize.y),
                        new Vector3(+aspect * tileSize.x, 0, -aspect * tileSize.y),
                        new Vector3(+aspect * tileSize.x , 0, +aspect * tileSize.y),
                        new Vector3(-aspect * tileSize.x, 0, +aspect * tileSize.y),
                    },
                    uv = new[]
                    {
                        new Vector2(
                            (index.x + 0) * tileSize.x * reverseTextureSize.x,
                            (index.y + 0) * tileSize.y * reverseTextureSize.y),
                        new Vector2(
                            (index.x + 1) * tileSize.x * reverseTextureSize.x,
                            (index.y + 0) * tileSize.y * reverseTextureSize.y),
                        new Vector2(
                            (index.x + 1) * tileSize.x * reverseTextureSize.x,
                            (index.y + 1) * tileSize.y * reverseTextureSize.y),
                        new Vector2(
                            (index.x + 0) * tileSize.x * reverseTextureSize.x,
                            (index.y + 1) * tileSize.y * reverseTextureSize.y)
                    },
                    normals = new[]
                    {
                        Vector3.up,
                        Vector3.up,
                        Vector3.up,
                        Vector3.up,
                    },
                    triangles = new[]
                    {
                        0, 2, 1,
                        0, 3, 2
                    }
                };

                _meshes[index] = mesh;
            }
        }
    }
}