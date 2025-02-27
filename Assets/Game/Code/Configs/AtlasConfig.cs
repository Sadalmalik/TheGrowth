using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    [CreateAssetMenu(
        fileName = "Atlas",
        menuName = "[Game]/Atlas",
        order = 0)]
    public class AtlasConfig : SerializedScriptableObject
    {
        [PreviewField(200)]
        public Texture targetTexture;
        public Material targetMaterial;
        public Vector2Int tiles;
        public float pixelPerUnit = 100;

        private Dictionary<Vector2Int, Mesh> _meshes;

        public Mesh GetMeshForSprite(Vector2Int index)
        {
            if (_meshes == null)
                BuildMeshes();
            return _meshes[index];
        }
        
        public void BuildMeshes()
        {
            _meshes = new Dictionary<Vector2Int, Mesh>();
            float aspect = 0.5f / pixelPerUnit;
            Vector2 textureSize  = new Vector2(targetTexture.width, targetTexture.height);
            Vector2 tileSize = textureSize / tiles;
            Vector2 reverseTextureSize = new Vector2(1f/targetTexture.width, 1f/targetTexture.height);
            Vector2Int index = Vector2Int.zero;
            for (index.y = 0; index.y < tiles.y; index.y++)
            for (index.x = 0; index.x < tiles.x; index.x++)
            {
                Mesh mesh = new Mesh
                {
                    vertices = new []
                    {
                        new Vector3(-aspect * tileSize.x * textureSize.x, 0, -aspect * tileSize.x * textureSize.y),
                        new Vector3(+aspect * tileSize.x * textureSize.x, 0, -aspect * tileSize.x * textureSize.y),
                        new Vector3(+aspect * tileSize.x * textureSize.x, 0, +aspect * tileSize.x * textureSize.y),
                        new Vector3(-aspect * tileSize.x * textureSize.x, 0, +aspect * tileSize.x * textureSize.y),
                    },
                    uv = new []
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
                    normals = new []
                    {
                        Vector3.up,
                        Vector3.up,
                        Vector3.up,
                        Vector3.up,
                    },
                    triangles = new []
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