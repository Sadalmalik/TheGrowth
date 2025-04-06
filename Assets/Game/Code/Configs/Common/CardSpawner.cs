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
        
        public EntityCard CreateCard(CardModel model)
        {
            var visual = model.GetComponent<CardVisual>();
            var entity = GameObject.Instantiate<EntityCard>(visual?.CustomPrefab ?? prefab);
            entity.name = $"Entity#{_count++}: {model.name}";
            entity.SetConfig(model);
            foreach (var component in model.components)
                component.OnEntityCreated(entity);
            return entity;
        }
    }
}