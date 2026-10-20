using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace XandArt.Architecture
{
    public class VectorJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3);
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var m = (Vector3) value;
            
            writer.WriteStartArray();
            writer.WriteValue(m.x);
            writer.WriteValue(m.y);
            writer.WriteValue(m.z);
            writer.WriteEnd();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return Vector3.zero;

            var a = JArray.Load(reader);
            return new Vector3
            {
                x = (float) a[0],
                y = (float) a[1],
                z = (float) a[2]
            };
        }
    }
}