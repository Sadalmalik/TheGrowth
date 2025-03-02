using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    [CreateAssetMenu(
        fileName = "CardSpawner",
        menuName = "[Game]/CardSpawner",
        order = 0)]
    public class CardSpawner : SingletonScriptableObject<CardSpawner>
    {
        public EntityCard prefab;

        private int _count = 0;
        
        public EntityCard CreateCard(CardConfig config)
        {
            var entity = GameObject.Instantiate<EntityCard>(prefab);
            entity.SetConfig(config);
            entity.name = $"Entity#{_count++}: {config.name}";
            return entity;
        }
    }
}