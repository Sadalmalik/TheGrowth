using System;
using Newtonsoft.Json;
using UnityEngine;

namespace XandArt.TheGrowth
{
    public class DebugConverter : JsonConverter
    {
        public string ID = "";

        public override bool CanConvert(Type objectType)
        {
            Debug.Log($"[TEST] Testing type{ID}: {objectType}");
            return false;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}