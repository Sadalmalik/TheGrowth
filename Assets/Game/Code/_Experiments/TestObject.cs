using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Sadalmalik.TheGrowth
{
    [Serializable]
    public class TestObjectData
    {
        public AssetReference<CardModel> config1;
        public AssetReference<CardModel> config2;
    }

    [Serializable]
    public abstract class MyElement
    {
        
    }
    
    public class MyIntElement : MyElement
    {
        public int Value;
        
        public override string ToString()
        {
            return $"[int: {Value}]";
        }
    }
    
    public class MyStrElement : MyElement
    {
        public string String;
        
        public override string ToString()
        {
            return $"[str: {String}]";
        }
    }
    
    public class MyVecElement : MyElement
    {
        public Vector3 Position;
        
        public override string ToString()
        {
            return $"[vec: {Position}]";
        }
    }
    
    
    [Serializable]
    public class MyData
    {
        public MyElement[] Elements;

        public override string ToString()
        {
            var arr = Elements.Select(e => e.ToString()).ToArray();
            return string.Join("\n", arr);
        }
    }

    [ExecuteInEditMode]
    public class TestObject : MonoBehaviour
    {
        public CardModel config1;
        public CardModel config2;

        public bool test1;
        public bool test2;

        public void Update()
        {
            test1.Trigger(Test1);
            test2.Trigger(Test2);
            
        }

        private void Test1()
        {
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

        private void Test2()
        {
            var data = new MyData
            {
                Elements = new MyElement[]
                {
                    new MyIntElement { Value = 15 },
                    new MyStrElement { String = "Dugh" },
                    new MyVecElement { Position = Vector3.one },
                }
            };

            var vectorConverter = new VectorJsonConverter();
            
            var json = JsonConvert.SerializeObject(data, Formatting.Indented, vectorConverter);
            Debug.Log(json);
            var loadedData= JsonConvert.DeserializeObject<MyData>(json, vectorConverter);
            Debug.Log("DATA:\n"+loadedData);
        }
    }
}