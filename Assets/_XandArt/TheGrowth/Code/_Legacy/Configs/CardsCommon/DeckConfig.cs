using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using XandArt.Architecture;

namespace XandArt.TheGrowth
{
    public class DeckEntry
    {
        public int Amount;
        public EntityModel Entity;
    }
    
    [CreateAssetMenu(
        fileName = "DeckConfig",
        menuName = "[Game]/DeckConfig",
        order = 0)]
    public class DeckConfig : SerializedScriptableObject
    {
        [TextArea(3, 5)]
        public string description;

        public bool shuffleOnStart = true;

        [InfoBox("$DynamicMessage")]
        [TableList(ShowIndexLabels = true)]
        public List<DeckEntry> entries;

        public IEnumerable<EntityModel> Entities => entries.Select(entry => entry.Entity).Distinct();
        
        private string DynamicMessage
        {
            get
            {
                var sum = entries.Sum(entry => entry.Amount);
                return $"Total: {sum}";
            }
        }
        
        public List<CompositeEntity> CreateCards(GameState gameState, int limit)
        {
            var models = new List<EntityModel>();
            foreach (var entry in entries)
            {
                var config = entry.Entity;
                var amount = entry.Amount;
                for (int k = 0; k < amount; k++)
                    models.Add(config);
            }
            models.Shuffle();

            return models.Select(gameState.Create<CompositeEntity>).ToList();
        }
    }
}