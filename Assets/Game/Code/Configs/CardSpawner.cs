using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    [CreateAssetMenu(
        fileName = "CardSpawner",
        menuName = "[Game]/CardSpawner",
        order = 0)]
    public class CardSpawner : SingletonScriptableObject<CardSpawner>
    {
        public CardEntity prefab;

        public CardEntity CreateCard(CardConfig config)
        {
            var entity = GameObject.Instantiate<CardEntity>(prefab);
            entity.SetConfig(config);
            return entity;
        }
    }
}