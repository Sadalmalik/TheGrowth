using System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    [Serializable]
    public class TestObjectData
    {
        public AssetReference<CardConfig> config1;
        public AssetReference<CardConfig> config2;
    }

    [ExecuteInEditMode]
    public class TestObject : MonoBehaviour
    {
        public CardConfig config1;
        public CardConfig config2;

        public bool test;

        public void Update()
        {
            if (!test) return;
            test = false;
            
            var data = new TestObjectData();
            data.config1.Value = config1;
            data.config2.Value = config2;
            var json = JsonConvert.SerializeObject(data);
            Debug.Log(json);

            var loadedData= JsonConvert.DeserializeObject<TestObjectData>(json);
            
            Debug.Log(loadedData.config1);
            Debug.Log(loadedData.config1.Value);
            Debug.Log(loadedData.config2);
            Debug.Log(loadedData.config2.Value);
        }
    }
}