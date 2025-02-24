using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    public class TestObjectData
    {
        public AssetReference<CardConfig> config;
    }

    [ExecuteInEditMode]
    public class TestObject : MonoBehaviour
    {
        public CardConfig config;

        public bool test;

        public void Update()
        {
            if (!test) return;
            test = false;
            
            var data = new TestObjectData();
            data.config.Value = config;
            var json = JsonConvert.SerializeObject(data);
            Debug.Log(json);

            var loadedData= JsonConvert.DeserializeObject<TestObjectData>(json);
            
            Debug.Log(loadedData.config);
            Debug.Log(loadedData.config.Value);
        }
    }
}