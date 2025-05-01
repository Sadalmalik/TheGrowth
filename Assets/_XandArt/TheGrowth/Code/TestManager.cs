using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace XandArt.TheGrowth
{
    [ExecuteInEditMode]
    public class TestManager : SerializedMonoBehaviour
    {
        public CardModel model;

        [Button]
        private void DoTest()
        {
            var state = GameState.Create();
            
            state.Add(new CardEntity(model));

            var pm = new PersistenceManager();
            pm.Save("TestSave", state);
            
            Debug.LogWarning("Loading Start");
            
            var loaded = pm.Load("TestSave");

            Debug.LogWarning("Loading finished");
            
            foreach (var entity in loaded.Entities)
            {
                var type = entity.GetType();
                Debug.Log($"Entity {type}");
                // if (entity is CardEntity card)
                // {
                //     Debug.Log($"Entity {type}: {(CardModel)card.Model}");
                // }
            }
        }

        [Button]
        void TestType()
        {
            var converter = new AssetRefConverter();

            converter.CanConvert(typeof(AssetRef<CardModel>));
        }
    }
}