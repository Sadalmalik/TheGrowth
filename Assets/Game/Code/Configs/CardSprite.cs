using System;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    [Serializable]
    public class CardSprite
    {
        public AtlasConfig atlas;
        public Vector2Int sprite;

        public Mesh Model => atlas.GetMeshForSprite(sprite);
        public Material Material => atlas.targetMaterial;
    }
}